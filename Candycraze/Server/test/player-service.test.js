import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import { MongoMemoryServer } from 'mongodb-memory-server';
import { MongoClient } from 'mongodb';
import { createApp } from '../app.js';
import { validateSave, defaultSave } from '../profile.js';
import { verifyPlayGamesCode } from '../google.js';
let mongo, client, players, server, base;
before(async () => {
  mongo = await MongoMemoryServer.create();
  client = new MongoClient(mongo.getUri()); await client.connect();
  players = client.db('test').collection('players');
  server = createApp({ players, verify: async code => {
    if (!['alice', 'bob', 'race'].includes(code)) throw Object.assign(new Error('Rejected'), { status: 401 });
    return { playerId: code, displayName: code.toUpperCase() };
  }}).listen(0, '127.0.0.1');
  await new Promise(resolve => server.once('listening', resolve));
  base = 'http://127.0.0.1:' + server.address().port;
});
after(async () => {
  if (server) { server.closeAllConnections(); await new Promise(resolve => server.close(resolve)); }
  if (client) await client.close();
  if (mongo) await mongo.stop();
});
async function post(path, body) {
  const result = await fetch(base + path, { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify(body) });
  return { status: result.status, body: await result.json() };
}
const login = code => post('/api/auth/play-games', { authorizationCode: code, playerId: 'spoofed' });
test('one verified player, one document across login, save, logout, restore', async () => {
  const first = await login('alice');
  assert.equal(first.status, 200); assert.equal(first.body.user.playerId, 'alice');
  assert.equal(JSON.parse(first.body.user.saveData).CurrentLevel, 3);
  const save = defaultSave();
  save.Coins = 321; save.BoosterHammer = 4; save.SoundOn = false; save.LevelsStarted = 12;
  save.LevelEntries = [{ LevelNumber: 5, Completed: true, StarsEarned: 3, HighScore: 12345, Attempts: 2, Wins: 1 }];
  const upload = { sessionToken: first.body.sessionToken, revision: 0, saveData: JSON.stringify(save) };
  const sent = await post('/api/save/upload', upload);
  assert.equal(sent.status, 200); assert.equal(sent.body.revision, 1);
  assert.equal((await post('/api/save/upload', upload)).body.revision, 1, 'identical retry is idempotent');
  const stale = await post('/api/save/upload', { ...upload, saveData: JSON.stringify({ ...save, Coins: 999 }) });
  assert.equal(stale.status, 409);
  assert.equal((await post('/api/auth/logout', { sessionToken: first.body.sessionToken })).status, 200);
  assert.equal((await post('/api/save/upload', upload)).status, 401);
  const second = await login('alice');
  const restored = JSON.parse(second.body.user.saveData);
  assert.equal(restored.CurrentLevel, 6); assert.equal(restored.TotalStars, 3);
  assert.equal(restored.Coins, 321); assert.equal(restored.SoundOn, false); assert.equal(restored.BoosterHammer, 4);
  assert.equal(await players.countDocuments({ _id: 'alice' }), 1);
  const doc = await players.findOne({ _id: 'alice' });
  assert.equal(doc.loginCount, 2); assert.equal(doc.saveCount, 1); assert.equal(doc.logoutCount, 1);
  assert.ok(!JSON.stringify(doc).includes(second.body.sessionToken), 'raw session token must not be stored');
});
test('new account does not receive another player save; old session cannot write', async () => {
  const bob = await login('bob');
  assert.equal(JSON.parse(bob.body.user.saveData).Coins, 0);
  const newer = await login('bob');
  assert.equal((await post('/api/save/upload', { sessionToken: bob.body.sessionToken, revision: 0, saveData: JSON.stringify(defaultSave()) })).status, 401);
  assert.equal((await post('/api/save/upload', { sessionToken: newer.body.sessionToken, revision: 0, saveData: JSON.stringify(defaultSave()) })).status, 200);
});
test('concurrent first sign-ins cannot create duplicate records', async () => {
  const results = await Promise.all(Array.from({ length: 5 }, () => login('race')));
  assert.ok(results.every(result => result.status === 200));
  assert.equal(await players.countDocuments({ _id: 'race' }), 1);
  assert.equal((await players.findOne({ _id: 'race' })).loginCount, 5);
});
test('failed Google verification creates no record', async () => {
  assert.equal((await login('invalid')).status, 401);
  assert.equal(await players.countDocuments({ _id: 'spoofed' }), 0);
  assert.equal(await players.countDocuments({ _id: 'invalid' }), 0);
});
test('malformed saves and duplicate level records are rejected', () => {
  for (const value of ['null', '[]', '{}garbage', '{"Coins":-1}', '{"Lives":99}', '{"SoundOn":"true"}',
    '{"LevelEntries":[{"LevelNumber":1,"Completed":true},{"LevelNumber":1,"Completed":true}]}'])
    assert.throws(() => validateSave(value));
  assert.equal(validateSave('{}').CurrentLevel, 3);
  assert.equal(validateSave('{"playerId":"admin","sessionHash":"fake"}').playerId, undefined);
});
test('Google identity comes from players/me, not client claims', async () => {
  const calls = [];
  const identity = await verifyPlayGamesCode('single-use-code', { GOOGLE_CLIENT_ID: 'web-client', GOOGLE_CLIENT_SECRET: 'secret' },
    async (url, options) => {
      calls.push({ url, options });
      return { ok: true, json: async () => calls.length === 1 ? { access_token: 'access' } : { playerId: 'verified', displayName: 'Candy' } };
    });
  assert.equal(identity.playerId, 'verified');
  assert.equal(calls[0].options.body.get('client_id'), 'web-client');
  assert.equal(calls[1].options.headers.Authorization, 'Bearer access');
  await assert.rejects(() => verifyPlayGamesCode('single-use-code', {}, async () => ({ ok: false })), /rejected/);
});
