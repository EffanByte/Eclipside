using UnityEngine;

public class DrawCollider : MonoBehaviour
{
    BoxCollider2D boxCollider;
    LineRenderer lineRenderer;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        boxCollider = GetComponent<BoxCollider2D>();
        lineRenderer = GetComponent<LineRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
     lineRendererUpdate();   
    }

    void lineRendererUpdate()
    {
        Vector2 boxSize = boxCollider.size;
        Vector2 boxOffset = boxCollider.offset;

        Vector3 topLeft = new Vector3(boxOffset.x - boxSize.x / 2, boxOffset.y + boxSize.y / 2, 0);
        Vector3 topRight = new Vector3(boxOffset.x + boxSize.x / 2, boxOffset.y + boxSize.y / 2, 0);
        Vector3 bottomRight = new Vector3(boxOffset.x + boxSize.x / 2, boxOffset.y - boxSize.y / 2, 0);
        Vector3 bottomLeft = new Vector3(boxOffset.x - boxSize.x / 2, boxOffset.y - boxSize.y / 2, 0);

        lineRenderer.positionCount = 5;
        lineRenderer.SetPosition(0, topLeft);
        lineRenderer.SetPosition(1, topRight);
        lineRenderer.SetPosition(2, bottomRight);
        lineRenderer.SetPosition(3, bottomLeft);
        lineRenderer.SetPosition(4, topLeft);
    }
}

