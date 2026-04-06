using TMPro;
using UnityEngine;

public class DevAccountAuthPanel : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private TMP_InputField displayNameInput;

    [Header("Output")]
    [SerializeField] private TextMeshProUGUI statusText;

    private void OnEnable()
    {
        DevAccountAuthBootstrap.EnsureExists();
        Subscribe();
        RefreshStatus();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Start()
    {
        DevAccountAuthBootstrap.EnsureExists();
        Subscribe();
        RefreshStatus();
    }

    public void OnRegisterAndLinkPressed()
    {
        if (DevAccountAuthManager.Instance == null) return;
        DevAccountAuthManager.Instance.RegisterAndLinkCurrentGuest(GetEmail(), GetPassword(), GetDisplayName());
    }

    public void OnRegisterOnlyPressed()
    {
        if (DevAccountAuthManager.Instance == null) return;
        DevAccountAuthManager.Instance.RegisterWithoutLinking(GetEmail(), GetPassword(), GetDisplayName());
    }

    public void OnLoginPressed()
    {
        if (DevAccountAuthManager.Instance == null) return;
        DevAccountAuthManager.Instance.LoginToExistingAccount(GetEmail(), GetPassword());
    }

    public void OnLoginAndLinkPressed()
    {
        if (DevAccountAuthManager.Instance == null) return;
        DevAccountAuthManager.Instance.LoginAndLinkCurrentGuest(GetEmail(), GetPassword());
    }

    public void OnLinkCurrentGuestPressed()
    {
        if (DevAccountAuthManager.Instance == null) return;
        DevAccountAuthManager.Instance.LinkCurrentGuestToExistingAccount(GetEmail(), GetPassword());
    }

    public void OnResetToFreshGuestPressed()
    {
        if (DevAccountAuthManager.Instance == null) return;
        DevAccountAuthManager.Instance.ResetToFreshGuest();
    }

    public void OnRefreshStatusPressed()
    {
        RefreshStatus();
    }

    private void HandleAuthSucceeded(FirebaseAuthSession response)
    {
        RefreshStatus();
    }

    private void HandleAuthFailed(string message)
    {
        if (statusText != null)
        {
            statusText.text = $"{message}\n\n{BackendApiClient.GetLocalAccountSummary()}";
        }
    }

    private void HandleStatusChanged(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void RefreshStatus()
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = DevAccountAuthManager.Instance != null
            ? DevAccountAuthManager.Instance.GetStatusSummary()
            : BackendApiClient.GetLocalAccountSummary();
    }

    private string GetEmail() => emailInput != null ? emailInput.text.Trim() : string.Empty;
    private string GetPassword() => passwordInput != null ? passwordInput.text : string.Empty;
    private string GetDisplayName() => displayNameInput != null ? displayNameInput.text.Trim() : string.Empty;

    private void Subscribe()
    {
        if (DevAccountAuthManager.Instance == null)
        {
            return;
        }

        DevAccountAuthManager.Instance.OnAuthSucceeded -= HandleAuthSucceeded;
        DevAccountAuthManager.Instance.OnAuthFailed -= HandleAuthFailed;
        DevAccountAuthManager.Instance.OnStatusChanged -= HandleStatusChanged;

        DevAccountAuthManager.Instance.OnAuthSucceeded += HandleAuthSucceeded;
        DevAccountAuthManager.Instance.OnAuthFailed += HandleAuthFailed;
        DevAccountAuthManager.Instance.OnStatusChanged += HandleStatusChanged;
    }

    private void Unsubscribe()
    {
        if (DevAccountAuthManager.Instance == null)
        {
            return;
        }

        DevAccountAuthManager.Instance.OnAuthSucceeded -= HandleAuthSucceeded;
        DevAccountAuthManager.Instance.OnAuthFailed -= HandleAuthFailed;
        DevAccountAuthManager.Instance.OnStatusChanged -= HandleStatusChanged;
    }
}
