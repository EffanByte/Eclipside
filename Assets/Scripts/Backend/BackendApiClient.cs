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

    public static string GetOrCreatePlayerId()
    {
        var profile = SaveManager.Profile;
        if (string.IsNullOrWhiteSpace(profile.user_profile.user_id))
        {
            profile.user_profile.user_id = Guid.NewGuid().ToString("N");
            SaveManager.SaveProfile();
        }

        return profile.user_profile.user_id;
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

    public static IEnumerator InitializePlayer(Action<BackendInitResponse> onSuccess, Action<string> onError)
    {
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
            onError?.Invoke(message);
            return;
        }

        string responseText = request.downloadHandler.text;
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
