import { MongoClient } from 'mongodb';
import { createApp } from './app.js';
import { verifyPlayGamesCode } from './google.js';

const required = ['MONGODB_URI', 'GOOGLE_CLIENT_ID', 'GOOGLE_CLIENT_SECRET'];
const missing = required.filter(name => !process.env[name]);
if (missing.length) throw new Error('Missing runtime configuration: ' + missing.join(', '));

console.log(`[STARTUP] commit=${(process.env.RENDER_GIT_COMMIT || 'local').slice(0, 7)}`);
console.log('[CONFIG] Google Play Games OAuth: configured');
console.log(`[CONFIG] Play Billing verification: ${process.env.PLAY_SERVICE_ACCOUNT_EMAIL && process.env.PLAY_SERVICE_ACCOUNT_PRIVATE_KEY ? 'configured' : 'NOT configured'}`);
console.log('[MONGODB] connecting...');

const client = new MongoClient(process.env.MONGODB_URI, { serverSelectionTimeoutMS: 10000 });
try {
  await client.connect();
  await client.db('admin').command({ ping: 1 });
  console.log(`[MONGODB] connected database=${process.env.MONGODB_DATABASE || 'candycraze'}`);

  const players = client.db(process.env.MONGODB_DATABASE || 'candycraze').collection('players');
  // Non-unique session index: records without an active session can coexist.
  await players.createIndex({ sessionHash: 1 }, { sparse: true });
  await players.createIndex({ 'purchases.tokenHash': 1 }, { unique: true, sparse: true });
  console.log('[MONGODB] player indexes ready');

  const trustedProxies = process.env.TRUSTED_PROXY_CIDRS ? process.env.TRUSTED_PROXY_CIDRS.split(',').map(value => value.trim()) : false;
  const port = Number(process.env.PORT || 3000);
  const server = createApp({ players, verify: code => verifyPlayGamesCode(code), trustedProxies }).listen(port,
    () => console.log(`[SERVER] CandyCraze player service listening on port ${port}`));
  server.on('error', error => console.error(`[SERVER] failed: ${error.name}: ${error.message}`));
  for (const signal of ['SIGTERM', 'SIGINT']) process.on(signal, () => {
    console.log(`[SERVER] received ${signal}; shutting down`);
    server.close(async () => { await client.close(); process.exit(0); });
  });
} catch (error) {
  console.error(`[MONGODB] connection/setup failed: ${error.name}: ${error.message}`);
  await client.close().catch(() => {});
  process.exitCode = 1;
}
