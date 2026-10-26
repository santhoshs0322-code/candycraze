// Monster Merge only. Add a separate games/<game>/config.js for another game.
const packageName = 'com.yourstudio.monstermerge';
const coinProducts = Object.freeze({starter:500, adventurer:1500, champion:4000, legend:10000});
function configuration(env = process.env) {
  return {packageName, accountNamespace:'monstermerge', enabled:env.MONSTER_PLAY_ENABLED === 'true',
    env:{PLAY_SERVICE_ACCOUNT_EMAIL:env.MONSTER_PLAY_SERVICE_ACCOUNT_EMAIL,
      PLAY_SERVICE_ACCOUNT_PRIVATE_KEY:env.MONSTER_PLAY_SERVICE_ACCOUNT_PRIVATE_KEY}};
}
module.exports = {packageName, coinProducts, configuration};
