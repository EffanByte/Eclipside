using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public static class BackendApiClient
{
    private const string DefaultBaseUrl = "http://localhost:8080";
    private const string BaseUrlPlayerPrefKey = "backend.base_url";

    public static string BaseUrl => PlayerPrefs.GetString(BaseUrlPlayerPrefKey, DefaultBaseUrl).TrimEnd('/');

    public static void EnsureLocalProfileIdentity()
    {
        var profile = SaveManager.Profile;
        var user = profile.user_profile;
        bool changed = false;
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        string stableId = !string.IsNullOrWhiteSpace(user.device_profile_id)
            ? user.device_profile_id
            : user.user_id;

        if (string.IsNullOrWhiteSpace(stableId))
        {
            stableId = Guid.NewGuid().ToString("N");
            changed = true;
        }

        if (user.user_id != stableId)
        {
            user.user_id = stableId;
            changed = true;
        }

        if (user.device_profile_id != stableId)
        {
            user.device_profile_id = stableId;
            changed = true;
        }

        if (user.date_created <= 0)
        {
            user.date_created = now;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(user.username))
        {
            user.username = $"Guest-{stableId.Substring(0, Math.Min(8, stableId.Length))}";
            changed = true;
        }

        if (user.last_login_timestamp <= 0)
        {
            user.last_login_timestamp = now;
            changed = true;
        }

        if (changed)
        {
            SaveManager.SaveProfile();
        }
    }

    public static string GetOrCreatePlayerId()
    {
        EnsureLocalProfileIdentity();
        return SaveManager.Profile.user_profile.user_id;
    }

    public static void MarkProfileDirty()
    {
        EnsureLocalProfileIdentity();
        SaveManager.Profile.user_profile.has_pending_sync = true;
        SaveManager.SaveProfile();
    }

    public static void ClearPendingSyncFlag()
    {
        SaveManager.Profile.user_profile.has_pending_sync = false;
        SaveManager.SaveProfile();
    }

    public static void ResetToFreshGuestProfile()
    {
        SaveManager.ReplaceProfile(new SaveFile_Profile());
        EnsureLocalProfileIdentity();

        var user = SaveManager.Profile.user_profile;
        user.remote_profile_id = string.Empty;
        user.account_id = string.Empty;
        user.account_email = string.Empty;
        user.is_guest = true;
        user.last_sync_unix = 0;
        user.last_synced_profile_version = 0;
        user.has_pending_sync = false;
        SaveManager.SaveProfile();
    }

    public static string GetLocalAccountSummary()
    {
        EnsureLocalProfileIdentity();

        var user = SaveManager.Profile.user_profile;
        string mode = string.IsNullOrWhiteSpace(user.account_id) ? "Guest" : "Account";
        string email = string.IsNullOrWhiteSpace(user.account_email) ? "none" : user.account_email;
        string remote = string.IsNullOrWhiteSpace(user.remote_profile_id) ? "none" : user.remote_profile_id;

        return $"Mode: {mode}\nUserId: {user.user_id}\nDeviceId: {user.device_profile_id}\nAccountId: {user.account_id}\nEmail: {email}\nRemoteProfileId: {remote}\nProfileVersion: {user.last_synced_profile_version}\nPendingSync: {user.has_pending_sync}";
    }

    public static void ApplyWalletToProfile(BackendWalletState wallet)
    {
        if (wallet == null)
        {
            return;
        }

        var profile = SaveManager.Profile;
        profile.user_profile.gold = wallet.Gold;
        profile.user_profile.orbs = wallet.Orbs;
        profile.user_profile.arena_tickets = wallet.Ticket;
    }

    public static void ApplyGachaToProfile(BackendGachaState gacha)
    {
        if (gacha == null)
        {
            return;
        }

        var profile = SaveManager.Profile;
        profile.gacha_state.total_pulls_lifetime = gacha.totalPullsLifetime;
        profile.gacha_state.current_pity_counter = gacha.currentPityCounter;
        profile.gacha_state.consecutive_pulls_no_epic = gacha.consecutivePullsNoEpic;
    }

    private static void ApplyProfileState(
        string playerId,
        string deviceProfileId,
        string remoteProfileId,
        string accountId,
        string displayName,
        bool isGuest,
        int profileVersion,
        long serverUnixTime,
        BackendProfileSyncableData profileData,
        BackendWalletState wallet,
        BackendGachaState gacha)
    {
        EnsureLocalProfileIdentity();

        var profile = SaveManager.Profile;
        var user = profile.user_profile;

        if (!string.IsNullOrWhiteSpace(playerId))
        {
            user.user_id = playerId;
        }

        if (string.IsNullOrWhiteSpace(user.device_profile_id) && !string.IsNullOrWhiteSpace(deviceProfileId))
        {
            user.device_profile_id = deviceProfileId;
        }

        user.remote_profile_id = remoteProfileId;
        user.account_id = accountId;
        user.is_guest = isGuest;
        user.last_sync_unix = serverUnixTime;
        user.last_synced_profile_version = profileVersion;
        user.has_pending_sync = false;

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            user.username = displayName;
        }

        if (profileData != null)
        {
            profile.user_profile.username = string.IsNullOrWhiteSpace(profileData.username)
                ? profile.user_profile.username
                : profileData.username;
            profile.monthly_pass = profileData.monthlyPass ?? profile.monthly_pass;
            profile.characters = profileData.characters ?? profile.characters;
            profile.weapons = profileData.weapons ?? profile.weapons;
            profile.consumables = profileData.consumables ?? profile.consumables;
            profile.progression = profileData.progression ?? profile.progression;
        }

        ApplyWalletToProfile(wallet);
        ApplyGachaToProfile(gacha);
        SaveManager.SaveProfile();
    }

    public static BackendProfileSyncableData BuildSyncableProfileData()
    {
        var profile = SaveManager.Profile;
        return new BackendProfileSyncableData
        {
            username = profile.user_profile.username,
            monthlyPass = profile.monthly_pass,
            characters = profile.characters,
            weapons = profile.weapons,
            consumables = profile.consumables,
            progression = profile.progression
        };
    }

    public static void ApplySyncedProfile(BackendProfileResponse response)
    {
        if (response == null)
        {
            return;
        }

        ApplyProfileState(
            response.playerId,
            response.deviceProfileId,
            response.remoteProfileId,
            response.accountId,
            response.displayName,
            response.isGuest,
            response.profileVersion,
            response.serverUnixTime,
            response.profile,
            response.wallet,
            response.gacha);
    }

    public static void ApplyAccountAuthResponse(BackendAccountAuthResponse response)
    {
        if (response == null)
        {
            return;
        }

        if (response.account != null)
        {
            var user = SaveManager.Profile.user_profile;
            user.account_id = response.account.accountId;
            user.account_email = response.account.email;
            if (!string.IsNullOrWhiteSpace(response.account.displayName))
            {
                user.username = response.account.displayName;
            }
        }

        if (!string.IsNullOrWhiteSpace(response.playerId) || response.profile != null || response.wallet != null || response.gacha != null)
        {
            ApplyProfileState(
                response.playerId,
                response.deviceProfileId,
                response.remoteProfileId,
                !string.IsNullOrWhiteSpace(response.accountId) ? response.accountId : response.account?.accountId,
                !string.IsNullOrWhiteSpace(response.displayName) ? response.displayName : response.account?.displayName,
                response.isGuest,
                response.profileVersion,
                response.serverUnixTime,
                response.profile,
                response.wallet,
                response.gacha);
            return;
        }

        SaveManager.SaveProfile();
    }

    public static IEnumerator InitializePlayer(Action<BackendInitResponse> onSuccess, Action<string> onError)
    {
        EnsureLocalProfileIdentity();
        var profile = SaveManager.Profile;
        var request = new BackendInitRequest
        {
            playerId = GetOrCreatePlayerId(),
            seedWallet = new BackendWalletState
            {
                Gold = profile.user_profile.gold,
                Orbs = profile.user_profile.orbs,
                Ticket = profile.user_profile.arena_tickets
            },
            seedGacha = new BackendGachaState
            {
                totalPullsLifetime = profile.gacha_state.total_pulls_lifetime,
                currentPityCounter = profile.gacha_state.current_pity_counter,
                consecutivePullsNoEpic = profile.gacha_state.consecutive_pulls_no_epic
            }
        };

        yield return PostJson("/player/init", request, onSuccess, onError);
    }

    public static IEnumerator BootstrapProfile(Action<BackendProfileResponse> onSuccess, Action<string> onError)
    {
        EnsureLocalProfileIdentity();

        var profile = SaveManager.Profile;
        var request = new BackendProfileBootstrapRequest
        {
            playerId = GetOrCreatePlayerId(),
            deviceProfileId = profile.user_profile.device_profile_id,
            accountId = profile.user_profile.account_id,
            displayName = profile.user_profile.username,
            lastKnownProfileVersion = profile.user_profile.last_synced_profile_version,
            seedWallet = new BackendWalletState
            {
                Gold = profile.user_profile.gold,
                Orbs = profile.user_profile.orbs,
                Ticket = profile.user_profile.arena_tickets
            },
            seedGacha = new BackendGachaState
            {
                totalPullsLifetime = profile.gacha_state.total_pulls_lifetime,
                currentPityCounter = profile.gacha_state.current_pity_counter,
                consecutivePullsNoEpic = profile.gacha_state.consecutive_pulls_no_epic
            },
            profile = BuildSyncableProfileData()
        };

        yield return PostJson("/profile/bootstrap", request, onSuccess, onError);
    }

    public static IEnumerator SyncProfile(bool pushLocalChanges, Action<BackendProfileResponse> onSuccess, Action<string> onError)
    {
        EnsureLocalProfileIdentity();

        var profile = SaveManager.Profile;
        var request = new BackendProfileSyncRequest
        {
            playerId = GetOrCreatePlayerId(),
            deviceProfileId = profile.user_profile.device_profile_id,
            accountId = profile.user_profile.account_id,
            displayName = profile.user_profile.username,
            lastKnownProfileVersion = profile.user_profile.last_synced_profile_version,
            pushLocalChanges = pushLocalChanges,
            profile = pushLocalChanges ? BuildSyncableProfileData() : null
        };

        yield return PostJson("/profile/sync", request, onSuccess, onError);
    }

    public static IEnumerator RegisterDevAccount(string email, string password, string displayName, bool linkCurrentPlayer, Action<BackendAccountAuthResponse> onSuccess, Action<string> onError)
    {
        EnsureLocalProfileIdentity();

        var request = new BackendAccountAuthRequest
        {
            playerId = GetOrCreatePlayerId(),
            email = email,
            password = password,
            displayName = displayName,
            linkCurrentPlayer = linkCurrentPlayer
        };

        yield return PostJson("/auth/register", request, onSuccess, onError);
    }

    public static IEnumerator LoginDevAccount(string email, string password, bool linkCurrentPlayer, Action<BackendAccountAuthResponse> onSuccess, Action<string> onError)
    {
        EnsureLocalProfileIdentity();

        var request = new BackendAccountAuthRequest
        {
            playerId = GetOrCreatePlayerId(),
            email = email,
            password = password,
            displayName = SaveManager.Profile.user_profile.username,
            linkCurrentPlayer = linkCurrentPlayer
        };

        yield return PostJson("/auth/login", request, onSuccess, onError);
    }

    public static IEnumerator LinkCurrentProfileToAccount(string email, string password, Action<BackendAccountAuthResponse> onSuccess, Action<string> onError)
    {
        EnsureLocalProfileIdentity();

        var request = new BackendAccountLinkRequest
        {
            playerId = GetOrCreatePlayerId(),
            email = email,
            password = password
        };

        yield return PostJson("/profile/link-account", request, onSuccess, onError);
    }

    public static IEnumerator RequestMissionsState(Action<BackendMissionsStateResponse> onSuccess, Action<string> onError)
    {
        string path = $"/missions/state?playerId={UnityWebRequest.EscapeURL(GetOrCreatePlayerId())}";
        yield return GetJson(path, onSuccess, onError);
    }

    public static IEnumerator PostMissionProgress(string statKey, int amount, Action<BackendMissionProgressResponse> onSuccess, Action<string> onError)
    {
        var request = new BackendMissionProgressRequest
        {
            playerId = GetOrCreatePlayerId(),
            statKey = statKey,
            amount = amount
        };

        yield return PostJson("/missions/progress", request, onSuccess, onError);
    }

    public static IEnumerator ClaimMission(string missionId, Action<BackendMissionClaimResponse> onSuccess, Action<string> onError)
    {
        var request = new BackendMissionClaimRequest
        {
            playerId = GetOrCreatePlayerId(),
            missionId = missionId
        };

        yield return PostJson("/missions/claim", request, onSuccess, onError);
    }

    public static IEnumerator RerollMission(string missionId, Action<BackendMissionRerollResponse> onSuccess, Action<string> onError)
    {
        var request = new BackendMissionRerollRequest
        {
            playerId = GetOrCreatePlayerId(),
            missionId = missionId
        };

        yield return PostJson("/missions/reroll", request, onSuccess, onError);
    }

    public static IEnumerator GachaPull(string bannerId, int pullCount, Action<BackendGachaPullResponse> onSuccess, Action<string> onError)
    {
        var request = new BackendGachaPullRequest
        {
            playerId = GetOrCreatePlayerId(),
            bannerId = bannerId,
            pullCount = pullCount
        };

        yield return PostJson("/gacha/pull", request, onSuccess, onError);
    }

    private static IEnumerator GetJson<T>(string relativePath, Action<T> onSuccess, Action<string> onError) where T : class
    {
        using (var request = UnityWebRequest.Get(BaseUrl + relativePath))
        {
            Debug.Log($"[BackendApi] GET {request.url}");
            yield return request.SendWebRequest();
            HandleResponse(request, onSuccess, onError);
        }
    }

    private static IEnumerator PostJson<TRequest, TResponse>(string relativePath, TRequest payload, Action<TResponse> onSuccess, Action<string> onError) where TResponse : class
    {
        string json = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (var request = new UnityWebRequest(BaseUrl + relativePath, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"[BackendApi] POST {request.url} {json}");
            yield return request.SendWebRequest();
            HandleResponse(request, onSuccess, onError);
        }
    }

    private static void HandleResponse<T>(UnityWebRequest request, Action<T> onSuccess, Action<string> onError) where T : class
    {
        if (request.result != UnityWebRequest.Result.Success)
        {
            string errorBody = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            string message = string.IsNullOrWhiteSpace(errorBody) ? request.error : errorBody;
            Debug.LogWarning($"[BackendApi] ERROR {request.method} {request.url} {message}");
            onError?.Invoke(message);
            return;
        }

        string responseText = request.downloadHandler.text;
        Debug.Log($"[BackendApi] OK {request.method} {request.url} {responseText}");
        if (typeof(T) == typeof(string))
        {
            onSuccess?.Invoke(responseText as T);
            return;
        }

        if (string.IsNullOrWhiteSpace(responseText))
        {
            onError?.Invoke("Empty backend response.");
            return;
        }

        T response = JsonUtility.FromJson<T>(responseText);
        if (response == null)
        {
            onError?.Invoke("Failed to parse backend response.");
            return;
        }

        onSuccess?.Invoke(response);
    }
}

