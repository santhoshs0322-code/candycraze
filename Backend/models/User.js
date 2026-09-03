// ============================================================
// User.js — MongoDB model (one document per Google user).
// Stores profile info + the full game save data JSON.
// Uses upsert on googleId so each user has exactly ONE entry.
// ============================================================

const mongoose = require('mongoose');

const userSchema = new mongoose.Schema(
  {
    googleId: {
      type: String,
      required: true,
      unique: true,
      index: true,
    },
    email: {
      type: String,
      default: '',
    },
    displayName: {
      type: String,
      default: '',
    },
    photoUrl: {
      type: String,
      default: '',
    },
    // The entire game save as a JSON string (same format as the local save).
    // Unity serializes SaveData → JSON → sends it here.
    saveData: {
      type: String,
      default: '{}',
    },
    // Metadata
    lastSyncedAt: {
      type: Date,
      default: Date.now,
    },
  },
  {
    timestamps: true, // adds createdAt, updatedAt automatically
  }
);

module.exports = mongoose.model('User', userSchema);
