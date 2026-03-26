using System;
using System.Collections.Generic;
using UnityEngine;

public static class WeaponDefinitionLibrary
{
    private sealed class WeaponDefinition
    {
        public string CanonicalName;
        public string DisplayName;
        public string Description;
        public ItemRarity Rarity;
        public float Damage;
        public float Cooldown;
        public float Knockback;
        public float HitDuration;
        public float CritChance;
        public float ProjectileSpeed;
        public DamageElement Element;
        public AttackStyle Style;
        public Func<List<WeaponEffect>> CreateEffects;
    }

    private static readonly Dictionary<string, WeaponDefinition> Definitions = new Dictionary<string, WeaponDefinition>(StringComparer.OrdinalIgnoreCase)
    {
        ["Rusty Dagger"] = Create("Rusty Dagger", "Rusty Dagger", "Basic light melee dagger with no effects.", ItemRarity.Common, 5f, 0.42f, 2.8f, 0.16f, 5f, 0f, DamageElement.Physical, AttackStyle.MeleeLight),
        ["Dull Sword"] = Create("Dull Sword", "Dull Sword", "+3% attack speed.", ItemRarity.Common, 6f, 0.48f, 3.2f, 0.18f, 5f, 0f, DamageElement.Physical, AttackStyle.MeleeLight, () => Effects(StatBoost(StatType.AttackSpeed, 0.03f))),
        ["Twin Fang Daggers"] = Create("Twin Fang Daggers", "Twin Fang Daggers", "+10% attack speed.", ItemRarity.Rare, 6.6f, 0.39f, 3f, 0.16f, 6f, 0f, DamageElement.Physical, AttackStyle.MeleeLight, () => Effects(StatBoost(StatType.AttackSpeed, 0.10f))),
        ["Hunter's Kukri"] = Create("Hunter's Kukri", "Hunter's Kukri", "+8% damage to enemies below 30% HP.", ItemRarity.Rare, 6.8f, 0.43f, 3.1f, 0.17f, 6f, 0f, DamageElement.Physical, AttackStyle.MeleeLight, () => Effects(Conditional(ConditionalDamageEffect.Condition.Below30PercentHP, damageMultiplier: 0.08f))),
        ["Predator's Dagger"] = Create("Predator's Dagger", "Predator's Dagger", "+15% crit chance against poisoned enemies.", ItemRarity.Rare, 6.7f, 0.43f, 3.1f, 0.17f, 6f, 0f, DamageElement.Physical, AttackStyle.MeleeLight, () => Effects(Conditional(ConditionalDamageEffect.Condition.TargetIsPoisoned, critChanceBonus: 15f))),
        ["Moonfang Knife"] = Create("Moonfang Knife", "Moonfang Knife", "+10% attack speed when hitting frozen enemies.", ItemRarity.Rare, 6.7f, 0.43f, 3.1f, 0.17f, 6f, 0f, DamageElement.Physical, AttackStyle.MeleeLight, () => Effects(Conditional(ConditionalDamageEffect.Condition.TargetIsFrozen, speedMultiplier: 0.10f))),
        ["Shadow Claws"] = Create("Shadow Claws", "Shadow Claws", "20% chance to confuse and +5% crit chance.", ItemRarity.Epic, 7.3f, 0.38f, 3.2f, 0.16f, 7f, 0f, DamageElement.Physical, AttackStyle.MeleeLight, () => Effects(Status(StatusType.Confusion, 0.20f), StatBoost(StatType.CritChance, 5f))),
        ["Vampiric Dagger"] = Create("Vampiric Dagger", "Vampiric Dagger", "Restores 0.1 hearts every 15 hits.", ItemRarity.Epic, 7.3f, 0.39f, 3.2f, 0.16f, 7f, 0f, DamageElement.Physical, AttackStyle.MeleeLight, () => Effects(HealEveryHits(15, 1f))),
        ["Flamefang Daggers"] = Create("Flamefang Daggers", "Flamefang Daggers", "20% burn chance and +5% attack speed.", ItemRarity.Epic, 7.2f, 0.39f, 3.2f, 0.16f, 7f, 0f, DamageElement.Physical, AttackStyle.MeleeLight, () => Effects(Status(StatusType.Burn, 0.20f), StatBoost(StatType.AttackSpeed, 0.05f))),
        ["Chrono Daggers"] = Create("Chrono Daggers", "Chrono Daggers", "+25% attack speed. Kills slowly drag down enemy tempo.", ItemRarity.Mythical, 8.4f, 0.33f, 3.4f, 0.15f, 10f, 0f, DamageElement.Physical, AttackStyle.MeleeLight, () => Effects(StatBoost(StatType.AttackSpeed, 0.25f), GlobalTempo(0.005f, 0.75f, 5))),

        ["Iron Hammer"] = Create("Iron Hammer", "Iron Hammer", "Heavy knockback, no effects.", ItemRarity.Common, 10.5f, 0.76f, 6.7f, 0.22f, 4f, 0f, DamageElement.Physical, AttackStyle.MeleeHeavy),
        ["Worn Mace"] = Create("Worn Mace", "Worn Mace", "+3% base damage.", ItemRarity.Common, 10.8f, 0.74f, 6.5f, 0.22f, 4f, 0f, DamageElement.Physical, AttackStyle.MeleeHeavy, () => Effects(StatBoost(StatType.BaseDamage, 0.03f))),
        ["War Axe"] = Create("War Axe", "War Axe", "+12% max health.", ItemRarity.Rare, 11.3f, 0.71f, 6.7f, 0.22f, 5f, 0f, DamageElement.Physical, AttackStyle.MeleeHeavy, () => Effects(PercentMaxHealth(0.12f))),
        ["Skull Splitter"] = Create("Skull Splitter", "Skull Splitter", "+10% critical damage.", ItemRarity.Rare, 11.4f, 0.72f, 6.8f, 0.22f, 5f, 0f, DamageElement.Physical, AttackStyle.MeleeHeavy, () => Effects(StatBoost(StatType.CritDamage, 0.10f))),
        ["Venom Axe"] = Create("Venom Axe", "Venom Axe", "+10% damage against poisoned enemies.", ItemRarity.Rare, 11.2f, 0.71f, 6.8f, 0.22f, 5f, 0f, DamageElement.Physical, AttackStyle.MeleeHeavy, () => Effects(Conditional(ConditionalDamageEffect.Condition.TargetIsPoisoned, damageMultiplier: 0.10f))),
        ["Venomfang Greatsword"] = Create("Venomfang Greatsword", "Venomfang Greatsword", "Applies poison and +5% base damage.", ItemRarity.Epic, 12.2f, 0.67f, 7f, 0.23f, 6f, 0f, DamageElement.Physical, AttackStyle.MeleeHeavy, () => Effects(Status(StatusType.Poison, 0.15f), StatBoost(StatType.BaseDamage, 0.05f))),
        ["Titan Maul"] = Create("Titan Maul", "Titan Maul", "+10% knockback and +8% confusion chance.", ItemRarity.Epic, 12.4f, 0.68f, 7.7f, 0.23f, 6f, 0f, DamageElement.Physical, AttackStyle.MeleeHeavy, () => Effects(Status(StatusType.Confusion, 0.08f))),
        ["Infernal Maul"] = Create("Infernal Maul", "Infernal Maul", "+7% knockback and 15% burn chance.", ItemRarity.Epic, 12.2f, 0.68f, 7.5f, 0.23f, 6f, 0f, DamageElement.Physical, AttackStyle.MeleeHeavy, () => Effects(Status(StatusType.Burn, 0.15f))),
        ["Bloodforged Colossus"] = Create("Bloodforged Colossus", "Bloodforged Colossus", "+50% damage. Every 3 kills, take 0.2 heart true damage.", ItemRarity.Mythical, 15f, 0.74f, 8.2f, 0.24f, 8f, 0f, DamageElement.Physical, AttackStyle.MeleeHeavy, () => Effects(StatBoost(StatType.BaseDamage, 0.50f), SelfDamageOnKill(3, 2f))),

        ["Apprentice's Staff"] = Create("Apprentice's Staff", "Apprentice's Staff", "Basic magic projectile.", ItemRarity.Common, 6f, 0.55f, 2f, 0.14f, 4f, 8f, DamageElement.Magic, AttackStyle.Ranged),
        ["Cracked Scepter"] = Create("Cracked Scepter", "Cracked Scepter", "+3% magic damage.", ItemRarity.Common, 6.2f, 0.54f, 2f, 0.14f, 4f, 8f, DamageElement.Magic, AttackStyle.Ranged, () => Effects(StatBoost(StatType.MagicDamage, 0.03f))),
        ["Crystal Wand"] = Create("Crystal Wand", "Crystal Wand", "+10% magic damage.", ItemRarity.Rare, 7f, 0.50f, 2.1f, 0.14f, 5f, 9f, DamageElement.Magic, AttackStyle.Ranged, () => Effects(StatBoost(StatType.MagicDamage, 0.10f))),
        ["Arcane Sling"] = Create("Arcane Sling", "Arcane Sling", "+7% projectile speed.", ItemRarity.Rare, 6.8f, 0.48f, 2.1f, 0.14f, 5f, 10f, DamageElement.Magic, AttackStyle.Ranged, () => Effects(StatBoost(StatType.projectileSpeed, 0.07f))),
        ["Storm Catalyst"] = Create("Storm Catalyst", "Storm Catalyst", "+15% projectile speed if the target is already burning.", ItemRarity.Rare, 6.9f, 0.49f, 2.1f, 0.14f, 5f, 9.5f, DamageElement.Magic, AttackStyle.Ranged, () => Effects(TargetStatusProjectileSpeed(StatusType.Burn, 1.15f))),
        ["Hex Scepter"] = Create("Hex Scepter", "Hex Scepter", "+20% magic damage against confused enemies.", ItemRarity.Rare, 7.1f, 0.49f, 2.1f, 0.14f, 5f, 9.2f, DamageElement.Magic, AttackStyle.Ranged, () => Effects(Conditional(ConditionalDamageEffect.Condition.TargetIsConfused, damageMultiplier: 0.20f))),
        ["Inferno Orb"] = Create("Inferno Orb", "Inferno Orb", "Burning projectiles and +5% attack speed.", ItemRarity.Epic, 8.2f, 0.44f, 2.2f, 0.14f, 6f, 10.5f, DamageElement.Magic, AttackStyle.Ranged, () => Effects(Status(StatusType.Burn, 0.20f), StatBoost(StatType.AttackSpeed, 0.05f))),
        ["Stormcaller Tome"] = Create("Stormcaller Tome", "Stormcaller Tome", "5% chance to summon lightning per projectile.", ItemRarity.Epic, 8.1f, 0.45f, 2.2f, 0.14f, 6f, 10f, DamageElement.Magic, AttackStyle.Ranged, () => Effects(LightningProc(0.05f, 15f))),
        ["Frostbinder Tome"] = Create("Frostbinder Tome", "Frostbinder Tome", "15% chance to freeze for 2 seconds.", ItemRarity.Epic, 8.0f, 0.46f, 2.2f, 0.14f, 6f, 9.6f, DamageElement.Magic, AttackStyle.Ranged, () => Effects(Status(StatusType.Freeze, 0.15f, 2f))),
        ["Aurora Prism"] = Create("Aurora Prism", "Aurora Prism", "+15% attack speed and magic damage. Projectiles pierce through enemies like beams.", ItemRarity.Mythical, 9.8f, 0.40f, 2.4f, 0.14f, 8f, 13f, DamageElement.Magic, AttackStyle.Ranged, () => Effects(StatBoost(StatType.AttackSpeed, 0.15f), StatBoost(StatType.MagicDamage, 0.15f), ProjectileModifier(1f, 6, 1.8f, 3f)))
    };