[Serializable]
public class BackendInitRequest
{
    public string playerId;
    public BackendWalletState seedWallet;
    public BackendGachaState seedGacha;
}

[Serializable]
public class BackendInitResponse
{
    public string playerId;
    public BackendWalletState wallet;
}

[Serializable]
public class BackendProfileSyncableData
{
    public string username;
    public MonthlyPass monthlyPass;
    public SaveCharacterData characters;
    public WeaponInventory weapons;
    public ConsumableInventory consumables;
    public ProgressionData progression;
}

[Serializable]
public class BackendProfileBootstrapRequest
{
    public string playerId;
    public string deviceProfileId;
    public string accountId;
    public string displayName;
    public int lastKnownProfileVersion;
    public BackendWalletState seedWallet;
    public BackendGachaState seedGacha;
    public BackendProfileSyncableData profile;
}

[Serializable]
public class BackendProfileSyncRequest
{
    public string playerId;
    public string deviceProfileId;
    public string accountId;
    public string displayName;
    public int lastKnownProfileVersion;
    public bool pushLocalChanges;
    public BackendProfileSyncableData profile;
}

[Serializable]
public class BackendProfileResponse
{
    public string playerId;
    public string deviceProfileId;
    public string remoteProfileId;
    public string accountId;
    public string displayName;
    public bool isGuest;
    public int profileVersion;
    public long serverUnixTime;
    public bool conflict;
    public BackendProfileSyncableData profile;
    public BackendWalletState wallet;
    public BackendGachaState gacha;
}

