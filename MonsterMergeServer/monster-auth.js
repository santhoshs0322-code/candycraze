// Browser Google sign-in with a private, short-lived app polling secret.
// No app session token is placed in a redirect URL or exposed to the browser.
const crypto=require('node:crypto');
const express=require('express');
const digest=value=>crypto.createHash('sha256').update(value).digest('hex');
const text=value=>typeof value==='string';
const page=(ok)=>`<!doctype html><meta name="viewport" content="width=device-width,initial-scale=1"><title>Monster Merge sign-in</title><style>body{font:18px system-ui;background:#081c29;color:white;padding:32px}main{max-width:480px;margin:15vh auto}h1{color:#76e1d0}</style><main><h1>${ok?'Signed in to Monster Merge':'Sign-in not completed'}</h1><p>${ok?'Return to the game to continue your purchase.':'Return to the game and try Google sign-in again.'}</p></main>`;

function createMonsterAuth({connection,google,clientId,users,issueToken,verifySession,now=Date.now}) {
  const schema=new connection.base.Schema({_id:String,pollHash:String,nonce:String,
    expiresAt:{type:Date,expires:0},status:{type:String,default:'pending'},userId:String},{versionKey:false});
  const Login=connection.models.MonsterGoogleLogin||connection.model('MonsterGoogleLogin',schema,'monster_google_logins');
  const router=express.Router(),starts=new Map();
  router.use((req,res,next)=>{res.set({'Cache-Control':'no-store','Referrer-Policy':'no-referrer','X-Frame-Options':'DENY'});next();});
  router.use(express.json({limit:'4kb'}));
  const wrap=fn=>(req,res,next)=>Promise.resolve().then(()=>fn(req,res)).catch(next);
  router.post('/start',wrap(async(req,res)=>{
    if(!req.body||Array.isArray(req.body)||Object.keys(req.body).length)return res.status(400).json({error:'Send an empty object.'});
    const key=req.ip,previous=starts.get(key),time=now();
    const bucket=previous&&time-previous.time<60000?previous:{time,count:0};
    if(++bucket.count>6)return res.status(429).json({error:'Please wait before trying again.'});
    if(starts.size>=10000&&!starts.has(key))starts.delete(starts.keys().next().value);
    starts.set(key,bucket);
    const loginId='mm_'+crypto.randomBytes(32).toString('hex'),pollToken=crypto.randomBytes(32).toString('base64url');
    const nonce=crypto.randomBytes(32).toString('hex');
    await Login.create({_id:loginId,pollHash:digest(pollToken),nonce,expiresAt:new Date(now()+600000)});
    const authorizationUrl=google.generateAuthUrl({access_type:'online',scope:['openid','email','profile'],
      state:loginId,nonce,prompt:'select_account'});
    res.json({loginId,pollToken,authorizationUrl});
  }));
  router.post('/poll',wrap(async(req,res)=>{
    const {loginId,pollToken}=req.body||{};
    if(!text(loginId)||!/^mm_[a-f0-9]{64}$/.test(loginId)||!text(pollToken)||!/^[A-Za-z0-9_-]{43}$/.test(pollToken))
      return res.status(401).json({error:'Invalid sign-in request.'});
    const filter={_id:loginId,pollHash:digest(pollToken),expiresAt:{$gt:new Date(now())}};
    const current=await Login.findOne(filter).lean();
    if(!current)return res.status(410).json({error:'Sign-in expired. Try again.'});
    if(current.status!=='ready')return res.json({status:current.status==='failed'?'failed':'pending'});
    const claimed=await Login.findOneAndDelete({...filter,status:'ready'}).lean();
    if(!claimed)return res.status(410).json({error:'Sign-in already completed.'});
    const user=await users().findById(claimed.userId);
    if(!user)return res.status(401).json({error:'Account unavailable.'});
    res.json({status:'signed_in',token:issueToken(user,'monstermerge'),user:{id:user.id,name:user.name,email:user.email}});
  }));
  router.get('/me',wrap(async(req,res)=>{
    const token=(req.headers.authorization||'').replace(/^Bearer\s+/i,'');
    const identity=await verifySession(token);
    const user=await users().findById(identity.sub).lean();
    if(!user)return res.status(401).json({error:'Account unavailable.'});
    res.json({status:'signed_in',user:{id:String(user._id),name:user.name,email:user.email}});
  }));
  router.use((error,_req,res,_next)=>res.status(error.status===401?401:500).json({error:error.status===401?'Sign in with Google to purchase.':'Google sign-in unavailable. Try again.'}));

  async function callback(req,res) {
    res.set({'Cache-Control':'no-store','Referrer-Policy':'no-referrer','X-Frame-Options':'DENY'});
    const state=req.query.state;
    if(!text(state)||!/^mm_[a-f0-9]{64}$/.test(state))return res.status(400).type('html').send(page(false));
    const attempt=await Login.findOneAndUpdate({_id:state,status:'pending',expiresAt:{$gt:new Date(now())}},
      {$set:{status:'processing'}},{new:true}).lean();
    if(!attempt)return res.status(400).type('html').send(page(false));
    try {
      if(!text(req.query.code)||!req.query.code||req.query.error)throw new Error('Cancelled sign-in');
      const {tokens}=await google.getToken(req.query.code);
      const ticket=await google.verifyIdToken({idToken:tokens.id_token,audience:clientId});
      const profile=ticket.getPayload();
      if(!profile||!profile.sub||profile.email_verified!==true||profile.nonce!==attempt.nonce)throw new Error('Invalid identity');
      const user=await users().findOneAndUpdate({googleSub:profile.sub},{$set:{email:profile.email,
        playerId:profile.sub,displayName:profile.name||profile.email,provider:'google',
        name:profile.name||profile.email,picture:profile.picture||'',lastLoginAt:new Date(now())},$inc:{loginCount:1}},
        {new:true,upsert:true,setDefaultsOnInsert:true});
      const completed=await Login.updateOne({_id:state,status:'processing',expiresAt:{$gt:new Date(now())}},{$set:{status:'ready',userId:user.id}});
      if(completed.modifiedCount!==1)throw new Error('Sign-in expired');
      res.type('html').send(page(true));
    } catch(_) {
      await Login.updateOne({_id:state},{$set:{status:'failed'}});
      res.status(400).type('html').send(page(false));
    }
  }
  return {router,callback,Login,initialize:()=>Login.init()};
}
module.exports={createMonsterAuth};
