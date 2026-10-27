using System;
using System.Collections.Generic;
using UnityEngine;

namespace MonsterMerge
{
    // Stable IDs keep existing collections and saves intact while the roster becomes all dragons.
    public static class DragonProgression
    {
        public const int MaxStage = 4;
        static readonly string[] ids = { "dragon", "shark", "slime", "trex", "panda", "sharkdragon", "cyberrex", "ninjapanda" };
        static readonly string[] names = { "Cinderclaw", "Coralith", "Viperthorn", "Cragback", "Glacierveil", "Voltwing", "Ironforge", "Nyx Eclipse" };
        static readonly int[] prices = { 0, 300, 700, 1400, 2400, 4000, 6500, 10000 };
        static readonly string[] titles = { "Flame Breath", "Tidal Mend", "Venom Fangs", "Stone Guard", "Frost Barrier", "Storm Surge", "Coin Engine", "Shadow Strike" };
        static readonly Element[] elements = { Element.Fire, Element.Ice, Element.Normal, Element.Normal, Element.Ice, Element.Lightning, Element.Machine, Element.Shadow };
        static readonly Color[] colors = {
            new Color(1,.34f,.13f), new Color(.13f,.68f,.79f), new Color(.47f,.86f,.23f), new Color(.65f,.45f,.25f),
            new Color(.56f,.91f,1), new Color(1,.84f,.19f), new Color(.54f,.67f,.79f), new Color(.63f,.32f,.91f)
        };
        static readonly float[] attackMultipliers={1f,1.16f,1.36f,1.62f};
        static readonly float[] healthMultipliers={1f,1.20f,1.46f,1.78f};
        static readonly float[] flameDamage={.10f,.17f,.28f,.43f};
        static readonly float[] tidalHealPerPower={.20f,.32f,.48f,.68f};
        static readonly float[] venomDamagePerHit={.04f,.06f,.09f,.13f};
        static readonly float[] stoneDamageReduction={.12f,.17f,.24f,.33f};
        static readonly int[] frostBarrier={20,35,55,80};
        static readonly float[] stormDamageBonus={.50f,.75f,1.10f,1.50f};
        static readonly int[] coinBonus={10,18,29,43};
        static readonly float[] shadowDamage={2f,2.5f,3.25f,4.25f};

        public static void Apply(GameCatalog catalog)
        {
            if (!catalog || catalog.monsters == null) return;
            GameObject dragonPrefab = null;
            foreach (var definition in catalog.monsters)
                if (definition && definition.id == "dragon") { dragonPrefab = definition.prefab; break; }
            foreach (var definition in catalog.monsters)
            {
                if (!definition) continue;
                int index = Array.IndexOf(ids, definition.id);
                if (index < 0) continue;
                definition.displayName = names[index];
                definition.unlockCost = prices[index];
                definition.baseHealth = 100 + index * 12;
                definition.baseAttack = 13 + index * 2;
                definition.baseSpeed = 7;
                definition.startingElement = elements[index];
                definition.power = (DragonPower)index;
                definition.color = colors[index];
                if (dragonPrefab) definition.prefab = dragonPrefab;
                // Run evolution stays distinct from the permanent four-stage training track.
                definition.evolutions = new[] {
                    new EvolutionStage { title = "Baby", requiredDna = 0, scale = .8f, attackBonus = 0 },
                    new EvolutionStage { title = "Grown", requiredDna = 20, scale = 1, attackBonus = 4 },
                    new EvolutionStage { title = "Mighty", requiredDna = 60, scale = 1.15f, attackBonus = 8 },
                    new EvolutionStage { title = "Legendary", requiredDna = 110, scale = 1.3f, attackBonus = 14 }
                };
            }
            catalog.monsters = Ordered(catalog);
        }

        public static MonsterDefinition[] Ordered(GameCatalog catalog)
        {
            var result = new List<MonsterDefinition>();
            if (!catalog || catalog.monsters == null) return result.ToArray();
            foreach (string id in ids)
                foreach (var definition in catalog.monsters)
                    if (definition && definition.id == id) { result.Add(definition); break; }
            return result.ToArray();
        }

