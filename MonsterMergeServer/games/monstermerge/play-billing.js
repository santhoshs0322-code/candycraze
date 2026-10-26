const express = require('express');
const crypto = require('node:crypto');
const mongoose = require('mongoose');
const api = require('./play-api');
class PaymentError extends Error {
  constructor(status, code, message) { super(message); this.status = status; this.code = code; }
}
const fail = (status, message) => { throw new PaymentError(status, 'billing_error', message); };
class PlayBilling {
  constructor({connection, profiles, env = process.env, verify = api.verifyPurchase, consume = api.consumePurchase}) {
    this.connection = connection; this.profiles = profiles; this.verify = verify; this.consume = consume;
    this.config = {mode:'live', enabled:require('./config').configuration(env).enabled};
    this.Receipt = connection.models.MonsterPlayReceipt || connection.model('MonsterPlayReceipt', new mongoose.Schema({
      _id:String, owner:String, productId:String, amount:Number, quantity:Number,
      orderId:String, test:Boolean, consumed:Boolean, grantedAt:Date
    }, {versionKey:false}), 'monster_play_receipts');
  }
  async initialize() { await this.Receipt.init(); }
  // Keep the legacy live cumulative balance and account binding. Never reset paid credits.
  async syncProfile(id) {
    const user = await this.profiles().findById(id).lean();
    if (!user) fail(401, 'Sign in again.');
    const accountId = user.profile?.save?.paymentAccountId || user.payments?.live?.accounts?.[0] || 'play_' + id;
    await this.profiles().updateOne({_id:id}, {$addToSet:{'payments.live.accounts':accountId}});
  }
  async wallet(id) {
    await this.syncProfile(id);
    const user = await this.profiles().findById(id).lean();
    return {success:true, accountId:user.profile?.save?.paymentAccountId || user.payments.live.accounts[0],
      creditedCoinsTotal:user.payments?.live?.creditedCoinsTotal || 0};
  }
  async purchase(id, productId, token) {
    if (!this.config.enabled) fail(503, 'Google Play purchases are not enabled yet.');
    if (!Object.hasOwn(api.coinProducts, productId) || typeof token !== 'string' || token.length < 10 || token.length > 4096)
      fail(400, 'Invalid purchase.');
    const key = crypto.createHash('sha256').update(token).digest('hex');
    let receipt = await this.Receipt.findById(key).lean();
    const check = row => { if (row.owner !== id || row.productId !== productId) fail(403, 'Purchase belongs to another account or product.'); };
    if (receipt) check(receipt);
    else {
      const verified = await this.verify(productId, token, id);
      const amount = api.coinProducts[productId] * verified.quantity;
      if (!Number.isSafeInteger(amount) || amount <= 0 || amount > 1000000) fail(400, 'Invalid quantity.');
      await this.syncProfile(id);
      try {
        await this.connection.transaction(async session => {
          const existing = await this.Receipt.findById(key).session(session).lean();
          if (existing) { check(existing); return; }
          const updated = await this.profiles().updateOne({_id:id, $or:[
            {'payments.live.creditedCoinsTotal':{$exists:false}},
            {'payments.live.creditedCoinsTotal':{$lte:2147483647-amount}}
          ]}, {$inc:{'payments.live.creditedCoinsTotal':amount}}, {session});
          if (updated.matchedCount !== 1) fail(409, 'Coin balance needs support.');
          await this.Receipt.create([{_id:key, owner:id, productId, amount, quantity:verified.quantity,
            orderId:verified.orderId, test:verified.test, consumed:false, grantedAt:new Date()}], {session});
        }, {writeConcern:{w:'majority', j:true}});
      } catch (e) { if (e.code !== 11000) throw e; }
      receipt = await this.Receipt.findById(key).lean();
      if (!receipt) fail(409, 'Purchase not saved. Restore purchases to retry.');
      check(receipt);
    }
    // A lost consume response is retried; the durable receipt prevents a second grant.
    if (!receipt.consumed) {
      await this.consume(productId, token);
      await this.Receipt.updateOne({_id:key}, {$set:{consumed:true}});
    }
    return this.wallet(id);
  }
}
function createPayments(options) {
  const service = new PlayBilling(options), router = express.Router();
  router.use('/api/billing', express.json({limit:'12kb'}));
  const wrap = fn => (req,res,next) => Promise.resolve().then(()=>fn(req,res)).catch(next);
  const owner = async req => (await options.authenticateGoogle((req.headers.authorization || '').replace(/^Bearer\s+/i,''))).sub;
  router.use('/api/billing', (_req,res,next)=>{res.set('Cache-Control','no-store');next();});
  router.get('/payments/health', (_req,res)=>res.json({ok:true, provider:'google-play', paymentsEnabled:service.config.enabled}));
  router.get('/api/billing/wallet', wrap(async(req,res)=>res.json(await service.wallet(await owner(req)))));
  router.post('/api/billing/account', wrap(async(req,res)=>{
    const id = await owner(req);
    if (!service.config.enabled) fail(503, 'Google Play purchases are not enabled yet.');
    res.json({success:true, accountId:api.billingAccountId(id)});
  }));
  router.post('/api/billing/verify', wrap(async(req,res)=>res.json(await service.purchase(await owner(req),req.body?.productId,req.body?.purchaseToken))));
  router.use((e,_req,res,_next)=>res.status(e.status||503).json({success:false,error:e.status?e.message:'Purchase verification unavailable. Restore purchases to retry.'}));
  return {router, service, initialize:()=>service.initialize()};
}
module.exports = {createPayments, PlayBilling, PaymentError};
