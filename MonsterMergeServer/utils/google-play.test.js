const {test}=require('node:test');
const assert=require('node:assert/strict');
const {generateKeyPairSync}=require('node:crypto');
const {createGooglePlayApi}=require('./google-play');
const {configuration}=require('../games/monstermerge/config');
test('each game has its own account namespace and package configuration',()=>{
 const a=createGooglePlayApi({packageName:'com.example.first',accountNamespace:'first'});
 const b=createGooglePlayApi({packageName:'com.example.second',accountNamespace:'second'});
 assert.notEqual(a.billingAccountId('same-player'),b.billingAccountId('same-player'));
 const purchase={purchaseStateContext:{purchaseState:'PURCHASED'},obfuscatedExternalAccountId:a.billingAccountId('same-player'),productLineItem:[{productId:'pack',productOfferDetails:{quantity:1}}]};
 assert.equal(a.validatePurchase(purchase,'pack','same-player').quantity,1);
 assert.throws(()=>b.validatePurchase(purchase,'pack','same-player'));
 assert.throws(()=>createGooglePlayApi({packageName:'bad/path',accountNamespace:'first'}));
});
test('Monster Merge never inherits another games enable flag or credentials',()=>{
 const config=configuration({PAYMENTS_ENABLED:'true',CLASSIC_PLAY_ENABLED:'true',PLAY_SERVICE_ACCOUNT_EMAIL:'other-game'});
 assert.equal(config.enabled,false);assert.equal(config.env.PLAY_SERVICE_ACCOUNT_EMAIL,undefined);
 assert.equal(configuration({MONSTER_PLAY_ENABLED:'true'}).enabled,true);
});
test('credential token caches and Publisher requests are isolated per game',async()=>{
 const {privateKey}=generateKeyPairSync('rsa',{modulusLength:2048});const key=privateKey.export({type:'pkcs8',format:'pem'});
 let grants=0;const calls=[];
 const fake=async(url,options)=>{
  if(url==='https://oauth2.googleapis.com/token'){grants++;const assertion=options.body.get('assertion');const claim=JSON.parse(Buffer.from(assertion.split('.')[1],'base64url'));return {ok:true,json:async()=>({access_token:claim.iss,expires_in:3600})};}
  calls.push([url,options.headers.Authorization]);
  return {ok:true,text:async()=>JSON.stringify({productLineItem:[{productOfferDetails:{consumptionState:'CONSUMPTION_STATE_CONSUMED'}}]})};
 };
 const a=createGooglePlayApi({packageName:'com.example.first',accountNamespace:'first',env:{PLAY_SERVICE_ACCOUNT_EMAIL:'first@example.test',PLAY_SERVICE_ACCOUNT_PRIVATE_KEY:key},fetchImpl:fake});
 const b=createGooglePlayApi({packageName:'com.example.second',accountNamespace:'second',env:{PLAY_SERVICE_ACCOUNT_EMAIL:'second@example.test',PLAY_SERVICE_ACCOUNT_PRIVATE_KEY:key},fetchImpl:fake});
 await a.consumePurchase('pack','token');await a.consumePurchase('pack','token');await b.consumePurchase('pack','token');
 assert.equal(grants,2);assert.match(calls[0][0],/applications\/com.example.first\//);assert.equal(calls[0][1],'Bearer first@example.test');assert.match(calls[2][0],/applications\/com.example.second\//);assert.equal(calls[2][1],'Bearer second@example.test');
});
