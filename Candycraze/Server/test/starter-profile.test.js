import { test } from 'node:test';
import assert from 'node:assert/strict';
import { validateSave } from '../profile.js';

test('starter pack marker survives cloud save with consumed inventory', () => {
  const saved = validateSave(JSON.stringify({ StarterBoostersGranted: true, BoosterHammer: 0, BoosterShuffle: 1 }));
  assert.equal(saved.StarterBoostersGranted, true);
  assert.equal(saved.BoosterHammer, 0);
  assert.equal(saved.BoosterShuffle, 1);
  assert.throws(() => validateSave(JSON.stringify({ StarterBoostersGranted: 'true' })));
});
