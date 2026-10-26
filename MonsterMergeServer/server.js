require("dotenv").config();
const crypto = require("crypto");
const express = require("express");
const cors = require("cors");
const jwt = require("jsonwebtoken");
const mongoose = require("mongoose");
const { OAuth2Client } = require("google-auth-library");
const apps = require("./apps");
const { createPayments, PaymentError } = require("./games/monstermerge/play-billing");
const { createMonsterAuth } = require("./monster-auth");
const { createMonsterProfile, migrateMonsterDatabase } = require("./monster-profile");

const required = ["MONGODB_URI", "MONSTER_GOOGLE_CLIENT_ID", "MONSTER_GOOGLE_CLIENT_SECRET", "JWT_SECRET", "PUBLIC_URL"];
for (const key of required) {
  if (!process.env[key]) throw new Error(`Missing required environment variable: ${key}`);
}
if (process.env.JWT_SECRET.length < 32) throw new Error("JWT_SECRET must contain at least 32 characters");

const app = express();
app.set("trust proxy", 1);
app.disable("x-powered-by");
const monsterDatabase=process.env.MONSTER_MONGODB_DATABASE||"monstermerge";
if(!/^[A-Za-z0-9_-]{1,64}$/.test(monsterDatabase)||monsterDatabase==="candycraze")throw new Error("Monster Merge requires its own database name");
const monsterConnection=mongoose.connection.useDb(monsterDatabase,{useCache:true});
const payments = createPayments({ connection: monsterConnection, authenticateGoogle: verifyMonsterSession, profiles:()=>usersFor("monstermerge") });
app.use(payments.router);
app.use(cors({ origin: process.env.ALLOWED_ORIGIN || "*" }));
app.use(express.json({ limit: "16kb" }));

const userSchema = new mongoose.Schema({
  googleSub: { type: String, required: true, unique: true, index: true },
  email: { type: String, required: true, lowercase: true, trim: true },
  name: { type: String, required: true, trim: true },
  picture: { type: String, default: "" },
  playerId:String,displayName:String,provider:String,loginCount:{type:Number,default:0},saveCount:{type:Number,default:0},
  profile:mongoose.Schema.Types.Mixed,testProfile:mongoose.Schema.Types.Mixed,payments:mongoose.Schema.Types.Mixed,purchases:mongoose.Schema.Types.Mixed,
  lastLoginAt: { type: Date, default: Date.now }
}, { timestamps: true, versionKey: false });

const userModels = new Map();
function usersFor(appId) {
  const config = apps[appId];
  if (!config) return null;
  if (!userModels.has(appId)) {
    userModels.set(appId, appId==="monstermerge"?monsterConnection.model("MonsterPlayer",userSchema,"players"):mongoose.model(`User_${appId}`, userSchema, config.userCollection));
  }
  return userModels.get(appId);
}

const stateSchema = new mongoose.Schema({
  value: { type: String, required: true, unique: true, index: true },
  appId: { type: String, required: true },
  expiresAt: { type: Date, required: true, expires: 0 }
}, { versionKey: false });
const OAuthState = mongoose.model("OAuthState", stateSchema, "oauth_states");

const revokedSchema = new mongoose.Schema({
  jti: { type: String, required: true, unique: true, index: true },
  expiresAt: { type: Date, required: true, expires: 0 }
}, { versionKey: false });
const RevokedToken = mongoose.model("RevokedToken", revokedSchema, "revoked_tokens");

const publicUrl = process.env.PUBLIC_URL.replace(/\/$/, "");
const redirectUri = `${publicUrl}/auth/google/callback`;
const google = new OAuth2Client(process.env.MONSTER_GOOGLE_CLIENT_ID, process.env.MONSTER_GOOGLE_CLIENT_SECRET, redirectUri);

function appRedirect(appId, values) {
  const config = apps[appId];
  const query = new URLSearchParams(values).toString();
  return `${config.redirectScheme}://auth?${query}`;
}

function issueToken(user, appId) {
  return jwt.sign({ sub: user.id, appId }, process.env.JWT_SECRET, {
    expiresIn: "30d", jwtid: crypto.randomUUID(), issuer: "central-game-auth", audience: appId
  });
}

async function verifyMonsterSession(token) {
  try {
    if (typeof token !== "string" || token.length > 4096) throw new Error("Invalid session");
    const identity = jwt.verify(token, process.env.JWT_SECRET, {
      issuer: "central-game-auth", audience: "monstermerge", algorithms: ["HS256"]
    });
    if (identity.appId !== "monstermerge" || typeof identity.sub !== "string" || !/^[a-f0-9]{24}$/.test(identity.sub)
        || !identity.jti || await RevokedToken.exists({ jti: identity.jti })
        || !await usersFor("monstermerge").exists({ _id: identity.sub })) throw new Error("Invalid session");
    return identity;
  } catch (_) { throw new PaymentError(401, "google_login_required", "Sign in with Google to purchase."); }
}
const monsterAuth = createMonsterAuth({ connection: monsterConnection, google,
  clientId: process.env.MONSTER_GOOGLE_CLIENT_ID, users: () => usersFor("monstermerge"), issueToken,
  verifySession: verifyMonsterSession });
app.use("/auth/monster", monsterAuth.router);
app.use("/auth/monster/profile",createMonsterProfile({users:()=>usersFor("monstermerge"),verifySession:verifyMonsterSession,payments:payments.service,mode:payments.service.config.mode}));