    public static void NormalizeWeapons(IEnumerable<WeaponData> weapons)
    {
        if (weapons == null)
        {
            return;
        }

        foreach (WeaponData weapon in weapons)
        {
            NormalizeWeapon(weapon);
        }
    }

    public static void NormalizeWeapon(WeaponData weapon)
    {
        if (weapon == null)
        {
            return;
        }

        string key = ResolveDefinitionKey(weapon);
        if (!Definitions.TryGetValue(key, out WeaponDefinition definition))
        {
            Debug.LogWarning($"[Weapons] No definition found for '{weapon.name}'.");
            return;
        }

        weapon.name = definition.CanonicalName;
        weapon.itemName = definition.DisplayName;
        weapon.description = definition.Description;
        weapon.rarity = definition.Rarity;
        weapon.ConfigureCoreStats(
            definition.Damage,
            definition.Cooldown,
            definition.Knockback,
            definition.HitDuration,
            definition.CritChance,
            definition.ProjectileSpeed,
            definition.Element,
            definition.Style);

        weapon.ReplaceEffects(definition.CreateEffects != null ? definition.CreateEffects() : new List<WeaponEffect>());
    }

    private static string ResolveDefinitionKey(WeaponData weapon)
    {
        if (weapon is HeavyMelee && string.Equals(weapon.name, "Dull Sword", StringComparison.OrdinalIgnoreCase))
        {
            return "Iron Hammer";
        }

        if (weapon is HeavyMelee && string.Equals(weapon.name, "Titan  Maul", StringComparison.OrdinalIgnoreCase))
        {
            return "Titan Maul";
        }

        return weapon.name?.Trim() ?? string.Empty;
    }

