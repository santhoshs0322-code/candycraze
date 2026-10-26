const express=require('express');
const crypto=require('node:crypto');
const ids=['dragon','shark','slime','trex','panda','sharkdragon','cyberrex','ninjapanda'];
const fail=(status,message)=>{throw Object.assign(new Error(message),{status});};
const integer=(value,max=2147483647)=>{if(!Number.isSafeInteger(value)||value<0||value>max)fail(400,'Invalid progress value.');return value;};
function validateSave(input,userId,total,accountIds){
 if(!input||typeof input!=='object'||Array.isArray(input)||input.version!==1)fail(400,'Invalid game save.');
 const save={version:1,googleUserId:userId};
 for(const key of ['coins','gems','level','attack','health','startingDna','dailyDay'])save[key]=integer(input[key]??0,key==='level'?10000:['attack','health','startingDna'].includes(key)?50:key==='dailyDay'?6:2147483647);
 save.level=Math.max(1,save.level);
 for(const key of ['unlocked','skins','mastered']){
  const values=input[key]??[];if(!Array.isArray(values)||values.length>100||values.some(v=>typeof v!=='string'||v.length>80))fail(400,'Invalid collection.');save[key]=[...new Set(values)];
 }
 if(save.unlocked.some(id=>!ids.includes(id)))fail(400,'Unknown dragon.');if(!save.unlocked.includes('dragon'))save.unlocked.unshift('dragon');
 save.selected=save.unlocked.includes(input.selected)?input.selected:'dragon';
 save.dragonMastery=[];if(!Array.isArray(input.dragonMastery)||input.dragonMastery.length>8)fail(400,'Invalid dragon stages.');
 for(const row of input.dragonMastery){if(!row||!save.unlocked.includes(row.id)||save.dragonMastery.some(r=>r.id===row.id))fail(400,'Invalid dragon stage.');save.dragonMastery.push({id:row.id,stage:Math.max(1,integer(row.stage,4))});}
 if(typeof input.goldSkin!=='boolean')fail(400,'Invalid skin.');save.goldSkin=input.goldSkin;
 for(const key of ['music','sfx']){if(!Number.isFinite(input[key])||input[key]<0||input[key]>1)fail(400,'Invalid sound setting.');save[key]=input[key];}
 save.fps=input.fps===30?30:60;
 if(typeof input.lastDaily!=='string'||input.lastDaily.length>40)fail(400,'Invalid reward date.');save.lastDaily=input.lastDaily;
 save.paidCreditsApplied=integer(input.paidCreditsApplied??0,Number.MAX_SAFE_INTEGER);
 if(save.paidCreditsApplied!==total)fail(409,'Refresh your verified coin balance before syncing.');
 save.paymentAccountId=input.paymentAccountId||'';
 if(save.paymentAccountId&&!accountIds.includes(save.paymentAccountId))fail(409,'Link your payment account before syncing.');
 return save;
}
function createMonsterProfile({users,verifySession,payments,mode}){
 const router=express.Router(),field=mode==='test'?'testProfile':'profile';
 const wrap=fn=>(req,res,next)=>Promise.resolve().then(()=>fn(req,res)).catch(next);
 router.use(express.json({limit:'48kb'}));
 router.use((req,res,next)=>{res.set('Cache-Control','no-store');next();});
 const owner=async req=>{const token=(req.headers.authorization||'').replace(/^Bearer\s+/i,'');return (await verifySession(token)).sub;};
 async function state(id){
  await payments.syncProfile(id);
  const user=await users().findById(id).lean();if(!user)fail(401,'Account unavailable.');
  const current=user[field]||{},accounts=user.payments?.[mode]?.accounts||[];
  return {user,current,accounts,total:user.payments?.[mode]?.creditedCoinsTotal||0};
 }
 router.get('/',wrap(async(req,res)=>{
  const id=await owner(req);const {current,accounts,total}=await state(id);let save=current.save||null;
  if(save){save={...save};const delta=total-(save.paidCreditsApplied||0);if(delta<0||delta>2147483647-save.coins)fail(409,'Coin balance needs support.');save.coins+=delta;save.paidCreditsApplied=total;}
  res.json({success:true,playerId:id,mode,revision:current.revision||0,save,creditedCoinsTotal:total,accountIds:accounts});
 }));
 router.post('/',wrap(async(req,res)=>{
  const id=await owner(req);const {current,accounts,total}=await state(id);const revision=integer(req.body?.revision);
  const save=validateSave(req.body?.save,id,total,accounts),hash=crypto.createHash('sha256').update(JSON.stringify(save)).digest('hex');
  if(current.revision===revision+1&&current.saveHash===hash)return res.json({success:true,revision:current.revision});
  const revisionMatch=revision===0?{$or:[{[field+'.revision']:{$exists:false}},{[field+'.revision']:0}]}:{[field+'.revision']:revision};
  const updated=await users().findOneAndUpdate({_id:id,...revisionMatch},{$set:{[field+'.save']:save,[field+'.saveHash']:hash,[field+'.lastSavedAt']:new Date()},$inc:{[field+'.revision']:1,saveCount:1}},{new:true});
  if(!updated)fail(409,'Newer cloud progress exists. Sign in again to restore it.');
  res.json({success:true,revision:updated[field].revision});
 }));
 router.use((e,_req,res,_next)=>res.status(e.status||503).json({success:false,error:e.status?e.message:'Cloud save is temporarily unavailable.'}));
 return router;
}
async function migrateMonsterDatabase(source,target){
 if(source.name===target.name)return;
 const migrations=target.db.collection('schema_migrations');
 if(await migrations.findOne({_id:'monster-separate-database-v1'}))return;
 const mappings=[['monstermerge_users','players'],...['test','live'].flatMap(mode=>['accounts','orders','credits'].map(kind=>{const name=`monster_payu_${mode}_${kind}`;return [name,name];}))];
 for(const [from,to] of mappings){
  const cursor=source.db.collection(from).find({});
  for await(const doc of cursor){const {_id,...fields}=doc;await target.db.collection(to).updateOne({_id},{$setOnInsert:fields},{upsert:true});}
 }
 await migrations.updateOne({_id:'monster-separate-database-v1'},{$setOnInsert:{completedAt:new Date(),source:source.name}},{upsert:true});
}
module.exports={validateSave,createMonsterProfile,migrateMonsterDatabase};
