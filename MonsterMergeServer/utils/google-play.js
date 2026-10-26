const { createSign, createHash } = require('node:crypto');
// One instance per game: package, account namespace, credentials and token cache never cross games.
function createGooglePlayApi({packageName, accountNamespace, env = {}, fetchImpl = fetch}) {
  if (!/^[a-zA-Z][a-zA-Z0-9_]*(\.[a-zA-Z][a-zA-Z0-9_]*)+$/.test(packageName || '') || !/^[a-z0-9_-]+$/.test(accountNamespace || '')) throw new Error('Invalid game billing configuration');
  const billingAccountId = playerId => createHash('sha256').update(accountNamespace + ':' + playerId).digest('hex');
let cachedToken, expiresAt = 0;
async function accessToken() {
  if (cachedToken && Date.now() < expiresAt) return cachedToken;
  const email = env.PLAY_SERVICE_ACCOUNT_EMAIL;
  const key = env.PLAY_SERVICE_ACCOUNT_PRIVATE_KEY?.replace(/\\n/g, '\n');
  if (!email || !key) throw new Error('Play billing credentials missing');
  const encode = value => Buffer.from(JSON.stringify(value)).toString('base64url');
  const iat = Math.floor(Date.now() / 1000);
  const unsigned = encode({ alg: 'RS256', typ: 'JWT' }) + '.' + encode({ iss: email,
    scope: 'https://www.googleapis.com/auth/androidpublisher', aud: 'https://oauth2.googleapis.com/token', iat, exp: iat + 3600 });
  const signer = createSign('RSA-SHA256'); signer.update(unsigned); signer.end();
  const assertion = unsigned + '.' + signer.sign(key, 'base64url');
  const response = await fetchImpl('https://oauth2.googleapis.com/token', { method: 'POST',
    headers: { 'content-type': 'application/x-www-form-urlencoded' }, signal: AbortSignal.timeout(15000),
    body: new URLSearchParams({ grant_type: 'urn:ietf:params:oauth:grant-type:jwt-bearer', assertion }) });
  if (!response.ok) throw new Error('Play billing service authorization failed');
  const body = await response.json();
  if (!body.access_token) throw new Error('Play billing token unavailable');
  cachedToken = body.access_token; expiresAt = Date.now() + Math.max(0, (body.expires_in - 120) * 1000);
  return cachedToken;
}
async function google(path, method = 'GET') {
  const response = await fetchImpl('https://androidpublisher.googleapis.com/androidpublisher/v3/applications/' +
    encodeURIComponent(packageName) + path, { method,
    headers: { Authorization: 'Bearer ' + await accessToken() }, signal: AbortSignal.timeout(15000) });
  const text = await response.text();
  if (!response.ok) {
    if (response.status === 401) { cachedToken = null; expiresAt = 0; }
    let reason = '';
    try {
      const body = JSON.parse(text);
      reason = String(body?.error?.message || body?.error?.status || '').replace(/[\r\n]/g, ' ').slice(0, 300);
    } catch { }
    console.error(`[PLAY-API] request rejected status=${response.status} reason=${reason || 'not provided'}`);
    throw new Error('Play purchase API request failed: ' + response.status + (reason ? ' - ' + reason : ''));
  }
  return method === 'GET' && text ? JSON.parse(text) : null;
}
async function verifyPurchase(productId, token, playerId) {
  const purchase = await google('/purchases/productsv2/tokens/' + encodeURIComponent(token));
  return validatePurchase(purchase, productId, playerId);
}
function validatePurchase(purchase, productId, playerId) {
  const fail = message => { throw Object.assign(new Error(message), { status: 400 }); };
  if (purchase.purchaseStateContext?.purchaseState !== 'PURCHASED') fail('Payment is pending or cancelled. No items credited.');
  if (purchase.obfuscatedExternalAccountId !== billingAccountId(playerId)) fail('Sign in to the account used for this purchase.');
  if (purchase.productLineItem?.length !== 1 || purchase.productLineItem[0].productId !== productId)
    fail('Purchase product did not match.');
  const offer = purchase.productLineItem[0].productOfferDetails;
  const quantity = offer?.quantity;
  if (!Number.isSafeInteger(quantity) || quantity < 1 || quantity > 100) fail('Unsupported purchase quantity.');
  if (offer.refundableQuantity !== undefined && offer.refundableQuantity !== quantity) fail('Purchase has been refunded.');
  if (offer.consumptionState === 'CONSUMPTION_STATE_CONSUMED') fail('Purchase was already consumed. Contact support.');
  return { quantity, orderId: purchase.orderId ?? '', test: !!purchase.testPurchaseContext };
}
async function consumePurchase(productId, token) {
  // Consumption can have succeeded even when a previous HTTP response was lost.
  const purchase = await google('/purchases/productsv2/tokens/' + encodeURIComponent(token));
  if (purchase.productLineItem?.[0]?.productOfferDetails?.consumptionState === 'CONSUMPTION_STATE_CONSUMED') return;
  await google('/purchases/products/' + encodeURIComponent(productId) + '/tokens/' + encodeURIComponent(token) + ':consume', 'POST');
}


return {billingAccountId, verifyPurchase, consumePurchase, validatePurchase};
}
module.exports = {createGooglePlayApi};
