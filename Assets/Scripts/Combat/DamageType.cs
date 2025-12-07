using UnityEngine;

// 1. Define the Elements (Based on Status Effects - Page 14 & 15)
public enum DamageElement
{
    Physical,   // Standard cuts, bludgeoning (Rusty Dagger, Iron Hammer)
    Magic,      // Raw arcane energy (Apprentice Staff)
    Fire,       // Sources: Lava, Flame weapons (Inferno Orb)
    Ice,        // Sources: Frost traps, Ice weapons (Winter Pearl)
    Poison,     // Sources: Swamp, Venom weapons (Swamp Brew)
    Psychic,    // Sources: Illusion traps, Psychic enemies (Confusion effect)
    True        // Ignores defense (for DoT ticks or special boss mechanics)
}

// 2. Define the Style (Based on Stat Upgrades - Page 1)
public enum AttackStyle
{
    MeleeLight, // Fast, low knockback
    MeleeHeavy, // Slow, high knockback, high damage
    Ranged,     // Projectiles, magic
    DoT,        // Damage over Time ticks (Burn, Poison)
    Environment // Traps, falling rocks
}

// 3. The "Packet" of data sent when something gets hit
[System.Serializable]
public class DamageInfo
{
    public float amount;
    public DamageElement element;
    public AttackStyle style;
    public Vector2 sourcePosition; // For calculating knockback direction
    public float knockbackForce;
    public bool isCritical; // Page 14: "Any weapon attack has a slight chance to crit"

    // Constructor helper
    public DamageInfo(float amt, DamageElement elm, AttackStyle sty, Vector2 src, float kb, bool crit = false)
    {
        amount = amt;
        element = elm;
        style = sty;
        sourcePosition = src;
        knockbackForce = kb;
        isCritical = crit;
    }
}