async function requireAuth(req, res, next) {
  try {
    const token = (req.headers.authorization || "").replace(/^Bearer\s+/i, "");
    if (!token) return res.status(401).json({ ok: false, message: "Missing session" });
    const payload = jwt.verify(token, process.env.JWT_SECRET, { issuer: "central-game-auth" });
    if (!apps[payload.appId] || payload.aud !== payload.appId) throw new Error("Invalid app session");
    if (await RevokedToken.exists({ jti: payload.jti })) return res.status(401).json({ ok: false, message: "Session revoked" });
    req.auth = payload;
    next();
  } catch (_) {
    res.status(401).json({ ok: false, message: "Invalid or expired session" });
  }
}

app.get("/health", (_req, res) => res.json({ ok: true, service: "monstermerge", apps: Object.keys(apps) }));

app.get("/auth/google", async (req, res, next) => {
  try {
    const appId = String(req.query.app || "").toLowerCase();
    if (!apps[appId]) return res.status(400).json({ ok: false, message: "Unknown app" });
    if (appId === "monstermerge") return res.status(400).json({ ok: false, message: "Start sign-in from Monster Merge." });
    const state = crypto.randomBytes(32).toString("hex");
    await OAuthState.create({ value: state, appId, expiresAt: new Date(Date.now() + 10 * 60 * 1000) });
    res.redirect(google.generateAuthUrl({
      access_type: "online", scope: ["openid", "email", "profile"], state, prompt: "select_account"
    }));
  } catch (error) { next(error); }
});

app.get("/auth/google/callback", async (req, res) => {
  if (typeof req.query.state === "string" && req.query.state.startsWith("mm_")) {
    try { return await monsterAuth.callback(req, res); }
    catch (_) { return res.status(500).send("Sign-in unavailable. Return to the game and try again."); }
  }
  let stateRecord;
  try {
    stateRecord = await OAuthState.findOneAndDelete({ value: req.query.state, expiresAt: { $gt: new Date() } });
    if (!stateRecord || !req.query.code) return res.status(400).send("Login request expired. Return to the game and try again.");
    const appId = stateRecord.appId;
    const { tokens } = await google.getToken(req.query.code);
    const ticket = await google.verifyIdToken({ idToken: tokens.id_token, audience: process.env.MONSTER_GOOGLE_CLIENT_ID });
    const profile = ticket.getPayload();
    if (!profile || !profile.sub || !profile.email_verified) {
      return res.redirect(appRedirect(appId, { error: "unverified_email" }));
    }

    const User = usersFor(appId);
    const user = await User.findOneAndUpdate(
      { googleSub: profile.sub },
      { $set: { email: profile.email, name: profile.name || profile.email, picture: profile.picture || "", lastLoginAt: new Date() } },
      { new: true, upsert: true, setDefaultsOnInsert: true }
    );
    res.redirect(appRedirect(appId, { token: issueToken(user, appId) }));
  } catch (error) {
    console.error("OAuth callback failed", error.message);
    if (stateRecord && apps[stateRecord.appId]) return res.redirect(appRedirect(stateRecord.appId, { error: "login_failed" }));
    res.status(500).send("Login failed. Return to the game and try again.");
  }
});

app.post("/auth/google/native", async (req, res) => {
  try {
    const appId = String(req.body.app || "").toLowerCase();
    const idToken = String(req.body.idToken || "");
    if (!apps[appId]) return res.status(400).json({ ok: false, message: "Unknown app" });
    if (!idToken || idToken.length > 10000) return res.status(400).json({ ok: false, message: "Missing identity token" });

    const ticket = await google.verifyIdToken({ idToken, audience: process.env.MONSTER_GOOGLE_CLIENT_ID });
    const profile = ticket.getPayload();
    if (!profile || !profile.sub || !profile.email_verified) {
      return res.status(401).json({ ok: false, message: "Google account is not verified" });
    }

    const User = usersFor(appId);
    const user = await User.findOneAndUpdate(
      { googleSub: profile.sub },
      { $set: { email: profile.email, name: profile.name || profile.email, picture: profile.picture || "", lastLoginAt: new Date() } },
      { new: true, upsert: true, setDefaultsOnInsert: true }
    );
    res.json({ ok: true, token: issueToken(user, appId), user: {
      id: user.id, name: user.name, email: user.email, picture: user.picture,
      createdAt: user.createdAt.toISOString(), lastLoginAt: user.lastLoginAt.toISOString()
    }});
  } catch (error) {
    console.error("Native Google sign-in failed", error.message);
    res.status(401).json({ ok: false, message: "Invalid Google identity token" });
  }
});

app.get("/api/me", requireAuth, async (req, res) => {
  const User = usersFor(req.auth.appId);
  const user = await User.findById(req.auth.sub).lean();
  if (!user) return res.status(404).json({ ok: false, message: "User not found" });
  res.json({ ok: true, appId: req.auth.appId, user: {
    id: user._id.toString(), name: user.name, email: user.email, picture: user.picture,
    createdAt: user.createdAt.toISOString(), lastLoginAt: user.lastLoginAt.toISOString()
  }});
});

app.post("/auth/logout", requireAuth, async (req, res) => {
  await RevokedToken.updateOne(
    { jti: req.auth.jti },
    { $setOnInsert: { jti: req.auth.jti, expiresAt: new Date(req.auth.exp * 1000) } },
    { upsert: true }
  );
  res.json({ ok: true });
});

app.use((error, _req, res, _next) => {
  console.error(error);
  res.status(500).json({ ok: false, message: "Server error" });
});

const port = process.env.PORT || 3000;

mongoose.connect(process.env.MONGODB_URI,{dbName:monsterDatabase})
  .then(async () => {
    await migrateMonsterDatabase(mongoose.connection,monsterConnection);
    await Promise.all(Object.keys(apps).map(appId => usersFor(appId).init()));
    await payments.initialize();
    await monsterAuth.initialize();
    app.listen(port, "0.0.0.0", () => console.log(`Monster Merge test service listening on ${port}`));
  })
  .catch(error => { console.error("MongoDB connection failed", error); process.exit(1); });
