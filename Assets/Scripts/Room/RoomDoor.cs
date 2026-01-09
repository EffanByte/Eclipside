using UnityEngine;

public class RoomDoor : MonoBehaviour
{
    [SerializeField] private GameObject doorVisual; // The wall/gate sprite
    [SerializeField] private Collider2D doorCollider; // The physics blocker

    public void SetLocked(bool locked)
    {
        if(doorVisual) doorVisual.SetActive(locked);
        if(doorCollider) doorCollider.enabled = locked;
    }
}