using UnityEngine;

public class DeathMenu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        PlayerHealth.OnPlayerDeath += OnPlayerDeathHandler;
        gameObject.SetActive(false); // Hide death menu at start
    }

    private void OnPlayerDeathHandler()
    {
        Time.timeScale = 0f; // Freeze game time
        gameObject.SetActive(true); // Show death menu
    }
    public void OnPauseButtonPressed()
    {
        // Logic to pause the game
        Time.timeScale = 0f; // Freeze game time
        gameObject.SetActive(true); // Show pause menu
    }

    public void OnReturnToMainMenuButtonPressed()
    {
        // Logic to return to main menu
        Time.timeScale = 1f; // Ensure game time is normal
        PlayerController.Instance.ResetPlayer();
        RunSceneTransitionState.Clear();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu"); // Load main menu scene
    }
    public void OnRetryButtonPressed()
    {
        // Logic to retry the current level
        Time.timeScale = 1f; // Ensure game time is normal
        PlayerController.Instance.ResetPlayer();
        if (GameDirector.Instance != null)
        {
            RunSceneTransitionState.SetBiomeState(GameDirector.Instance.CurrentBiomeIndex, GameDirector.Instance.CurrentDifficultyValue);
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name); // Reload current scene
    }
}
