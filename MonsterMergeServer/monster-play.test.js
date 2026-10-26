const {test,before,after,beforeEach}=require('node:test');
const assert=require('node:assert/strict');
const crypto=require('node:crypto');
const fs=require('node:fs');
const os=require('node:os');
const path=require('node:path');
const net=require('node:net');
const {spawn}=require('node:child_process');
const mongoose=require('mongoose');
const express=require('express');
const {once}=require('node:events');
const {PlayBilling,createPayments}=require('./games/monstermerge/play-billing');
const {validatePurchase,billingAccountId}=require('./games/monstermerge/play-api');
let players,consumeFails,verifyCalls,consumeCalls;
const owner='0123456789abcdef01234567',other='1123456789abcdef01234567',token='fixture-purchase-token';
function options(){return {connection,profiles:()=>players,env:{MONSTER_PLAY_ENABLED:'true'},verify:async()=>{verifyCalls++;return {quantity:1,orderId:'fixture',test:true};},consume:async()=>{consumeCalls++;if(consumeFails)throw Error('network failure');}};}

let mongo,folder,connection,uri,service,account,order,row,remote,clock,config;
async function freePort() {
  const server=net.createServer();server.listen(0,'127.0.0.1');await once(server,'listening');
  const port=server.address().port;await new Promise(resolve=>server.close(resolve));return port;
}
before(async()=>{
  folder=fs.mkdtempSync(path.join(os.tmpdir(),'monster-play-mongo-'));
  const port=await freePort();
  mongo=spawn(process.env.MONGOD_BINARY||'C:/Program Files/MongoDB/Server/8.2/bin/mongod.exe',
    ['--dbpath',folder,'--port',String(port),'--bind_ip','127.0.0.1','--replSet','playTest','--logpath',path.join(folder,'mongo.log')],{windowsHide:true,stdio:'ignore'});
  let startupError;
  mongo.on('error',error=>{startupError=error;});
  const direct=mongoose.createConnection(`mongodb://127.0.0.1:${port}/admin?directConnection=true`,{serverSelectionTimeoutMS:20000});
  await direct.asPromise();
  if(startupError)throw startupError;
  await direct.db.admin().command({replSetInitiate:{_id:'playTest',members:[{_id:0,host:`127.0.0.1:${port}`}]}});
  await direct.close();
  uri=`mongodb://127.0.0.1:${port}/play_integration?replicaSet=playTest`;
  connection=await mongoose.createConnection(uri,{serverSelectionTimeoutMS:20000}).asPromise();
  players=connection.model('Player',new mongoose.Schema({payments:mongoose.Schema.Types.Mixed,profile:mongoose.Schema.Types.Mixed}));await players.init();
  service=new PlayBilling(options());await service.initialize();
});
after(async()=>{
  if(connection)await connection.close();
  if(mongo&&mongo.exitCode===null){const done=once(mongo,'exit');mongo.kill();await done;}
  if(folder){const resolved=path.resolve(folder);if(path.dirname(resolved)!==path.resolve(os.tmpdir())||!path.basename(resolved).startsWith('monster-play-mongo-'))throw new Error('Unsafe cleanup path');fs.rmSync(resolved,{recursive:true,force:true});}
});
beforeEach(async()=>{await players.deleteMany({});await service.Receipt.deleteMany({});await players.create([{_id:owner},{_id:other}]);consumeFails=false;verifyCalls=consumeCalls=0;});
function purchased(){return {purchaseStateContext:{purchaseState:'PURCHASED'},obfuscatedExternalAccountId:billingAccountId(owner),productLineItem:[{productId:'starter',productOfferDetails:{quantity:1,refundableQuantity:1,consumptionState:'CONSUMPTION_STATE_YET_TO_BE_CONSUMED'}}]};}
test('Google verification rejects pending, wrong account, product, refunds and consumed receipts',()=>{
 assert.equal(validatePurchase(purchased(),'starter',owner).quantity,1);
 for(const state of ['PENDING','CANCELLED']){const p=purchased();p.purchaseStateContext.purchaseState=state;assert.throws(()=>validatePurchase(p,'starter',owner));}
 assert.throws(()=>validatePurchase(purchased(),'starter',other));assert.throws(()=>validatePurchase(purchased(),'legend',owner));
 for(const change of [{quantity:0},{refundableQuantity:0},{consumptionState:'CONSUMPTION_STATE_CONSUMED'}]){const p=purchased();Object.assign(p.productLineItem[0].productOfferDetails,change);assert.throws(()=>validatePurchase(p,'starter',owner));}
});
test('concurrent token replay grants exactly once',async()=>{await Promise.all(Array.from({length:8},()=>service.purchase(owner,'starter',token)));assert.equal((await service.wallet(owner)).creditedCoinsTotal,500);assert.equal(await service.Receipt.countDocuments(),1);});
test('receipt cannot be replayed for another account or product',async()=>{await service.purchase(owner,'starter',token);await assert.rejects(service.purchase(other,'starter',token));await assert.rejects(service.purchase(owner,'legend',token));assert.equal((await service.wallet(other)).creditedCoinsTotal,0);});
test('consume failure survives service restart without duplicate grant',async()=>{consumeFails=true;await assert.rejects(service.purchase(owner,'starter',token));assert.equal((await service.wallet(owner)).creditedCoinsTotal,500);consumeFails=false;const restarted=new PlayBilling(options());await restarted.purchase(owner,'starter',token);assert.equal(verifyCalls,1);assert.equal((await restarted.wallet(owner)).creditedCoinsTotal,500);assert.equal((await service.Receipt.findOne()).consumed,true);});
test('preserves pre-existing live payment identity and balance',async()=>{await players.updateOne({_id:owner},{$set:{payments:{live:{accounts:['legacy-account'],creditedCoinsTotal:1500}},profile:{save:{paymentAccountId:'legacy-account',paidCreditsApplied:1500,coins:200}}}});const result=await service.purchase(owner,'starter',token);assert.equal(result.accountId,'legacy-account');assert.equal(result.creditedCoinsTotal,2000);assert.equal((await players.findById(owner)).profile.save.coins,200);});
test('invalid and disabled purchases do not verify or grant',async()=>{await assert.rejects(service.purchase(owner,'fake',token));await assert.rejects(service.purchase(owner,'starter',''));const disabled=new PlayBilling({...options(),env:{}});await assert.rejects(disabled.purchase(owner,'starter',token));assert.equal(verifyCalls,0);assert.equal(await service.Receipt.countDocuments(),0);});
test('transaction rolls back receipt on balance overflow',async()=>{await players.updateOne({_id:owner},{$set:{'payments.live.creditedCoinsTotal':2147483647}});await assert.rejects(service.purchase(owner,'starter',token));assert.equal(await service.Receipt.countDocuments(),0);assert.equal(consumeCalls,0);});
test('routes require Google session and old PayU checkout is absent',async()=>{
 const app=express(),payments=createPayments({...options(),authenticateGoogle:async t=>{if(t!=='fixture')throw Object.assign(Error('Sign in'),{status:401});return {sub:owner};}});app.use(payments.router);
 const server=app.listen(0,'127.0.0.1');await once(server,'listening');const base=`http://127.0.0.1:${server.address().port}`;
 try {assert.equal((await fetch(base+'/api/billing/wallet')).status,401);assert.equal((await fetch(base+'/api/catalog')).status,404);assert.equal((await fetch(base+'/checkout/old')).status,404);assert.equal((await (await fetch(base+'/payments/health')).json()).provider,'google-play');const r=await fetch(base+'/api/billing/verify',{method:'POST',headers:{Authorization:'Bearer fixture','Content-Type':'application/json'},body:JSON.stringify({productId:'starter',purchaseToken:token})});assert.equal(r.status,200);assert.equal((await r.json()).creditedCoinsTotal,500);}finally{await new Promise(r=>server.close(r));}
});
test('complete auth service starts with Play billing and rejects retired checkout',async()=>{
 const port=await freePort();
 const child=spawn(process.execPath,['server.js'],{cwd:__dirname,windowsHide:true,stdio:'ignore',env:{...process.env,MONGODB_URI:uri,MONSTER_MONGODB_DATABASE:'monster_play_boot',PORT:String(port),MONSTER_GOOGLE_CLIENT_ID:'fixture',MONSTER_GOOGLE_CLIENT_SECRET:'fixture',JWT_SECRET:'fixture-secret-longer-than-thirty-two-characters',PUBLIC_URL:'https://example.test',MONSTER_PLAY_ENABLED:'false'}});
 try {
  const base=`http://127.0.0.1:${port}`;let ready=false;
  for(let i=0;i<100;i++){try{ready=(await fetch(base+'/health')).ok;}catch{}if(ready)break;await new Promise(r=>setTimeout(r,100));}
  assert.equal(ready,true);
  const health=await (await fetch(base+'/health')).json();assert.equal(health.service,'monstermerge');assert.deepEqual(health.apps,['monstermerge']);
  const start=await fetch(base+'/auth/monster/start',{method:'POST',headers:{'Content-Type':'application/json'},body:'{}'});
  assert.equal(start.status,200);const login=await start.json();assert.match(login.loginId,/^mm_/);assert.ok(login.pollToken);assert.ok(login.authorizationUrl.includes('accounts.google.com'));
  const invalid=await fetch(base+'/auth/monster/start',{method:'POST',headers:{'Content-Type':'application/json'},body:'{"unexpected":true}'});assert.equal(invalid.status,400);
  assert.equal((await (await fetch(base+'/payments/health')).json()).provider,'google-play');assert.equal((await fetch(base+'/api/catalog')).status,404);assert.equal((await fetch(base+'/auth/monster/profile')).status,401);
 }finally{if(child.exitCode===null){const done=once(child,'exit');child.kill();await done;}}
});

