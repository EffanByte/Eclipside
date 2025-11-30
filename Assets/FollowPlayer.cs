using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    private Transform playerTransform;
    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        transform.position = playerTransform.position + new Vector3(0, 0, -10);
    }
}
