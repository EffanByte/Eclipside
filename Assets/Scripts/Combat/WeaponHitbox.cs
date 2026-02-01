    using UnityEngine;
    using System.Collections.Generic;
    using System;
    public class WeaponHitbox : MonoBehaviour
    {
        private DamageInfo currentDamageInfo;
        private List<Collider2D> hitList = new List<Collider2D>();
        private Collider2D myCollider;
        
        // Cache the filter to avoid creating garbage memory every attack
        private ContactFilter2D filter;
        private List<Collider2D> overlapResults = new List<Collider2D>();
        private PlayerController playerController;
        private void Awake()
        {
            myCollider = GetComponent<Collider2D>();
            myCollider.enabled = false; 
            myCollider.isTrigger = true;
            playerController = GetComponentInParent<PlayerController>();
            // Set up a filter that checks everything (or specific layers if you prefer)
            filter = ContactFilter2D.noFilter;
        }

        public void Start()
        {
            hitList.Clear();
            myCollider.enabled = true; // Ensure it's on

            Physics2D.OverlapCollider(myCollider, filter, overlapResults);
        }

        public void DisableHitbox()
        {
            myCollider.enabled = false;
        }

        // Unity calls this automatically when something enters
        private void OnTriggerEnter2D(Collider2D collision)
        {
            TryDealDamage(collision);
        }

        // Extracted logic so we can call it from both OnTriggerEnter and Initialize
        private void TryDealDamage(Collider2D collision)
        {
            // Won't hold up sourceposition when attack is something besides a sword
        currentDamageInfo = playerController.currentWeapon.GetDamageInfoOnHit(playerController, collision.GetComponent<EnemyBase>());

            EnemyBase target = collision.GetComponent<EnemyBase>();

            if (target != null)
            {
                    target.ReceiveDamage(currentDamageInfo);
            }   
        }
        private DamageElement GetRandomDamageElement()
    {
        Array values = Enum.GetValues(typeof(DamageElement));
        return (DamageElement)values.GetValue(UnityEngine.Random.Range(0, values.Length));
    }

    }