test('native login preserves stable game accounts and does not expose secrets',async()=>{
 const {createNativeAuth}=require('./native-auth');
 const schema=new mongoose.Schema({googleSub:{type:String,unique:true},name:String,email:String,playerId:String,provider:String,displayName:String,loginCount:Number,lastLoginAt:Date});
 const model=connection.model('NativeTestPlayer',schema);await model.init();
 const app=express();app.use(express.json());
 app.use('/auth/monster',createNativeAuth({users:()=>model,issueToken:u=>'session-'+u.id,
 env:{MONSTER_GOOGLE_CLIENT_ID:'public-web-id',MONSTER_GOOGLE_CLIENT_SECRET:'must-not-leak'},
 verify:async code=>{if(code!=='fixture-code')throw Object.assign(Error('rejected'),{status:401});return {playerId:'native-player',name:'Dragon'};}}));
 const server=app.listen(0,'127.0.0.1');await once(server,'listening');const base=`http://127.0.0.1:${server.address().port}/auth/monster`;
 try {
  assert.deepEqual(await (await fetch(base+'/config')).json(),{serverClientId:'public-web-id'});
  const login=async code=>fetch(base+'/play-games',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({authorizationCode:code,playerId:'forged-id'})});
  assert.equal((await login('bad-code')).status,401);assert.equal(await model.countDocuments(),0);
  const first=await (await login('fixture-code')).json();const second=await (await login('fixture-code')).json();
  assert.equal(first.status,'signed_in');assert.equal(first.user.id,second.user.id);assert.equal(first.user.email,'');assert.ok(first.token);
  assert.equal(await model.countDocuments(),1);assert.equal((await model.findOne()).googleSub,'playgames:native-player');
 }finally{await new Promise(resolve=>server.close(resolve));}
});
