using UnityEngine;

public class GachaPortal : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            DontDestroyOnLoad(PlayerController.Instance.gameObject);
            UnityEngine.SceneManagement.SceneManager.LoadScene("GachaScene");
        }
    }
}
