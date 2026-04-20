using System;
using System.Collections;
using UnityEngine;

public static class ProfileSyncBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void EnsureExists()
    {
        if (!BackendRuntimeSettings.IsEnabled)
        {
            return;
        }

        if (ProfileSyncManager.Instance != null)
        {
            return;
        }

        var go = new GameObject("ProfileSyncManager");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<ProfileSyncManager>();
    }
}

public class ProfileSyncManager : MonoBehaviour
{
    public static ProfileSyncManager Instance { get; private set; }

    [Header("Startup")]
    [SerializeField] private bool bootstrapOnStart = true;
    [SerializeField] private bool syncPendingChangesOnStart = true;

    [Header("Lifecycle Sync")]
    [SerializeField] private bool syncOnPause = true;
    [SerializeField] private bool markDirtyOnPause = true;
    [SerializeField] private bool markDirtyOnQuit = true;

    [Header("Network")]
    [SerializeField] private bool requireReachableInternet = true;
    [SerializeField] private bool verboseLogging = true;

    private bool syncInFlight;

    public event Action<BackendProfileResponse> OnProfileSyncSucceeded;
    public event Action<string> OnProfileSyncFailed;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
        BackendApiClient.EnsureLocalProfileIdentity();
    }

    private void Start()
    {
        if (!bootstrapOnStart)
        {
            return;
        }

        StartCoroutine(BootstrapRoutine(syncPendingChangesOnStart));
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus)
        {
            return;
        }

        if (markDirtyOnPause)
        {
            BackendApiClient.MarkProfileDirty();
        }

        if (syncOnPause)
        {
            SyncNow(true);
        }
    }

    private void OnApplicationQuit()
    {
        if (markDirtyOnQuit)
        {
            BackendApiClient.MarkProfileDirty();
        }
    }

    public void BootstrapNow(bool syncPendingChangesAfterBootstrap = true)
    {
        StartCoroutine(BootstrapRoutine(syncPendingChangesAfterBootstrap));
    }

    public void SyncNow(bool pushLocalChanges = true)
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        StartCoroutine(SyncRoutine(pushLocalChanges));
    }

    private IEnumerator BootstrapRoutine(bool syncPendingChangesAfterBootstrap)
    {
        if (syncInFlight)
        {
            yield break;
        }

        if (!CanReachBackend())
        {
            Log("Skipping profile bootstrap because internet is unavailable.");
            yield break;
        }

        bool hadPendingSync = SaveManager.Profile.user_profile.has_pending_sync;
        syncInFlight = true;

        BackendProfileResponse bootstrapResponse = null;
        string bootstrapError = null;

        yield return BackendApiClient.BootstrapProfile(
            response => bootstrapResponse = response,
            error => bootstrapError = error);

        syncInFlight = false;

        if (!string.IsNullOrWhiteSpace(bootstrapError))
        {
            Fail($"Profile bootstrap failed. {bootstrapError}");
            yield break;
        }

        BackendApiClient.ApplySyncedProfile(bootstrapResponse);
        OnProfileSyncSucceeded?.Invoke(bootstrapResponse);
        Log($"Profile bootstrap succeeded. RemoteProfileId={bootstrapResponse?.remoteProfileId} Version={bootstrapResponse?.profileVersion}");

        if (syncPendingChangesAfterBootstrap && hadPendingSync)
        {
            SaveManager.Profile.user_profile.has_pending_sync = true;
            SaveManager.SaveProfile();
            yield return SyncRoutine(true);
        }
    }

    private IEnumerator SyncRoutine(bool pushLocalChanges)
    {
        if (syncInFlight)
        {
            yield break;
        }

        if (!CanReachBackend())
        {
            Log("Skipping profile sync because internet is unavailable.");
            yield break;
        }

        syncInFlight = true;

        BackendProfileResponse syncResponse = null;
        string syncError = null;

        yield return BackendApiClient.SyncProfile(
            pushLocalChanges,
            response => syncResponse = response,
            error => syncError = error);

        syncInFlight = false;

        if (!string.IsNullOrWhiteSpace(syncError))
        {
            Fail($"Profile sync failed. {syncError}");
            yield break;
        }

        BackendApiClient.ApplySyncedProfile(syncResponse);
        OnProfileSyncSucceeded?.Invoke(syncResponse);

        if (syncResponse != null && syncResponse.conflict)
        {
            Debug.LogWarning("[ProfileSync] Server returned a newer profile version. Local mergeable changes were not pushed.");
        }

        Log($"Profile sync succeeded. PushLocalChanges={pushLocalChanges} Version={syncResponse?.profileVersion} Conflict={syncResponse?.conflict}");
    }

    private bool CanReachBackend()
    {
        return !requireReachableInternet || Application.internetReachability != NetworkReachability.NotReachable;
    }

    private void Fail(string message)
    {
        Debug.LogWarning($"[ProfileSync] {message}");
        OnProfileSyncFailed?.Invoke(message);
    }

    private void Log(string message)
    {
        if (verboseLogging)
        {
            Debug.Log($"[ProfileSync] {message}");
        }
    }
}