        public static bool Known(string id) => Array.IndexOf(ids, id) >= 0;
        public static string Species(string id)
        {
            int i=Array.IndexOf(ids,id);
            return i<0?"Dragon":new[]{"FIRE WYVERN","REEF LEVIATHAN","VENOM DRAKE","STONE BEHEMOTH","CRYSTAL DRAGON","THUNDER WYVERN","FORGED AUTOMATON","ECLIPSE WRAITH"}[i];
        }
        public static string VisualUpgrade(int stage)
        { return new[]{"Signature anatomy","Armored shoulders","Expanded wing crests","Ancient crown & tail"}[Mathf.Clamp(stage,1,4)-1]; }
        public static string VisualUpgrade(string id,int stage)
        {
            string[] forms;
            switch(id)
            {
                case "dragon":forms=new[]{"Ember scales & teal wings","Glowing ember shoulder scales","Swept flame wing crests","Royal flame horns & blazing tail"};break;
                case "shark":forms=new[]{"Reef fins & ocean markings","Pearl shoulder fins","Flowing coral wing fans","Branching coral crown & tail flukes"};break;
                case "slime":forms=new[]{"Serpent hood & jade scales","Twin venom shoulder glands","Serrated leaf wing spurs","Split serpent crown & venom tail"};break;
                case "trex":forms=new[]{"Stone hide & sturdy paws","Layered basalt shoulder shields","Broad granite wing ridges","Mountain crown & stone tail hammer"};break;
                case "panda":forms=new[]{"Frost scales & crystal antlers","Faceted ice shoulder crystals","Long frost wing shards","Glacier crown & diamond tail"};break;
                case "sharkdragon":forms=new[]{"Swept quills & golden scales","Lightning shoulder fins","Forked thunder wing bolts","Storm crown & twin lightning tail"};break;
                case "cyberrex":forms=new[]{"Metal visor & articulated wings","Reactor shoulder housings","Extended mechanical wing panels","Twin antenna crown & turbine tail"};break;
                default:forms=new[]{"Crescent horns & violet wings","Moonstone shoulder guards","Swept eclipse wing ribbons","Crescent crown & shadow tail fans"};break;
            }
            return forms[Mathf.Clamp(stage,1,4)-1];
        }
        public static int UnlockCost(string id)
        { int index = Array.IndexOf(ids, id); return index < 0 ? -1 : prices[index]; }
        public static string StageName(int stage)
        { return new[] { "Hatchling", "Awakened", "Ascendant", "Ancient" }[Mathf.Clamp(stage, 1, MaxStage) - 1]; }
        static int Tier(int stage)=>Mathf.Clamp(stage,1,MaxStage)-1;
        public static float AttackMultiplier(int stage)=>attackMultipliers[Tier(stage)];
        public static float HealthMultiplier(int stage)=>healthMultipliers[Tier(stage)];
        public static float FlameDamageBonus(int stage)=>flameDamage[Tier(stage)];
        public static float TidalHealPerPower(int stage)=>tidalHealPerPower[Tier(stage)];
        public static float VenomDamagePerHit(int stage)=>venomDamagePerHit[Tier(stage)];
        public static float StoneDamageReduction(int stage)=>stoneDamageReduction[Tier(stage)];
        public static int FrostBarrier(int stage)=>frostBarrier[Tier(stage)];
        public static float StormDamageBonus(int stage)=>stormDamageBonus[Tier(stage)];
        public static int CoinBonusPercent(int stage)=>coinBonus[Tier(stage)];
        public static float ShadowDamageMultiplier(int stage)=>shadowDamage[Tier(stage)];
        public static int StageCost(MonsterDefinition definition, int currentStage) => definition ? StageCost(definition.id, currentStage) : 0;
        public static int StageCost(string id, int currentStage)
        {
            if (!Known(id) || currentStage >= MaxStage) return 0;
            int basis = Mathf.Max(150, Mathf.CeilToInt(UnlockCost(id) * .4f / 50) * 50);
            return basis * Mathf.Clamp(currentStage, 1, MaxStage - 1);
        }
        public static int SavedStage(SaveData data, string id)
        {
            int stage = 1;
            if (data?.dragonMastery != null)
                foreach (var record in data.dragonMastery)
                    if (record != null && record.id == id) stage = Mathf.Max(stage, Mathf.Clamp(record.stage, 1, MaxStage));
            return stage;
        }
        public static string PowerTitle(string id)
        { int index = Array.IndexOf(ids, id); return index < 0 ? "Dragon Power" : titles[index]; }
        public static string PowerUpgradeDelta(string id,int fromStage,int toStage)
        {
            int from=Tier(fromStage),to=Tier(toStage);
            switch(id)
            {
                case "dragon":return "+"+Percent((flameDamage[to]-flameDamage[from])*100)+" percentage points of flame damage per hit";
                case "shark":return "+"+Number((tidalHealPerPower[to]-tidalHealPerPower[from])*10)+" HP per 10 power collected";
                case "slime":return "+"+Percent((venomDamagePerHit[to]-venomDamagePerHit[from])*100)+" percentage points of venom damage per hit";
                case "trex":return "+"+Percent((stoneDamageReduction[to]-stoneDamageReduction[from])*100)+" percentage points of damage reduction";
                case "panda":return "+"+(frostBarrier[to]-frostBarrier[from])+" starting barrier";
                case "sharkdragon":return "+"+Percent((stormDamageBonus[to]-stormDamageBonus[from])*100)+" percentage points on every third hit";
                case "cyberrex":return "+"+(coinBonus[to]-coinBonus[from])+" percentage points of track coins";
                case "ninjapanda":return "+"+Number(shadowDamage[to]-shadowDamage[from])+"x damage on every fourth hit";
                default:return "Your dragon's signature power becomes stronger";
            }
        }
        static string Number(float value)=>value.ToString("0.#",System.Globalization.CultureInfo.InvariantCulture);
        static string Percent(float value)=>Number(value);
        public static string PowerName(MonsterDefinition definition) => PowerTitle(definition ? definition.id : "");
        public static string PowerDescription(MonsterDefinition definition, int stage) => PowerDescription(definition ? definition.id : "", stage);
        public static string PowerDescription(string id, int stage)
        {
            switch (id)
            {
                case "dragon": return "Every hit deals " + Percent(flameDamage[Tier(stage)]*100) + "% extra flame damage.";
                case "shark": return "Each 10 power collected heals " + Number(tidalHealPerPower[Tier(stage)]*10) + " HP.";
                case "slime": return "Each hit adds " + Percent(venomDamagePerHit[Tier(stage)]*100) + "% venom damage, stacking up to 5 hits.";
                case "trex": return "Take " + Percent(stoneDamageReduction[Tier(stage)]*100) + "% less damage from every hit.";
                case "panda": return "Start each run with a barrier that absorbs " + frostBarrier[Tier(stage)] + " damage.";
                case "sharkdragon": return "Every third hit deals " + Percent(stormDamageBonus[Tier(stage)]*100) + "% extra lightning damage.";
                case "cyberrex": return "Collect " + coinBonus[Tier(stage)] + "% more coins on the track.";
                case "ninjapanda": return "Every fourth hit deals " + Number(shadowDamage[Tier(stage)]) + "x damage.";
                default: return "Train your dragon to strengthen its unique power.";
            }
        }
    }
}
