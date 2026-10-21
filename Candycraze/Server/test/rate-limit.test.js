import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createApp } from '../app.js';

test('diagnostics cannot exhaust billing or sign-in; each budget still rejects excess requests', async () => {
  const server = createApp({ players: {}, verify: async () => {
    throw Object.assign(new Error('Test rejection'), { status: 401 });
  }, trustedProxies: 'loopback' }).listen(0, '127.0.0.1');
  await new Promise(resolve => server.once('listening', resolve));
  const base = 'http://127.0.0.1:' + server.address().port;
  const post = async (path, ip = '192.0.2.1', body = {}) => {
    const response = await fetch(base + path, { method: 'POST',
      headers: { 'content-type': 'application/json', 'x-forwarded-for': ip },
      body: JSON.stringify(body) });
    await response.json();
    return response;
  };
  try {
    for (let i = 0; i < 300; i++)
      assert.equal((await post('/api/diagnostics/billing-catalog')).status, 200);
    const limited = await post('/api/diagnostics/billing-event');
    assert.equal(limited.status, 429);
    assert.ok(Number(limited.headers.get('retry-after')) > 0);
    for (let i = 0; i < 60; i++)
      assert.equal((await post('/api/billing/account')).status, 401, 'billing reaches authentication despite telemetry limit');
    assert.equal((await post('/api/billing/account')).status, 429);
    assert.equal((await post('/api/billing/account', '192.0.2.2')).status, 401, 'separate client IP has its own budget');
    for (let i = 0; i < 30; i++)
      assert.equal((await post('/api/auth/play-games', '192.0.2.1', { authorizationCode: 'test-code' })).status, 401);
    assert.equal((await post('/api/auth/play-games')).status, 429);
    assert.equal((await fetch(base + '/health')).status, 200);
  } finally {
    server.closeAllConnections();
    await new Promise(resolve => server.close(resolve));
  }
});
