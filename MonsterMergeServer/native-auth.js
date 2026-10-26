const express=require('express');
async function verifyPlayGamesCode(code,env=process.env,fetcher=fetch) {
  if(typeof code!=='string'||code.length<5||code.length>4096)throw Object.assign(Error('Invalid authorization code.'),{status:400});
  const response=await fetcher('https://oauth2.googleapis.com/token',{method:'POST',signal:AbortSignal.timeout(15000),
    headers:{'Content-Type':'application/x-www-form-urlencoded'},body:new URLSearchParams({code,
      client_id:env.MONSTER_GOOGLE_CLIENT_ID,client_secret:env.MONSTER_GOOGLE_CLIENT_SECRET,
      grant_type:'authorization_code',redirect_uri:''})});
  if(!response.ok)throw Object.assign(Error('Google authorization rejected.'),{status:401});
  const token=await response.json();
  if(!token.access_token)throw Object.assign(Error('Google authorization rejected.'),{status:401});
  const result=await fetcher('https://games.googleapis.com/games/v1/players/me',{signal:AbortSignal.timeout(15000),headers:{Authorization:'Bearer '+token.access_token}});
  if(!result.ok)throw Object.assign(Error('Play Games identity unavailable.'),{status:401});
  const player=await result.json();
  if(typeof player.playerId!=='string'||!player.playerId||player.playerId.length>200)throw Object.assign(Error('Invalid player.'),{status:401});
  return {playerId:player.playerId,name:String(player.displayName||'Dragon Explorer').slice(0,100)};
}
function createNativeAuth({users,issueToken,verify=verifyPlayGamesCode,env=process.env,now=Date.now}) {
  const router=express.Router(),starts=new Map();
  router.use((_req,res,next)=>{res.set('Cache-Control','no-store');next();});
  router.get('/config',(_req,res)=>res.json({serverClientId:env.MONSTER_GOOGLE_CLIENT_ID||''}));
  router.post('/play-games',async(req,res)=>{
    const time=now(),key=req.ip,old=starts.get(key),bucket=old&&time-old.time<60000?old:{time,count:0};
    if(starts.size>=10000&&!starts.has(key))starts.delete(starts.keys().next().value);
    starts.set(key,bucket);
    if(++bucket.count>6)return res.status(429).json({error:'Please wait before retrying sign-in.'});
    try {
      const identity=await verify(req.body?.authorizationCode);
      // Play Games IDs are distinct from Google OpenID subjects. Never merge by name/email.
      const googleSub='playgames:'+identity.playerId;
      const update={$set:{playerId:identity.playerId,displayName:identity.name,name:identity.name,provider:'play-games',lastLoginAt:new Date(time)},$setOnInsert:{email:''},$inc:{loginCount:1}};
      let user;
      try {user=await users().findOneAndUpdate({googleSub},update,{upsert:true,new:true,setDefaultsOnInsert:true});}
      catch(error) {if(error.code!==11000)throw error;user=await users().findOneAndUpdate({googleSub},update,{new:true});}
      res.json({status:'signed_in',token:issueToken(user,'monstermerge'),user:{id:user.id,name:user.name,email:user.email||''}});
    } catch(error) {const status=[400,401].includes(error.status)?error.status:503;res.status(status).json({error:status===400?'Invalid authorization code.':status===401?'Play Games sign-in was rejected. Please retry.':'Sign-in is temporarily unavailable.'});}
  });
  return router;
}
module.exports={verifyPlayGamesCode,createNativeAuth};
