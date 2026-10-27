using System;
using UnityEngine;
namespace MonsterMerge
{
    public interface IDamageable { void TakeDamage(float damage); }
    public sealed class PlayerMonster : MonoBehaviour, IDamageable
    {
        public float CurrentHealth { get; private set; }
        public float MaxHealth { get; private set; }
        public int DNA { get; private set; }
        public int CoinsCollected { get; private set; }
        public int EvolutionLevel { get; private set; }
        public int MasteryStage { get; private set; } = 1;
        public float BarrierHealth { get; private set; }
        public bool LastPowerActivated { get; private set; }
        public Element CurrentElement { get; private set; }
        public MonsterDefinition Definition { get; private set; }
        public float Speed => Definition.baseSpeed;
        public float AttackPower => (Definition.baseAttack + upgrades * catalog.attackPerUpgrade +
            Definition.evolutions[EvolutionLevel].attackBonus + DNA * .12f) * catalog.ElementData(CurrentElement).attackMultiplier * DragonProgression.AttackMultiplier(MasteryStage);
        public string EvolutionName => (CurrentElement == Element.Normal ? "" : catalog.ElementData(CurrentElement).prefix + " ") +
            (string.IsNullOrEmpty(Definition.evolutions[EvolutionLevel].title) ? "" : Definition.evolutions[EvolutionLevel].title + " ") + Definition.displayName;
        public event Action Changed;
        public event Action Evolved;
        GameCatalog catalog;
        int upgrades, attackCount, coinBonusRemainder;
        public void Initialize(MonsterDefinition definition, GameCatalog config, SaveData save, int bonus)
        {
            Definition = definition; catalog = config; upgrades = save.attack;
            MasteryStage = DragonProgression.SavedStage(save, definition.id);
            DNA = 0; EvolutionLevel = 0; CoinsCollected = 0; attackCount = 0; coinBonusRemainder = 0;
            LastPowerActivated = false;
            BarrierHealth = definition.power == DragonPower.FrostBarrier ? DragonProgression.FrostBarrier(MasteryStage) : 0;
            MaxHealth = (definition.baseHealth + save.health * config.healthPerUpgrade) * DragonProgression.HealthMultiplier(MasteryStage);
            CurrentHealth = MaxHealth; CurrentElement = definition.startingElement;
            AddDna(save.startingDna * config.dnaPerUpgrade + bonus);
        }
        public void AddDna(int amount)
        {
            int previous = DNA;
            DNA = (int)Math.Max(0, Math.Min(int.MaxValue, (long)DNA + amount));
            RecoverFromPower(DNA - previous);
            int stage = 0;
            while (stage + 1 < Definition.evolutions.Length && DNA >= Definition.evolutions[stage + 1].requiredDna) stage++;
            if (stage != EvolutionLevel) { EvolutionLevel = stage; Evolved?.Invoke(); }
            Changed?.Invoke();
        }
        public void Mutate(Element element)
        {
            CurrentElement = element;
            // One evolution event per gate, even when its DNA crosses a stage threshold.
            DNA += catalog.gateDna;
            RecoverFromPower(catalog.gateDna);
            while (EvolutionLevel + 1 < Definition.evolutions.Length && DNA >= Definition.evolutions[EvolutionLevel + 1].requiredDna) EvolutionLevel++;
            Evolved?.Invoke(); Changed?.Invoke();
        }
        void RecoverFromPower(int amount)
        {
            if (amount > 0 && Definition.power == DragonPower.TidalMend)
                CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount * DragonProgression.TidalHealPerPower(MasteryStage));
        }
        // Called once when a boss hit lands. Reading AttackPower for HUD text must not
        // advance counters or trigger special attacks, and an intact boss ward stays intact.
        public float ResolveAttackDamage()
        {
            attackCount++;
            float multiplier = 1;
            LastPowerActivated = false;
            switch (Definition.power)
            {
                case DragonPower.FlameBreath: multiplier += DragonProgression.FlameDamageBonus(MasteryStage); break;
                case DragonPower.VenomFangs: multiplier += Mathf.Min(attackCount, 5) * DragonProgression.VenomDamagePerHit(MasteryStage); break;
                case DragonPower.StormSurge: if (attackCount % 3 == 0) multiplier += DragonProgression.StormDamageBonus(MasteryStage); break;
                case DragonPower.ShadowStrike: if (attackCount % 4 == 0) multiplier = DragonProgression.ShadowDamageMultiplier(MasteryStage); break;
            }
            LastPowerActivated = multiplier > 1;
            return AttackPower * multiplier;
        }
        public void CollectCoin(int amount)
        {
            if (amount <= 0) return;
            long total = amount;
            if (Definition.power == DragonPower.CoinEngine)
            {
                long bonus = (long)amount * DragonProgression.CoinBonusPercent(MasteryStage) + coinBonusRemainder;
                total += bonus / 100;
                coinBonusRemainder = (int)(bonus % 100);
            }
            CoinsCollected = (int)Math.Min(int.MaxValue, CoinsCollected + total);
            Changed?.Invoke();
        }
        public void TakeDamage(float damage)
        {
            damage = Mathf.Max(0, damage);
            if (Definition.power == DragonPower.StoneGuard) damage *= 1 - DragonProgression.StoneDamageReduction(MasteryStage);
            float absorbed = Mathf.Min(BarrierHealth, damage);
            BarrierHealth -= absorbed;
            CurrentHealth = Mathf.Max(0, CurrentHealth - (damage - absorbed));
            Changed?.Invoke();
        }
        public void Revive() { CurrentHealth = MaxHealth * .5f; Changed?.Invoke(); }
    }
}
