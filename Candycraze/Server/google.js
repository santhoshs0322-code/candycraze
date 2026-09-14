export async function verifyPlayGamesCode(code, env = process.env, fetcher = fetch) {
  if (typeof code !== 'string' || code.length < 5 || code.length > 4096)
    throw Object.assign(new Error('Invalid authorization code.'), { status: 400 });
  const tokenResponse = await fetcher('https://oauth2.googleapis.com/token', {
    method: 'POST', signal: AbortSignal.timeout(15000),
    headers: { 'content-type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({ code, client_id: env.GOOGLE_CLIENT_ID,
      client_secret: env.GOOGLE_CLIENT_SECRET, grant_type: 'authorization_code', redirect_uri: '' })
  });
  if (!tokenResponse.ok) throw Object.assign(new Error('Google authorization was rejected. Please sign in again.'), { status: 401 });
  const token = await tokenResponse.json();
  if (!token.access_token) throw Object.assign(new Error('Google returned no access token.'), { status: 401 });
  const playerResponse = await fetcher('https://games.googleapis.com/games/v1/players/me', {
    headers: { Authorization: 'Bearer ' + token.access_token }, signal: AbortSignal.timeout(15000)
  });
  if (!playerResponse.ok) throw Object.assign(new Error('Play Games player could not be verified.'), { status: 401 });
  const player = await playerResponse.json();
  if (typeof player.playerId !== 'string' || !player.playerId || player.playerId.length > 200)
    throw Object.assign(new Error('Invalid Play Games identity.'), { status: 401 });
  // No Google access/refresh tokens, auth codes, email, or contacts are stored.
  return { playerId: player.playerId, displayName: String(player.displayName ?? 'Player').slice(0, 100) };
}
