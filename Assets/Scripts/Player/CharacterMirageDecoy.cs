using System.Collections;
using UnityEngine;

public class CharacterMirageDecoy : MonoBehaviour
{
    private PlayerCharacterRuntime ownerRuntime;
    private bool resolved;

    public void Initialize(PlayerCharacterRuntime runtime, float duration, float radius)
    {
        ownerRuntime = runtime;

        CircleCollider2D collider = gameObject.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = radius;

        StartCoroutine(LifetimeRoutine(duration));
    }

    private IEnumerator LifetimeRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        Resolve(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        EnemyBase enemy = collision.GetComponent<EnemyBase>() ?? collision.GetComponentInParent<EnemyBase>();
        if (enemy != null)
        {
            Resolve(true);
        }
    }

    private void Resolve(bool destroyedPrematurely)
    {
        if (resolved)
        {
            return;
        }

        resolved = true;
        ownerRuntime?.ResolveMirage(this, destroyedPrematurely, transform.position);
        Destroy(gameObject);
    }
}
