using UnityEngine;
namespace MonsterMerge
{
    public sealed class GameplayController : MonoBehaviour
    {
        public GameState State { get; private set; } = GameState.Preparing;
        public PlayerMonster Player { get; private set; }
        public LevelDefinition Level { get; private set; }
        public float BossHealth { get; private set; }
        public float BossMaxHealth { get; private set; }
        public bool Revived { get; private set; }
        public bool Doubled { get; private set; }
        public int RewardCoins { get; private set; }
        public string UnlockedMonster { get; private set; }
        public int DifficultyLevel { get; private set; }
        public int AvailableBoostPower { get; private set; }
        public int EarnedBoostPower { get; private set; }
        public int RequiredBoostPower => RunRules.RequiredPower(AvailableBoostPower);
        public float BoostProgress => AvailableBoostPower == 0 ? 0 : Mathf.Clamp01((float)EarnedBoostPower/AvailableBoostPower);
        public bool BossWardBroken => EarnedBoostPower >= RequiredBoostPower;
        public LevelTheme Theme => LevelTheme.For(DifficultyLevel);
        public MonsterVisual PlayerVisual => visual;
        public MonsterVisual BossVisual => bossVisual;
        public string BossName => Theme.boss;
        public float Progress => Mathf.Clamp01(Player.transform.position.z / Level.length);
        GameApp app;
        RunnerMotor motor;
        MonsterVisual visual, bossVisual;
        FollowCamera cameraRig;
        FeedbackPool feedback;
        BattleEffects battleEffects;
        GameUI ui;
        float evolveUntil, nextPlayerAttack, nextBossAttack, bossElapsed;
        public float BossSecondsRemaining => Mathf.Max(0,RunRules.BossBattleSeconds-bossElapsed);
        bool playerWindingUp, bossWindingUp;
        bool losingPower;
        float speedEffectUntil;
        GameState resumeState, defeatState;
        bool rewardCommitted;
        int bankedRunCoins;
        float countdownRemaining=3;
        bool countdownPaused, countdownUnfocused;
        public bool IsBoss => State == GameState.BossBattle || (State == GameState.Defeat && defeatState == GameState.BossBattle);
        void Start()
        {
            app = GameApp.Instance;
            if (!app) { UnityEngine.SceneManagement.SceneManager.LoadScene("Bootstrap"); return; }
            // A new run owns its clock, including direct scene entry after a paused run.
            Time.timeScale = 1;
            DifficultyLevel=app.ConsumeLevel();
            Level = LevelChallenge.CreateRuntime(app.catalog.levels[(DifficultyLevel - 1) % app.catalog.levels.Length],DifficultyLevel);
             AvailableBoostPower=RunRules.AvailablePower(Level,app.catalog);
            var definition = app.catalog.Monster(app.Save.Data.selected);
            var player = new GameObject("Player");
            var controller = player.AddComponent<CharacterController>(); controller.center = Vector3.up; controller.height = 2; controller.radius = .38f;
            player.AddComponent<RunnerInput>().Sensitivity = app.catalog.dragSensitivity;
            motor = player.AddComponent<RunnerMotor>(); motor.HalfWidth = app.catalog.trackHalfWidth; motor.Speed = definition.baseSpeed;
            Player = player.AddComponent<PlayerMonster>();
            Player.Initialize(definition, app.catalog, app.Save.Data, app.BonusDna ? 20 : 0); app.BonusDna = false;
            visual = Instantiate(definition.prefab, player.transform).GetComponent<MonsterVisual>();
            RefreshVisual(); Player.Evolved += Evolve;
            cameraRig = Camera.main.GetComponent<FollowCamera>(); cameraRig.target = player.transform;
            cameraRig.transform.position = new Vector3(0,6.8f,-10.8f);
            cameraRig.transform.LookAt(new Vector3(0,.6f,5));
            feedback = new GameObject("Feedback pool").AddComponent<FeedbackPool>(); feedback.Initialize(app.catalog.baseMaterial);
            new GameObject("Modular track").AddComponent<TrackWorld>().Build(this,app.catalog,Level);
            var boss = Instantiate(Level.boss.prefab,new Vector3(0,0,Level.length + 6),Quaternion.Euler(0,180,0));
            bossVisual = boss.GetComponent<MonsterVisual>();
            bossVisual.ConfigureBoss(DifficultyLevel,Theme.accent);
            boss.AddComponent<BossWard>().Initialize(this,app.catalog);
            battleEffects = gameObject.AddComponent<BattleEffects>(); battleEffects.Initialize(app.catalog,visual.transform,boss.transform);
            BossMaxHealth = RunRules.BossHealth(DifficultyLevel); BossHealth = BossMaxHealth;
            ui = gameObject.AddComponent<GameUI>(); ui.Initialize(app,this);
            State = GameState.Preparing; motor.Running = false;motor.Input.enabled=false;
            ui.ShowCountdown(3);
        }
        void Update()
        {
            if (!Player) return;
            if(State==GameState.Preparing)
            {
                if(countdownPaused || countdownUnfocused)return;
                countdownRemaining=Mathf.Max(0,countdownRemaining-Mathf.Min(Time.unscaledDeltaTime,.1f));
                ui.UpdateCountdown(countdownRemaining);
                if(countdownRemaining<=0)
                {
                    State=GameState.Running;motor.Input.enabled=true;motor.Running=true;
                    ui.ShowHud();ui.Toast("GO!",.65f);
                    app.Analytics.Track("level_started",DifficultyLevel.ToString());
                }
                return;
            }
            visual.SetFlying(Player.DNA >= Mathf.CeilToInt(AvailableBoostPower*.55f));
            bool bossFlight=DifficultyLevel>=3 && (DifficultyLevel>=7 || Mathf.Sin(Time.time*(.5f+DifficultyLevel*.03f))>-.2f);
            bossVisual.SetFlying(bossFlight,.7f+Mathf.Min(DifficultyLevel,15)*.09f);
            cameraRig.altitude=IsBoss ? (visual.FlightHeight+bossVisual.FlightHeight)*.5f : visual.FlightHeight;
            if (speedEffectUntil > 0 && Time.time >= speedEffectUntil) { motor.Speed = Player.Speed; speedEffectUntil = 0; }
            if (State == GameState.Evolving && Time.unscaledTime >= evolveUntil) { Time.timeScale = 1; State = GameState.Running; }
            if ((State == GameState.Running || State == GameState.Evolving) && Player.transform.position.z >= Level.length) BeginBoss();
            if (State == GameState.BossBattle)
            {
                bossElapsed+=Time.unscaledDeltaTime;
                if(BossSecondsRemaining<=0) { Lose();return; }
                if (!playerWindingUp && bossElapsed >= nextPlayerAttack - .3f) { visual.Attack(); battleEffects.Breathe(false,app.catalog.ElementData(Player.CurrentElement).color); playerWindingUp = true; }
                if (!bossWindingUp && bossElapsed >= nextBossAttack - .3f) { bossVisual.Attack(); battleEffects.Breathe(true,Theme.accent); bossWindingUp = true; }
                if (bossElapsed >= nextPlayerAttack)
                {
                    playerWindingUp = false;
                    nextPlayerAttack += .85f;
                    float hit=BossWardBroken ? Player.ResolveAttackDamage()*RunRules.BossBattleDamageMultiplier : 0;
                    BossHealth = Mathf.Max(0,BossHealth-hit);
                    battleEffects.Impact(bossVisual.transform.position + Vector3.up * (1.5f+bossVisual.FlightHeight),app.catalog.ElementData(Player.CurrentElement).color);
                    bossVisual.Hit(); cameraRig.Punch(); app.Audio.Play(app.catalog.attack);
                    ui.WorldNumber(bossVisual.transform.position+Vector3.up*(2.8f+bossVisual.FlightHeight),BossWardBroken ? "-"+Mathf.RoundToInt(hit) : "SHIELDED",BossWardBroken ? new Color(1,.82f,.35f) : Color.cyan);
                    if (BossHealth <= 0) { Win(); return; }
                }
                if (bossElapsed >= nextBossAttack)
                {
                    bossWindingUp = false;
                    nextBossAttack += RunRules.BossInterval(DifficultyLevel);
                    Hurt(RunRules.BossDamage(DifficultyLevel)*RunRules.BossBattleDamageMultiplier);
                }
            }
            if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        }
        void BeginBoss()
        {
            Time.timeScale = 1; State = GameState.BossBattle; motor.Running = false;
            cameraRig.combat = true;
            // Close the visual combat gap while keeping runner collision/progression coordinates intact.
            bossVisual.transform.position = new Vector3(Player.transform.position.x, 0, Player.transform.position.z + 5.6f);
            bossElapsed=0;nextPlayerAttack=.65f;nextBossAttack=1.1f;
            playerWindingUp=bossWindingUp=false;
            app.Analytics.Track("boss_started",BossName); ui.Toast(BossWardBroken ? "SHIELD BROKEN" : "NEED 90% BOOSTS TO BREAK SHIELD",1.3f);
        }
        public void CollectDna(int amount)
        { EarnedBoostPower=Mathf.Clamp(EarnedBoostPower+amount,0,AvailableBoostPower); Player.AddDna(amount); RefreshVisual(); feedback.Emit(Player.transform.position + Vector3.up*(1+visual.FlightHeight),Color.cyan,5); app.Audio.Play(app.catalog.dna); }
        public void CollectCoins(int amount)
        { if(State!=GameState.Running && State!=GameState.Evolving)return; Player.CollectCoin(amount); ui.CoinPulse(); app.Audio.Play(app.catalog.coin); }
        public void ChangePower(int amount)
        {
            if (State != GameState.Running && State != GameState.Evolving) return;
            losingPower = amount < 0; int previous = Player.DNA;
            EarnedBoostPower=Mathf.Clamp(EarnedBoostPower+amount,0,AvailableBoostPower);
            Player.AddDna(amount); RefreshVisual();
            if (losingPower) visual.Penalty(); else visual.Burst();
            feedback.Emit(Player.transform.position+Vector3.up,losingPower ? new Color(1,.2f,.12f) : new Color(.25f,1,.6f),26);
            ui.WorldNumber(Player.transform.position+Vector3.up*2.7f,(amount > 0 ? "+" : "")+(Player.DNA-previous)+" POWER",losingPower ? new Color(1,.3f,.2f) : new Color(.35f,1,.65f));
            motor.Speed = Player.Speed * (losingPower ? .68f : 1.22f); speedEffectUntil = Time.time + (losingPower ? 1 : 2.5f);
            cameraRig.Punch(); app.Audio.Play(losingPower ? app.catalog.damage : app.catalog.evolution); losingPower = false;
        }
        public void SelectGate(Element element)
        {
            EarnedBoostPower=Mathf.Clamp(EarnedBoostPower+app.catalog.gateDna,0,AvailableBoostPower);
            Player.Mutate(element);
            if (app.ExtraMutation) { app.ExtraMutation = false; Player.AddDna(app.catalog.gateDna); }
            app.Analytics.Track("mutation_selected",element.ToString());
        }
        void RefreshVisual() { visual.Apply(Player.Definition,app.catalog,Player.CurrentElement,Player.EvolutionLevel,app.Save.Data.goldSkin); visual.SetPower(Player.DNA); }
        void Evolve()
        {
            RefreshVisual(); if (losingPower) visual.Penalty(); else visual.Burst(); cameraRig.Punch();
            feedback.Emit(Player.transform.position + Vector3.up,app.catalog.ElementData(Player.CurrentElement).color,24);
            if (State == GameState.Running || State == GameState.Evolving)
            { State = GameState.Evolving; Time.timeScale = .55f; evolveUntil = Time.unscaledTime + .45f; }
            if (!losingPower) ui.Toast(Player.EvolutionName,1.2f); app.Audio.Play(app.catalog.evolution);
            app.Analytics.Track("evolution_reached",Player.EvolutionName);
        }
        public void Hurt(float amount)
        {
            if (State != GameState.Running && State != GameState.Evolving && State != GameState.BossBattle) return;
            Player.TakeDamage(amount); visual.Hit(); app.Audio.Play(app.catalog.damage);
            battleEffects.Impact(Player.transform.position+Vector3.up*(1+visual.FlightHeight),new Color(1,.3f,.12f));
            if (Player.CurrentHealth <= 0) Lose();
        }
        int BankCollectedCoins()
        {
            int earned=RunRules.UnbankedCoins(Player.CoinsCollected,bankedRunCoins);
            bankedRunCoins=Player.CoinsCollected;
            app.Save.Data.coins+=earned;
            return earned;
        }
        public void Win()
        {
            if (rewardCommitted || State == GameState.Victory) return;
            Time.timeScale = 1; motor.Running = false; State = GameState.Victory; rewardCommitted = true;
            visual.Celebrate(); bossVisual.SetDefeated(true);
            int completionBonus=RunRules.CompletionCoins(Level.completionCoins);
            RewardCoins = completionBonus + Player.CoinsCollected;
            BankCollectedCoins();
            app.Save.Data.coins += completionBonus; app.Save.Data.level=Mathf.Max(app.Save.Data.level,DifficultyLevel+1);
            string mastery = Player.Definition.id + ":" + Player.CurrentElement;
            if (!app.Save.Data.mastered.Contains(mastery)) app.Save.Data.mastered.Add(mastery);
            app.Save.Save(); app.Analytics.Track("boss_defeated",Level.boss.displayName);
            app.Analytics.Track("level_completed",Level.number.ToString()); app.Audio.Play(app.catalog.victory);
            feedback.Emit(Player.transform.position + Vector3.up * 2,Color.yellow,40); ui.ShowResult();
        }
        public void Lose()
        {
            if (State == GameState.Defeat || State == GameState.Victory) return;
            defeatState = State == GameState.BossBattle ? GameState.BossBattle : GameState.Running;
            Time.timeScale = 1; motor.Running = false; State = GameState.Defeat;
            visual.SetDefeated(true);
            BankCollectedCoins();RewardCoins=Player.CoinsCollected;app.Save.Save();
            app.Analytics.Track("level_failed"); app.Audio.Play(app.catalog.defeat); ui.ShowResult();
        }
        public void Revive()
        {
            if (Revived || State != GameState.Defeat || (IsBoss && !BossWardBroken)) return;
            app.Reward("revive", () =>
            {
                if (State != GameState.Defeat || Revived) return;
                Revived = true; Player.Revive(); State = defeatState; motor.Running = State == GameState.Running;
                visual.SetDefeated(false); playerWindingUp = bossWindingUp = false;
                bossElapsed=0;nextPlayerAttack=.65f;
                nextBossAttack=1.1f; ui.ShowHud();
            });
        }
        public void DoubleCoins()
        {
            if (Doubled || State != GameState.Victory) return;
            app.Reward("double_coins", () =>
            {
                if (Doubled) return;
                Doubled = true; app.Save.Data.coins += RewardCoins; app.Save.Save(); ui.ShowResult();
            });
        }
        public void TogglePause()
        {
            if (State == GameState.Paused)
            {
                State = resumeState; motor.Running = State == GameState.Running || State == GameState.Evolving;
                Time.timeScale = State == GameState.Evolving ? .55f : 1; ui.ShowHud(); return;
            }
            if (State != GameState.Running && State != GameState.Evolving && State != GameState.BossBattle) return;
            motor.Input.ResetHold();
            resumeState = State; State = GameState.Paused; motor.Running = false; Time.timeScale = 0; ui.ShowPause();
        }
        void OnApplicationPause(bool paused)
        {
            countdownPaused=paused;
            if(State==GameState.Preparing){countdownRemaining=3;return;}
            if(paused && State!=GameState.Paused)TogglePause();
        }
        void OnApplicationFocus(bool focused)
        {
            countdownUnfocused=!focused;
            if(State==GameState.Preparing && !focused)countdownRemaining=3;
        }
        void OnDestroy()
        {
            Time.timeScale=1;
            if(Player)Player.Evolved-=Evolve;
            if(Level)Destroy(Level);
        }
    }
}
