import { MongoClient } from 'mongodb';
import { createApp } from './app.js';
import { verifyPlayGamesCode } from './google.js';
for (const name of ['MONGODB_URI', 'GOOGLE_CLIENT_ID', 'GOOGLE_CLIENT_SECRET'])
  if (!process.env[name]) throw new Error('Missing runtime configuration: ' + name);
const client = new MongoClient(process.env.MONGODB_URI, { serverSelectionTimeoutMS: 10000 });
await client.connect();
const players = client.db(process.env.MONGODB_DATABASE || 'candycraze').collection('players');
// Non-unique session index: records without an active session can coexist.
await players.createIndex({ sessionHash: 1 }, { sparse: true });
const trustedProxies = process.env.TRUSTED_PROXY_CIDRS ? process.env.TRUSTED_PROXY_CIDRS.split(',').map(value => value.trim()) : false;
const server = createApp({ players, verify: code => verifyPlayGamesCode(code), trustedProxies }).listen(Number(process.env.PORT || 3000),
  () => console.log('CandyCraze player service ready'));
for (const signal of ['SIGTERM', 'SIGINT']) process.on(signal, () => server.close(async () => { await client.close(); process.exit(0); }));
