using System;
using System.Collections;
using UnityEngine;

public static class DevAccountAuthBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void EnsureExists()
    {
        if (DevAccountAuthManager.Instance != null)
        {
            return;
        }

        var go = new GameObject("DevAccountAuthManager");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<DevAccountAuthManager>();
    }
}

public class DevAccountAuthManager : MonoBehaviour
{
    public static DevAccountAuthManager Instance { get; private set; }

    [SerializeField] private bool verboseLogging = true;

    public event Action<BackendAccountAuthResponse> OnAuthSucceeded;
    public event Action<string> OnAuthFailed;
    public event Action<string> OnStatusChanged;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        PublishStatus();
    }

    public void RegisterAndLinkCurrentGuest(string email, string password, string displayName)
    {
        StartCoroutine(RegisterRoutine(email, password, displayName, true));
    }

    public void RegisterWithoutLinking(string email, string password, string displayName)
    {
        StartCoroutine(RegisterRoutine(email, password, displayName, false));
    }

    public void LoginToExistingAccount(string email, string password)
    {
        StartCoroutine(LoginRoutine(email, password, false));
    }

    public void LoginAndLinkCurrentGuest(string email, string password)
    {
        StartCoroutine(LoginRoutine(email, password, true));
    }

    public void LinkCurrentGuestToExistingAccount(string email, string password)
    {
        StartCoroutine(LinkRoutine(email, password));
    }

    public void ResetToFreshGuest()
    {
        BackendApiClient.ResetToFreshGuestProfile();
        Log("Reset to a fresh local guest profile.");
        PublishStatus();

        ProfileSyncBootstrap.EnsureExists();
        if (ProfileSyncManager.Instance != null)
        {
            ProfileSyncManager.Instance.BootstrapNow();
        }
    }

    public string GetStatusSummary()
    {
        return BackendApiClient.GetLocalAccountSummary();
    }

    private IEnumerator RegisterRoutine(string email, string password, string displayName, bool linkCurrentPlayer)
    {
        BackendAccountAuthResponse response = null;
        string error = null;

        yield return BackendApiClient.RegisterDevAccount(
            email,
            password,
            displayName,
            linkCurrentPlayer,
            value => response = value,
            message => error = message);

        if (!string.IsNullOrWhiteSpace(error))
        {
            Fail($"Register failed. {error}");
            yield break;
        }

        HandleSuccess(response, $"Register succeeded. AccountId={response?.account?.accountId}");
    }

    private IEnumerator LoginRoutine(string email, string password, bool linkCurrentPlayer)
    {
        BackendAccountAuthResponse response = null;
        string error = null;

        yield return BackendApiClient.LoginDevAccount(
            email,
            password,
            linkCurrentPlayer,
            value => response = value,
            message => error = message);

        if (!string.IsNullOrWhiteSpace(error))
        {
            Fail($"Login failed. {error}");
            yield break;
        }

        HandleSuccess(response, $"Login succeeded. AccountId={response?.account?.accountId} LinkedPlayerId={response?.account?.linkedPlayerId}");
    }

    private IEnumerator LinkRoutine(string email, string password)
    {
        BackendAccountAuthResponse response = null;
        string error = null;

        yield return BackendApiClient.LinkCurrentProfileToAccount(
            email,
            password,
            value => response = value,
            message => error = message);

        if (!string.IsNullOrWhiteSpace(error))
        {
            Fail($"Link failed. {error}");
            yield break;
        }

        HandleSuccess(response, $"Link succeeded. AccountId={response?.account?.accountId}");
    }

    private void HandleSuccess(BackendAccountAuthResponse response, string logMessage)
    {
        BackendApiClient.ApplyAccountAuthResponse(response);
        Log(logMessage);
        OnAuthSucceeded?.Invoke(response);
        PublishStatus();

        ProfileSyncBootstrap.EnsureExists();
        if (ProfileSyncManager.Instance != null)
        {
            ProfileSyncManager.Instance.SyncNow(false);
        }
    }

    private void Fail(string message)
    {
        Debug.LogWarning($"[DevAuth] {message}");
        OnAuthFailed?.Invoke(message);
        PublishStatus();
    }

    private void Log(string message)
    {
        if (verboseLogging)
        {
            Debug.Log($"[DevAuth] {message}");
        }
    }

    private void PublishStatus()
    {
        OnStatusChanged?.Invoke(BackendApiClient.GetLocalAccountSummary());
    }
}
