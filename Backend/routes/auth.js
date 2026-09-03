// ============================================================
// auth.js — POST /api/auth/google
// Verifies a Google ID token, upserts the user in MongoDB,
// and returns their profile + saved game data.
// ============================================================

const express = require('express');
const { OAuth2Client } = require('google-auth-library');
const User = require('../models/User');

const router = express.Router();

// POST /api/auth/google
// Body: { idToken: "..." }
// Returns: { success, user: { googleId, email, displayName, photoUrl, saveData } }
router.post('/google', async (req, res) => {
  try {
    const { idToken } = req.body;
    if (!idToken) {
      return res.status(400).json({ success: false, error: 'Missing idToken.' });
    }

    // Verify the Google ID token.
    const client = new OAuth2Client(process.env.GOOGLE_CLIENT_ID);
    let ticket;
    try {
      ticket = await client.verifyIdToken({
        idToken,
        audience: process.env.GOOGLE_CLIENT_ID,
      });
    } catch (err) {
      return res.status(401).json({ success: false, error: 'Invalid Google token.' });
    }

    const payload = ticket.getPayload();
    const googleId = payload.sub;

    // Upsert: create user if new, update profile if existing.
    // saveData is NOT overwritten here (only on explicit /save calls).
    const user = await User.findOneAndUpdate(
      { googleId },
      {
        $set: {
          email: payload.email || '',
          displayName: payload.name || '',
          photoUrl: payload.picture || '',
          lastSyncedAt: new Date(),
        },
        $setOnInsert: {
          saveData: '{}', // fresh save for new users
        },
      },
      { upsert: true, new: true, setDefaultsOnInsert: true }
    );

    return res.json({
      success: true,
      user: {
        googleId: user.googleId,
        email: user.email,
        displayName: user.displayName,
        photoUrl: user.photoUrl,
        saveData: user.saveData,
      },
    });
  } catch (err) {
    console.error('[auth/google] Error:', err);
    return res.status(500).json({ success: false, error: 'Server error.' });
  }
});

module.exports = router;
