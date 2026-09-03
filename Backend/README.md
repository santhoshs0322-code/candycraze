# CandyCraze Backend

Node.js/Express API that verifies Google logins and stores each user's game
save in MongoDB Atlas — **one document per user** (upsert by Google ID).

Unity NEVER talks to MongoDB directly. It calls this API over HTTPS. This API
holds the DB credentials (kept secret in `.env`, never in the app).

---

## Endpoints

| Method | Path | Body | Purpose |
|--------|------|------|---------|
| GET  | `/`                  | —                     | Health check |
| POST | `/api/auth/google`   | `{ idToken }`         | Verify Google login, upsert user, return profile + saveData |
| POST | `/api/save/upload`   | `{ idToken, saveData }` | Save game data (JSON string) |
| POST | `/api/save/download` | `{ idToken }`         | Fetch latest game data |

`saveData` is the same JSON your Unity `SaveData` serializes to.

---

## Setup (one time)

### 1. MongoDB Atlas (free tier)
1. Create a free cluster at https://www.mongodb.com/atlas
2. Database Access → add a user (username + password).
3. Network Access → allow access from anywhere (`0.0.0.0/0`) for testing.
4. Connect → Drivers → copy the connection string. It looks like:
   `mongodb+srv://USER:PASS@cluster0.xxxxx.mongodb.net/candycraze?retryWrites=true&w=majority`

### 2. Google OAuth Client ID
1. https://console.cloud.google.com → create a project.
2. APIs & Services → Credentials → Create Credentials → OAuth client ID.
3. Application type: **Android** (use your app's package name + SHA-1), and
   also create a **Web** client ID (the backend verifies tokens against this).
4. Copy the **Client ID**.

### 3. Configure this backend
1. Copy `.env.example` to `.env`.
2. Fill in `MONGODB_URI` and `GOOGLE_CLIENT_ID`.

### 4. Run locally
```
cd Backend
npm install
npm start
```
Open http://localhost:3000 → should show `{ ok: true, ... }`.

---

## Deploy (free hosting)

**Render.com** (easiest):
1. Push this `Backend` folder to a GitHub repo.
2. Render → New → Web Service → connect the repo.
3. Build command: `npm install`   Start command: `npm start`
4. Add environment variables `MONGODB_URI` and `GOOGLE_CLIENT_ID` in the Render dashboard.
5. Deploy → you get a URL like `https://candycraze-backend.onrender.com`.

Put that URL into Unity's `CloudSaveManager.BACKEND_URL`.

Other options: Railway.app, Fly.io, Vercel (serverless) — same idea.

---

## Security notes
- `.env` is git-ignored — never commit secrets.
- The API re-verifies the Google token on every request, so only the owner
  can read/write their own document.
- Restrict Atlas Network Access to your host's IP for production.
