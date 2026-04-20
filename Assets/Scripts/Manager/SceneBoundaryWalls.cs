using UnityEngine;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
public class SceneBoundaryWalls : MonoBehaviour
{
    [SerializeField] private float wallThickness = 2f;
    [SerializeField] private float padding = 1f;

    private Transform wallRoot;

    private void Start()
    {
        BuildWalls();
    }

    public void BuildWalls()
    {
        Bounds? combinedBounds = GetCombinedTilemapBounds();
        if (!combinedBounds.HasValue)
        {
            return;
        }

        if (wallRoot != null)
        {
            Destroy(wallRoot.gameObject);
        }

        wallRoot = new GameObject("RuntimeBoundaryWalls").transform;
        wallRoot.SetParent(transform, false);

        Bounds bounds = combinedBounds.Value;
        bounds.Expand(new Vector3(padding * 2f, padding * 2f, 0f));

        CreateWall("TopWall", new Vector2(bounds.center.x, bounds.max.y + (wallThickness * 0.5f)), new Vector2(bounds.size.x + wallThickness * 2f, wallThickness));
        CreateWall("BottomWall", new Vector2(bounds.center.x, bounds.min.y - (wallThickness * 0.5f)), new Vector2(bounds.size.x + wallThickness * 2f, wallThickness));
        CreateWall("LeftWall", new Vector2(bounds.min.x - (wallThickness * 0.5f), bounds.center.y), new Vector2(wallThickness, bounds.size.y));
        CreateWall("RightWall", new Vector2(bounds.max.x + (wallThickness * 0.5f), bounds.center.y), new Vector2(wallThickness, bounds.size.y));
    }

    private Bounds? GetCombinedTilemapBounds()
    {
        TilemapRenderer[] renderers = FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
        Bounds combined = default;
        bool foundAny = false;

        foreach (TilemapRenderer tilemapRenderer in renderers)
        {
            if (tilemapRenderer == null || !tilemapRenderer.enabled)
            {
                continue;
            }

            Bounds bounds = tilemapRenderer.bounds;
            if (bounds.size.sqrMagnitude <= 0.001f)
            {
                continue;
            }

            if (!foundAny)
            {
                combined = bounds;
                foundAny = true;
            }
            else
            {
                combined.Encapsulate(bounds.min);
                combined.Encapsulate(bounds.max);
            }
        }

        return foundAny ? combined : null;
    }

    private void CreateWall(string wallName, Vector2 position, Vector2 size)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(wallRoot, false);
        wall.transform.position = position;
        wall.tag = "Wall";

        BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
        collider.size = size;
    }
}
