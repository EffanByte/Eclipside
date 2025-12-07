using UnityEngine;

// 1. The Interface (The Contract)
// Any script that signs this contract MUST have a ReceiveDamage function.
public interface IDamageable
{
    void ReceiveDamage(DamageInfo damage);
}

// -------------------------------------------------------------------------
// HELPER CLASSES (Required for the Interface to work)
// -------------------------------------------------------------------------

// 2. Damage Element (Page 14-16 of PDF)
public enum DamageElement
{
    Physical,   // Standard
    Magic,      // Arcane/Purple
    Fire,       // Burn
    Ice,        // Freeze
    Poison,     // DoT
    Psychic,    // Confusion
    True        // Ignores armor/mechanics
}

// 3. Attack Style (Page 1 of PDF - "Melee" vs "Magic")
public enum AttackStyle
{
    MeleeLight,
    MeleeHeavy,
    Ranged,
    Environment // Traps
}

// 4. The Data Packet
// This passes all necessary info from Attacker -> Defender
[System.Serializable]
public struct DamageInfo
{
    public float amount;
    public DamageElement element;
    public AttackStyle style;
    public Vector2 sourcePosition; // Used to calculate knockback direction
    public float knockbackForce;
    public bool isCritical;

    // Constructor for easy creation
    public DamageInfo(float amount, DamageElement element, AttackStyle style, Vector2 sourcePosition, float knockbackForce = 0f, bool isCritical = false)
    {
        this.amount = amount;
        this.element = element;
        this.style = style;
        this.sourcePosition = sourcePosition;
        this.knockbackForce = knockbackForce;
        this.isCritical = isCritical;
    }
}