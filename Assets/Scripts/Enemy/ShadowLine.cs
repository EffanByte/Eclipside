using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ShadowLine : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private PolygonCollider2D polyCollider;
    private Transform followTarget;
    private Vector3 startPos;
    private float width;
    private bool isGrowing = false;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;

        polyCollider = gameObject.AddComponent<PolygonCollider2D>();
        polyCollider.isTrigger = true;
    }

    // Called when the arrow is fired
    public void StartGrowing(Vector3 start, Transform target, float lineWidth)
    {
        startPos = start;
        followTarget = target;
        width = lineWidth;
        isGrowing = true;

        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.SetPosition(0, startPos);
    }

    private void Update()
    {
        if (!isGrowing) return;

        if (followTarget != null)
        {
            // Keep the end of the line pinned to the arrow
            lineRenderer.SetPosition(1, followTarget.position);
            
            // Re-bake the collider so it matches the current length
            BakeLineToCollider();
        }
        else
        {
            // Arrow hit something or max range reached
            StopGrowing();
        }
    }

    public void StopGrowing()
    {
        isGrowing = false;
        // GDD: Shadow line exists for 4.0 seconds total
        Destroy(gameObject, 4.0f); 
    }

    private void BakeLineToCollider()
    {
        Mesh mesh = new Mesh();
        if (Camera.main != null)
        {
            // Same logic as Crystal Beam
            lineRenderer.BakeMesh(mesh, Camera.main, true);
        }
        else return;

        Vector3[] vertices3D = mesh.vertices;
        
        if (vertices3D.Length >= 4)
        {
            Vector2[] path = new Vector2[4];
            // PERIMETER order: 0, 1, 3, 2
            path[0] = transform.InverseTransformPoint(vertices3D[0]);
            path[1] = transform.InverseTransformPoint(vertices3D[1]);
            path[2] = transform.InverseTransformPoint(vertices3D[3]); 
            path[3] = transform.InverseTransformPoint(vertices3D[2]); 

            polyCollider.SetPath(0, path);
        }
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
        {
            PlayerController.Instance.TryAddStatus(StatusType.Freeze);
            Debug.Log("<color=purple>[Shadow Line] Player Paralyzed!</color>");
        }
    }
}