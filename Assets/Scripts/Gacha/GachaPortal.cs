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
            Debug.Log(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            DontDestroyOnLoad(PlayerController.Instance.gameObject);
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "GachaScene")
            UnityEngine.SceneManagement.SceneManager.LoadScene("GachaScene");
            else
            UnityEngine.SceneManagement.SceneManager.LoadScene("Demo");
        }
    }
}