    private static WeaponDefinition Create(
        string canonicalName,
        string displayName,
        string description,
        ItemRarity rarity,
        float damage,
        float cooldown,
        float knockback,
        float hitDuration,
        float critChance,
        float projectileSpeed,
        DamageElement element,
        AttackStyle style,
        Func<List<WeaponEffect>> createEffects = null)
    {
        return new WeaponDefinition
        {
            CanonicalName = canonicalName,
            DisplayName = displayName,
            Description = description,
            Rarity = rarity,
            Damage = damage,
            Cooldown = cooldown,
            Knockback = knockback,
            HitDuration = hitDuration,
            CritChance = critChance,
            ProjectileSpeed = projectileSpeed,
            Element = element,
            Style = style,
            CreateEffects = createEffects
        };
    }

    private static List<WeaponEffect> Effects(params WeaponEffect[] effects)
    {
        return new List<WeaponEffect>(effects);
    }

    private static WeaponStatBoost StatBoost(StatType statType, float amount)
    {
        WeaponStatBoost effect = CreateRuntimeEffect<WeaponStatBoost>("StatBoost");
        effect.statToBuff = statType;
        effect.amount = amount;
        return effect;
    }

    private static ConditionalDamageEffect Conditional(
        ConditionalDamageEffect.Condition condition,
        float damageMultiplier = 0f,
        float critChanceBonus = 0f,
        float speedMultiplier = 0f,
        float knockbackMultiplier = 0f)
    {
        ConditionalDamageEffect effect = CreateRuntimeEffect<ConditionalDamageEffect>("Conditional");
        effect.condition = condition;
        effect.damageMultiplier = damageMultiplier;
        effect.critChanceBonus = critChanceBonus;
        effect.speedMultiplier = speedMultiplier;
        effect.knockbackMultiplier = knockbackMultiplier;
        return effect;
    }

