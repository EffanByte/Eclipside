using UnityEngine;

public class PauseMenu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameObject.SetActive(false); // Ensure pause menu is hidden at start
    }


    public void OnPauseButtonPressed()
    {
        // Logic to pause the game
        Time.timeScale = 0f; // Freeze game time
        gameObject.SetActive(true); // Show pause menu
    }
    
    public void OnResumeButtonPressed()
    {
        // Logic to resume the game
        Time.timeScale = 1f; // Resume game time
        gameObject.SetActive(false); // Hide pause menu
    }
    public void OnReturnToMainMenuButtonPressed()
    {
        // Logic to return to main menu
        Time.timeScale = 1f; // Ensure game time is normal
        RunSceneTransitionState.Clear();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"); // Load main menu scene
    }
    public void OnRetryButtonPressed()
    {
        // Logic to retry the current level
        Time.timeScale = 1f; // Ensure game time is normal
        if (GameDirector.Instance != null)
        {
            RunSceneTransitionState.SetBiomeState(GameDirector.Instance.CurrentBiomeIndex, GameDirector.Instance.CurrentDifficultyValue);
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name); // Reload current scene
    }
}