[Serializable]
public class BackendAccountAuthRequest
{
    public string playerId;
    public string email;
    public string password;
    public string displayName;
    public bool linkCurrentPlayer;
}

[Serializable]
public class BackendAccountLinkRequest
{
    public string playerId;
    public string email;
    public string password;
}

[Serializable]
public class BackendAccountInfo
{
    public string accountId;
    public string email;
    public string displayName;
    public string linkedPlayerId;
    public long createdAt;
    public long lastLoginAt;
}

[Serializable]
public class BackendAccountAuthResponse
{
    public bool ok;
    public BackendAccountInfo account;
    public string playerId;
    public string deviceProfileId;
    public string remoteProfileId;
    public string accountId;
    public string displayName;
    public bool isGuest;
    public int profileVersion;
    public long serverUnixTime;
    public bool conflict;
    public BackendProfileSyncableData profile;
    public BackendWalletState wallet;
    public BackendGachaState gacha;
}

[Serializable]
public class BackendWalletState
{
    public int Gold;
    public int Orbs;
    public int Ticket;
}

[Serializable]
public class BackendGachaState
{
    public int totalPullsLifetime;
    public int currentPityCounter;
    public int consecutivePullsNoEpic;
}

[Serializable]
public class BackendReward
{
    public string id;
    public string type;
    public int amount;
}

