using UnityEngine;
using System.Collections; 

public abstract class WeaponData : ScriptableObject
{
    [Header("Core Info")]
    public string weaponName;
    public Sprite icon;
    
    // FIX: Re-added this field so PlayerController works
    public AnimatorOverrideController animatorOverride; 

    [Header("Visuals & Physics")]
    public GameObject weaponPrefab; 

    [Header("Stats")]
    public float damage;
    public float cooldown;
    public float knockbackForce;
    public float hitDuration = 0.2f; 
    
    [Header("Damage Type")]
    public DamageElement element = DamageElement.Physical; 
    public AttackStyle style = AttackStyle.MeleeLight;

    public abstract IEnumerator OnAttack(PlayerController player, WeaponHitbox activeHitbox);
}   