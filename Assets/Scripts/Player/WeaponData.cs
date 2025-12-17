using UnityEngine;

public abstract class WeaponData : ScriptableObject
{
    [Header("Core Info")]
    public string weaponName;
    public Sprite icon;
    public AnimatorOverrideController animatorOverride; // Custom animations per weapon

    [Header("Base Stats")]
    public float damage;
    public float cooldown;
    public float knockbackForce;

    // Every weapon must implement this, but they do it differently
    public abstract void OnAttack(PlayerController player, Vector2 aimDirection);
}