import express from 'express';
import { createHash, randomBytes } from 'node:crypto';
import { defaultSave, validateSave } from './profile.js';

const hash = token => createHash('sha256').update(token).digest('hex');
const reject = (message, status) => Object.assign(new Error(message), { status });
export function createApp({ players, verify, now = () => new Date(), trustedProxies = false }) {
  const app = express();
  app.disable('x-powered-by');
  app.set('query parser', 'simple');
  app.set('trust proxy', trustedProxies);
  app.use((req, res, next) => {
    res.set('Cache-Control', 'no-store');
    res.set('X-Content-Type-Options', 'nosniff');
    next();
  });
  app.use(express.json({ limit: '1mb', strict: true }));
  // Bounded, per-process abuse protection; use an edge rate limiter for multi-instance deployment.
  const attempts = new Map();
  app.use('/api/auth/play-games', (req, res, next) => {
    const time = Date.now();
    for (const [ip, entry] of attempts) if (time - entry.start > 60000) attempts.delete(ip);
    const ip = req.ip;
    if (!attempts.has(ip) && attempts.size >= 10000) return res.status(429).json({ success: false, error: 'Please retry later.' });
    const entry = attempts.get(ip) ?? { start: time, count: 0 };
    entry.count++; attempts.set(ip, entry);
    if (entry.count > 20) return res.status(429).json({ success: false, error: 'Too many attempts. Retry in a minute.' });
    next();
  });
  const route = fn => (req, res, next) => Promise.resolve(fn(req, res)).catch(next);
  async function session(body) {
    const token = body?.sessionToken;
    if (typeof token !== 'string' || !/^[a-f0-9]{64}$/.test(token)) throw reject('Please sign in again.', 401);
    const sessionHash = hash(token);
    const user = await players.findOne({ sessionHash, sessionExpiresAt: { $gt: now() } });
    if (!user) throw reject('Session expired or changed. Please sign in again.', 401);
    return { user, sessionHash };
  }
  app.get('/health', (req, res) => res.json({ ok: true }));
  app.post('/api/auth/play-games', route(async (req, res) => {
    const identity = await verify(req.body?.authorizationCode); // Never accept identity from the client.
    const date = now(), sessionToken = randomBytes(32).toString('hex');
    const update = {
      $set: { playerId: identity.playerId, displayName: identity.displayName,
        provider: 'google-play-games', lastLoginAt: date, updatedAt: date,
        lastAppVersion: String(req.body?.appVersion ?? '').slice(0, 100),
        sessionHash: hash(sessionToken), sessionExpiresAt: new Date(date.getTime() + 30*86400000) },
      $setOnInsert: { createdAt: date, revision: 0, save: defaultSave(), saveCount: 0 },
      $inc: { loginCount: 1 }
    };
    // MongoDB's unique _id is the verified player ID. Even concurrent first logins
    // cannot create a second document. Retry an upsert race as an ordinary update.
    let user;
    try { user = await players.findOneAndUpdate({ _id: identity.playerId }, update, { upsert: true, returnDocument: 'after' }); }
    catch (error) {
      if (error.code !== 11000) throw error;
      user = await players.findOneAndUpdate({ _id: identity.playerId }, update, { returnDocument: 'after' });
    }
    res.json({ success: true, sessionToken, revision: user.revision,
      user: { playerId: user.playerId, displayName: user.displayName, saveData: JSON.stringify(user.save) } });
  }));
  app.post('/api/save/upload', route(async (req, res) => {
    const { user, sessionHash } = await session(req.body);
    const revision = req.body.revision;
    if (!Number.isSafeInteger(revision) || revision < 0) throw reject('Invalid revision.', 400);
    if (typeof req.body.saveData !== 'string') throw reject('Invalid save payload.', 400);
    const save = validateSave(req.body.saveData), fingerprint = hash(JSON.stringify(save));
    // Retrying a request whose response was lost does not apply it a second time.
    if (user.revision === revision + 1 && user.saveHash === fingerprint)
      return res.json({ success: true, revision: user.revision });
    const result = await players.findOneAndUpdate(
      { _id: user._id, sessionHash, revision, sessionExpiresAt: { $gt: now() } },
      { $set: { save, saveHash: fingerprint, updatedAt: now(), lastSavedAt: now() }, $inc: { revision: 1, saveCount: 1 } },
      { returnDocument: 'after' });
    if (!result) throw reject('Newer cloud progress or a newer session exists. Sign in again.', 409);
    res.json({ success: true, revision: result.revision });
  }));
  app.post('/api/auth/logout', route(async (req, res) => {
    const { user, sessionHash } = await session(req.body);
    await players.updateOne({ _id: user._id, sessionHash },
      { $unset: { sessionHash: '', sessionExpiresAt: '' }, $set: { lastLogoutAt: now() }, $inc: { logoutCount: 1 } });
    res.json({ success: true });
  }));
  app.use((error, req, res, next) => {
    const status = error.status >= 400 && error.status < 500 ? error.status : 503;
    if (status === 503) console.error('Player service request failed:', error.name); // No tokens or payloads in logs.
    res.status(status).json({ success: false, error: status === 503 ? 'Cloud service is temporarily unavailable. Please retry.' : error.message });
  });
  return app;
}