[Serializable]
public class BackendMissionEntryDto
{
    public string missionId;
    public string title;
    public string statKey;
    public int targetValue;
    public BackendReward reward;
    public int currentProgress;
    public bool isCompleted;
    public bool isClaimed;
}

[Serializable]
public class BackendMissionsStateResponse
{
    public string playerId;
    public int dayIndex;
    public int weekIndex;
    public bool dailyRerollUsed;
    public bool dailyBonusClaimed;
    public bool weeklyBonusClaimed;
    public BackendMissionEntryDto[] dailyMissions;
    public BackendMissionEntryDto[] weeklyMissions;
    public BackendWalletState wallet;
}

[Serializable]
public class BackendMissionProgressRequest
{
    public string playerId;
    public string statKey;
    public int amount;
}

[Serializable]
public class BackendMissionProgressResponse
{
    public bool ok;
}

[Serializable]
public class BackendMissionClaimRequest
{
    public string playerId;
    public string missionId;
}

[Serializable]
public class BackendMissionClaimResponse
{
    public bool ok;
}

[Serializable]
public class BackendMissionRerollRequest
{
    public string playerId;
    public string missionId;
}

[Serializable]
public class BackendMissionRerollResponse
{
    public bool ok;
}

[Serializable]
public class BackendGachaPullRequest
{
    public string playerId;
    public string bannerId;
    public int pullCount;
}

[Serializable]
public class BackendGachaPullResult
{
    public string rarity;
    public BackendReward reward;
}

[Serializable]
public class BackendGachaPullResponse
{
    public bool ok;
    public string bannerId;
    public int pullCount;
    public BackendWalletState wallet;
    public BackendGachaState gacha;
    public BackendGachaPullResult[] results;
}
