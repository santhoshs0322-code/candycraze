// ============================================================
// server.js — CandyCraze backend entry point.
// Express API that verifies Google logins and stores each
// user's game save in MongoDB Atlas (one document per user).
// ============================================================

require('dotenv').config();
const express = require('express');
const mongoose = require('mongoose');
const cors = require('cors');

const authRoutes = require('./routes/auth');
const saveRoutes = require('./routes/save');

const app = express();
app.use(cors());
app.use(express.json({ limit: '512kb' })); // save JSON is small

// Health check (open this URL in a browser to confirm the server is up).
app.get('/', (req, res) => {
  res.json({ ok: true, service: 'CandyCraze backend', time: new Date().toISOString() });
});

// API routes
app.use('/api/auth', authRoutes);
app.use('/api/save', saveRoutes);

// ── Start ────────────────────────────────────────────────
const PORT = process.env.PORT || 3000;

async function start() {
  if (!process.env.MONGODB_URI) {
    console.error('[server] MONGODB_URI is not set. Create a .env file (see .env.example).');
    process.exit(1);
  }
  if (!process.env.GOOGLE_CLIENT_ID) {
    console.error('[server] GOOGLE_CLIENT_ID is not set. Create a .env file (see .env.example).');
    process.exit(1);
  }

  try {
    await mongoose.connect(process.env.MONGODB_URI);
    console.log('[server] Connected to MongoDB Atlas.');
  } catch (err) {
    console.error('[server] MongoDB connection failed:', err.message);
    process.exit(1);
  }

  app.listen(PORT, () => {
    console.log(`[server] CandyCraze backend running on port ${PORT}`);
  });
}

start();