    private static WeaponStatusChance Status(StatusType statusType, float chance, float durationOverride = -1f)
    {
        WeaponStatusChance effect = CreateRuntimeEffect<WeaponStatusChance>("Status");
        effect.statusToApply = statusType;
        effect.chance = chance;
        effect.durationOverride = durationOverride;
        return effect;
    }

    private static WeaponHealEveryHitsEffect HealEveryHits(int threshold, float healAmount)
    {
        WeaponHealEveryHitsEffect effect = CreateRuntimeEffect<WeaponHealEveryHitsEffect>("HealEveryHits");
        effect.hitThreshold = threshold;
        effect.healAmount = healAmount;
        return effect;
    }

    private static WeaponPercentMaxHealthBoostEffect PercentMaxHealth(float percent)
    {
        WeaponPercentMaxHealthBoostEffect effect = CreateRuntimeEffect<WeaponPercentMaxHealthBoostEffect>("PercentMaxHealth");
        effect.percent = percent;
        return effect;
    }

    private static WeaponLightningProcEffect LightningProc(float chance, float damageAmount)
    {
        WeaponLightningProcEffect effect = CreateRuntimeEffect<WeaponLightningProcEffect>("LightningProc");
        effect.chance = chance;
        effect.damageAmount = damageAmount;
        return effect;
    }

    private static WeaponSelfDamageOnKillEffect SelfDamageOnKill(int killThreshold, float selfDamageAmount)
    {
        WeaponSelfDamageOnKillEffect effect = CreateRuntimeEffect<WeaponSelfDamageOnKillEffect>("SelfDamageOnKill");
        effect.killThreshold = killThreshold;
        effect.selfDamageAmount = selfDamageAmount;
        return effect;
    }

    private static WeaponProjectileModifierEffect ProjectileModifier(float speedMultiplier, int additionalPierces, float colliderRadiusMultiplier, float lifetime)
    {
        WeaponProjectileModifierEffect effect = CreateRuntimeEffect<WeaponProjectileModifierEffect>("ProjectileModifier");
        effect.speedMultiplier = speedMultiplier;
        effect.additionalPierces = additionalPierces;
        effect.colliderRadiusMultiplier = colliderRadiusMultiplier;
        effect.lifetime = lifetime;
        return effect;
    }

    private static WeaponTargetStatusProjectileSpeedEffect TargetStatusProjectileSpeed(StatusType statusType, float speedMultiplier)
    {
        WeaponTargetStatusProjectileSpeedEffect effect = CreateRuntimeEffect<WeaponTargetStatusProjectileSpeedEffect>("TargetStatusProjectileSpeed");
        effect.requiredStatus = statusType;
        effect.speedMultiplier = speedMultiplier;
        return effect;
    }

    private static WeaponGlobalTempoOnKillEffect GlobalTempo(float slowPerKill, float minTempoMultiplier, int resetEveryWaveCount)
    {
        WeaponGlobalTempoOnKillEffect effect = CreateRuntimeEffect<WeaponGlobalTempoOnKillEffect>("GlobalTempoOnKill");
        effect.slowPerKill = slowPerKill;
        effect.minTempoMultiplier = minTempoMultiplier;
        effect.resetEveryWaveCount = resetEveryWaveCount;
        return effect;
    }

    private static T CreateRuntimeEffect<T>(string effectName) where T : WeaponEffect
    {
        T effect = ScriptableObject.CreateInstance<T>();
        effect.name = effectName;
        return effect;
    }
}
