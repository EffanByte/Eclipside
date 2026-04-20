using System;
using System.Collections;
using UnityEngine;

public static class DevAccountAuthBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void EnsureExists()
    {
        if (!BackendRuntimeSettings.IsEnabled)
        {
            return;
        }

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

    public event Action<FirebaseAuthSession> OnAuthSucceeded;
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
        FirebaseAuthSession response = null;
        string error = null;

        yield return FirebaseAuthApiClient.RegisterEmailPassword(
            email,
            password,
            displayName,
            value => response = value,
            message => error = message);

        if (!string.IsNullOrWhiteSpace(error))
        {
            Fail($"Register failed. {error}");
            yield break;
        }

        HandleSuccess(response, displayName, linkCurrentPlayer, $"Register succeeded. FirebaseUid={response?.localId} LinkCurrentPlayer={linkCurrentPlayer}");
    }

    private IEnumerator LoginRoutine(string email, string password, bool linkCurrentPlayer)
    {
        FirebaseAuthSession response = null;
        string error = null;

        yield return FirebaseAuthApiClient.LoginEmailPassword(
            email,
            password,
            value => response = value,
            message => error = message);

        if (!string.IsNullOrWhiteSpace(error))
        {
            Fail($"Login failed. {error}");
            yield break;
        }

        HandleSuccess(response, SaveManager.Profile.user_profile.username, linkCurrentPlayer, $"Login succeeded. FirebaseUid={response?.localId} LinkCurrentPlayer={linkCurrentPlayer}");
    }

    private IEnumerator LinkRoutine(string email, string password)
    {
        yield return LoginRoutine(email, password, true);
    }

    private void HandleSuccess(FirebaseAuthSession response, string fallbackDisplayName, bool linkCurrentPlayer, string logMessage)
    {
        BackendApiClient.ApplyFirebaseAuthSession(response, fallbackDisplayName);
        Log(logMessage);
        OnAuthSucceeded?.Invoke(response);
        PublishStatus();

        ProfileSyncBootstrap.EnsureExists();
        if (ProfileSyncManager.Instance != null)
        {
            ProfileSyncManager.Instance.BootstrapNow(linkCurrentPlayer);
        }
    }

    private void Fail(string message)
    {
        Debug.LogWarning($"[FirebaseAuth] {message}");
        OnAuthFailed?.Invoke(message);
        PublishStatus();
    }

    private void Log(string message)
    {
        if (verboseLogging)
        {
            Debug.Log($"[FirebaseAuth] {message}");
        }
    }

    private void PublishStatus()
    {
        OnStatusChanged?.Invoke(BackendApiClient.GetLocalAccountSummary());
    }
}
