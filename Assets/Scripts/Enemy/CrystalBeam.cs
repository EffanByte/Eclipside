using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class CrystalBeam : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private PolygonCollider2D polyCollider;

    private float damage;
    private float duration;
    
    // GDD: "Can damage at most once per cast"
    private bool hasHitPlayer = false; 

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;

        // Dynamically add the 2D Collider we will shape later
        polyCollider = gameObject.AddComponent<PolygonCollider2D>();
        polyCollider.isTrigger = true;
        polyCollider.enabled = false; // Off until the mesh is baked
    }

    public void FireBeam(Vector3 startPos, Vector2 dir, float beamWidth, float dmg, float dur)
    {
        damage = dmg;
        duration = dur;

        lineRenderer.startWidth = beamWidth;
        lineRenderer.endWidth = beamWidth;

        // 1. Calculate End Point (Raycast to walls)
        float maxRange = 30f;
        LayerMask wallMask = LayerMask.GetMask("Environment", "Wall");
        RaycastHit2D hit = Physics2D.Raycast(startPos, dir.normalized, maxRange, wallMask);
        
        Vector3 endPos = hit.collider != null ? (Vector3)hit.point : startPos + (Vector3)(dir.normalized * maxRange);

        // 2. Draw Line
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);

        // 3. Bake the Line into a 2D Collider
        BakeLineToPolygonCollider();

        // 4. Start the active timer
        StartCoroutine(ActiveRoutine());
    }

    private void BakeLineToPolygonCollider()
    {
        // A. Bake the 3D Mesh from the LineRenderer
        Mesh mesh = new Mesh();
            lineRenderer.BakeMesh(mesh, Camera.current, true); // true = use transform scale

        // B. Extract the 3D vertices from the mesh
        Vector3[] vertices3D = mesh.vertices;
        
        // C. Convert 3D vertices to 2D paths for the PolygonCollider
        // A LineRenderer with 2 points creates a quad (4 vertices)
            Vector2[] path = new Vector2[4];

            // Map the outer corners of the baked quad.
            // Depending on the camera facing/bake orientation, the indices might vary slightly,
            // but for a simplasdae 2-point line, 0, 1, 2, 3 form the rectangle.
            path[0] = transform.InverseTransformPoint(vertices3D[0]);
            path[1] = transform.InverseTransformPoint(vertices3D[1]);
            path[2] = transform.InverseTransformPoint(vertices3D[2]);
            path[3] = transform.InverseTransformPoint(vertices3D[3]);

            // D. Assign the shape to the 2D collider
            polyCollider.SetPath(0, path);
            polyCollider.enabled = true;
            Debug.LogWarning("[Crystal Beam] BakeMesh failed to produce enough vertices for a 2D Collider.");
    }

    private IEnumerator ActiveRoutine()
    {
        // Wait for the duration of the beam
        yield return new WaitForSeconds(duration);
        Destroy(gameObject);
    }

    // ---------------------------------------------------------
    // 2D COLLISION DETECTION
    // ---------------------------------------------------------
    private void OnTriggerStay2D(Collider2D collision)
    {
        // We use OnTriggerStay2D in case the player was already standing 
        // inside the area when the collider was enabled.
        if (!hasHitPlayer && collision.CompareTag("Player"))
        {
            hasHitPlayer = true;
            
            DamageInfo info = new DamageInfo(damage, DamageElement.Magic, AttackStyle.Ranged, lineRenderer.GetPosition(0), 0f);
            
            PlayerController pc = collision.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.ReceiveDamage(info);
                Debug.Log("<color=cyan>[Crystal Beam] Hit Player via PolygonCollider2D!</color>");
            }
        }
    }
}