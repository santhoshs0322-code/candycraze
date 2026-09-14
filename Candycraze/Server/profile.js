const counters = [
  'Coins', 'LevelsStarted', 'LevelsWon', 'LevelsLost', 'TotalMoves', 'BoostersUsed',
  'DailyRewardsClaimed', 'BoosterHammer', 'BoosterRowBlast', 'BoosterShuffle',
  'BoosterExtraMoves', 'BoosterColorBlast'
];
export function defaultSave() {
  return { SchemaVersion: 2, CurrentLevel: 3, TotalStars: 0, LevelEntries: [],
    Coins: 0, Lives: 5, SoundOn: true, MusicOn: true, DailyRewardDay: 0,
    LastLifeLostTicks: 0, LastDailyRewardTicks: 0, LastPlayedLevel: 1,
    PlaySeconds: 0, ...Object.fromEntries(counters.map(key => [key, 0])) };
}
function integer(value, max = 2147483647) {
  if (!Number.isInteger(value) || value < 0 || value > max) throw Object.assign(new Error('Invalid progress value.'), { status: 400 });
  return value;
}
export function validateSave(json) {
  let input;
  try { input = JSON.parse(json); } catch { throw Object.assign(new Error('Invalid save JSON.'), { status: 400 }); }
  if (!input || Array.isArray(input) || typeof input !== 'object') throw Object.assign(new Error('Invalid save.'), { status: 400 });
  const save = defaultSave();
  for (const key of counters) if (input[key] !== undefined) save[key] = integer(input[key]);
  for (const key of ['SoundOn', 'MusicOn', 'StarterBoostersGranted']) if (input[key] !== undefined) {
    if (typeof input[key] !== 'boolean') throw Object.assign(new Error('Invalid preference.'), { status: 400 });
    save[key] = input[key];
  }
  save.CurrentLevel = Math.max(3, integer(input.CurrentLevel ?? 3, 10001));
  save.LastPlayedLevel = Math.max(1, integer(input.LastPlayedLevel ?? 1, 10000));
  save.Lives = integer(input.Lives ?? 5, 5);
  save.DailyRewardDay = integer(input.DailyRewardDay ?? 0, 6);
  for (const key of ['LastLifeLostTicks', 'LastDailyRewardTicks'])
    save[key] = integer(input[key] ?? 0, 3155378975999999999);
  if (input.PlaySeconds !== undefined) {
    if (!Number.isFinite(input.PlaySeconds) || input.PlaySeconds < 0 || input.PlaySeconds > 1e10)
      throw Object.assign(new Error('Invalid play duration.'), { status: 400 });
    save.PlaySeconds = input.PlaySeconds;
  }
  for (const key of ['UpdatedAtUtc', 'AppVersion']) {
    if (input[key] !== undefined && (typeof input[key] !== 'string' || input[key].length > 100))
      throw Object.assign(new Error('Invalid profile metadata.'), { status: 400 });
    save[key] = input[key] ?? '';
  }
  const entries = input.LevelEntries ?? [];
  if (!Array.isArray(entries) || entries.length > 10000) throw Object.assign(new Error('Invalid level history.'), { status: 400 });
  const seen = new Set();
  save.LevelEntries = entries.map(entry => {
    if (!entry || typeof entry !== 'object') throw Object.assign(new Error('Invalid level.'), { status: 400 });
    const n = integer(entry.LevelNumber, 10000);
    if (!n || seen.has(n)) throw Object.assign(new Error('Duplicate or invalid level.'), { status: 400 });
    seen.add(n);
    if (typeof entry.Completed !== 'boolean') throw Object.assign(new Error('Invalid completion flag.'), { status: 400 });
    const timestamp = entry.LastPlayedAtUtc ?? '';
    if (typeof timestamp !== 'string' || timestamp.length > 100) throw Object.assign(new Error('Invalid timestamp.'), { status: 400 });
    const result = { LevelNumber: n, StarsEarned: integer(entry.StarsEarned ?? 0, 3),
      HighScore: integer(entry.HighScore ?? 0), Completed: entry.Completed,
      Attempts: integer(entry.Attempts ?? 0), Wins: integer(entry.Wins ?? 0), LastPlayedAtUtc: timestamp };
    save.TotalStars += result.StarsEarned;
    if (result.Completed) save.CurrentLevel = Math.max(save.CurrentLevel, n + 1);
    return result;
  });
  return save; // Whitelist fields; never permit player IDs/session fields in a save.
}
