// ============================================================
// save.js — cloud save/load of game data.
//   POST /api/save/upload   — save game data (verifies token)
//   POST /api/save/download — fetch latest game data
// Every request re-verifies the Google token so only the owner
// can read/write their own single document.
// ============================================================

const express = require('express');
const { OAuth2Client } = require('google-auth-library');
const User = require('../models/User');

const router = express.Router();
const client = new OAuth2Client(process.env.GOOGLE_CLIENT_ID);

// Helper: verify token and return the googleId (or null).
async function verify(idToken) {
  if (!idToken) return null;
  try {
    const ticket = await client.verifyIdToken({
      idToken,
      audience: process.env.GOOGLE_CLIENT_ID,
    });
    return ticket.getPayload().sub;
  } catch {
    return null;
  }
}

// POST /api/save/upload
// Body: { idToken, saveData }  (saveData = JSON string of the game save)
router.post('/upload', async (req, res) => {
  try {
    const { idToken, saveData } = req.body;
    const googleId = await verify(idToken);
    if (!googleId) return res.status(401).json({ success: false, error: 'Invalid token.' });
    if (typeof saveData !== 'string') {
      return res.status(400).json({ success: false, error: 'saveData must be a JSON string.' });
    }

    await User.findOneAndUpdate(
      { googleId },
      { $set: { saveData, lastSyncedAt: new Date() } },
      { upsert: true }
    );

    return res.json({ success: true });
  } catch (err) {
    console.error('[save/upload] Error:', err);
    return res.status(500).json({ success: false, error: 'Server error.' });
  }
});

// POST /api/save/download
// Body: { idToken }
// Returns: { success, saveData }
router.post('/download', async (req, res) => {
  try {
    const { idToken } = req.body;
    const googleId = await verify(idToken);
    if (!googleId) return res.status(401).json({ success: false, error: 'Invalid token.' });

    const user = await User.findOne({ googleId });
    return res.json({ success: true, saveData: user ? user.saveData : '{}' });
  } catch (err) {
    console.error('[save/download] Error:', err);
    return res.status(500).json({ success: false, error: 'Server error.' });
  }
});

module.exports = router;
