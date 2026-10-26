const {test}=require('node:test');
const assert=require('node:assert/strict');
const {verifyPlayGamesCode}=require('./native-auth');
const env={MONSTER_GOOGLE_CLIENT_ID:'monster-web',MONSTER_GOOGLE_CLIENT_SECRET:'monster-secret',GOOGLE_CLIENT_ID:'candy-web',GOOGLE_CLIENT_SECRET:'candy-secret'};
test('native code exchange uses Monster credentials and server-verified identity',async()=>{
 let calls=0;
 const result=await verifyPlayGamesCode('one-time-code',env,async(url,options)=>{
  calls++;
  if(calls===1){assert.equal(options.body.get('client_id'),'monster-web');assert.equal(options.body.get('client_secret'),'monster-secret');assert.equal(options.body.get('redirect_uri'),'');return {ok:true,json:async()=>({access_token:'private-access-token'})};}
  assert.equal(url,'https://games.googleapis.com/games/v1/players/me');assert.equal(options.headers.Authorization,'Bearer private-access-token');
  return {ok:true,json:async()=>({playerId:'verified-player',displayName:'Dragon'})};
 });
 assert.deepEqual(result,{playerId:'verified-player',name:'Dragon'});assert.equal(calls,2);
});
test('invalid, expired, or unverified native codes cannot authenticate',async()=>{
 await assert.rejects(verifyPlayGamesCode('',env,()=>{throw Error('must not call');}),e=>e.status===400);
 await assert.rejects(verifyPlayGamesCode('expired-code',env,async()=>({ok:false})),e=>e.status===401);
 let n=0;await assert.rejects(verifyPlayGamesCode('valid-code',env,async()=>++n===1?{ok:true,json:async()=>({access_token:'token'})}:{ok:true,json:async()=>({displayName:'untrusted'})}),e=>e.status===401);
});
