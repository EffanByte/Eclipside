using UnityEngine;

public class MainMenu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void OnStartGameButtonPressed()
    {
        // Logic to start the game
        UnityEngine.SceneManagement.SceneManager.LoadScene("Demo"); // Load main game scene
    }
}
