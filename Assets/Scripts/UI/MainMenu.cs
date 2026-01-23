using UnityEngine;


public class MainMenu : MonoBehaviour
{   
    [SerializeField] private GameObject achievementPanel;
    [SerializeField] private GameObject ChallengePanel;

       void Start()
    {
        
    }

    public void OnStartGameButtonPressed()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Demo"); // Load main game scene
    }

    public void OnAchievementButtonPressed()
    {
        achievementPanel.SetActive(true);
        ChallengePanel.SetActive(false);
    }

    public void OnChallengeButtonPressed()
    {
        ChallengePanel.SetActive(true);
        achievementPanel.SetActive(false);
    }

    public void OnExitAchievementPanel()
    {
        achievementPanel.SetActive(false);
    }
    public void OnExitChallengePanel()
    {
        ChallengePanel.SetActive(false);
    }

    public void OnGachaButtonPressed()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Demo"); // Load gacha scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("GachaScene"); // Load gacha scene
    }
}
