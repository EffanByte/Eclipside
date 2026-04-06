using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class FirebaseAuthApiClient
{
    public static IEnumerator RegisterEmailPassword(string email, string password, string displayName, Action<FirebaseAuthSession> onSuccess, Action<string> onError)
    {
        FirebaseAuthConfig config = FirebaseAuthConfig.Load();
        if (!TryValidateConfig(config, onError))
        {
            yield break;
        }

        FirebaseEmailPasswordRequest request = new FirebaseEmailPasswordRequest
        {
            email = email?.Trim(),
            password = password ?? string.Empty,
            returnSecureToken = true
        };

        FirebaseAuthSession session = null;
        string error = null;
        yield return PostAuthJson(
            BuildAuthUrl(config, "accounts:signUp"),
            request,
            value => session = value,
            message => error = message);

        if (!string.IsNullOrWhiteSpace(error))
        {
            onError?.Invoke(error);
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            FirebaseAuthSession updatedSession = null;
            string updateError = null;
            yield return UpdateDisplayName(session.idToken, displayName, value => updatedSession = value, message => updateError = message);
            if (string.IsNullOrWhiteSpace(updateError) && updatedSession != null)
            {
                session = updatedSession;
            }
        }

        onSuccess?.Invoke(session);
    }

    public static IEnumerator LoginEmailPassword(string email, string password, Action<FirebaseAuthSession> onSuccess, Action<string> onError)
    {
        FirebaseAuthConfig config = FirebaseAuthConfig.Load();
        if (!TryValidateConfig(config, onError))
        {
            yield break;
        }

        FirebaseEmailPasswordRequest request = new FirebaseEmailPasswordRequest
        {
            email = email?.Trim(),
            password = password ?? string.Empty,
            returnSecureToken = true
        };

        yield return PostAuthJson(
            BuildAuthUrl(config, "accounts:signInWithPassword"),
            request,
            onSuccess,
            onError);
    }

    private static IEnumerator UpdateDisplayName(string idToken, string displayName, Action<FirebaseAuthSession> onSuccess, Action<string> onError)
    {
        FirebaseAuthConfig config = FirebaseAuthConfig.Load();
        if (!TryValidateConfig(config, onError))
        {
            yield break;
        }

        FirebaseUpdateProfileRequest request = new FirebaseUpdateProfileRequest
        {
            idToken = idToken,
            displayName = displayName,
            returnSecureToken = true
        };

        yield return PostAuthJson(
            BuildAuthUrl(config, "accounts:update"),
            request,
            onSuccess,
            onError);
    }

    private static string BuildAuthUrl(FirebaseAuthConfig config, string endpoint)
    {
        return $"{config.IdentityToolkitBaseUrl}/{endpoint}?key={UnityWebRequest.EscapeURL(config.WebApiKey)}";
    }

    private static bool TryValidateConfig(FirebaseAuthConfig config, Action<string> onError)
    {
        if (config == null)
        {
            onError?.Invoke("FirebaseAuthConfig asset not found. Create Resources/FirebaseAuthConfig and set the Web API key.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(config.WebApiKey))
        {
            onError?.Invoke("FirebaseAuthConfig is missing the Web API key.");
            return false;
        }

        return true;
    }

    private static IEnumerator PostAuthJson<TRequest>(string url, TRequest payload, Action<FirebaseAuthSession> onSuccess, Action<string> onError)
    {
        string json = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"[FirebaseAuth] POST {url}");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(ParseFirebaseError(request));
                yield break;
            }

            string responseText = request.downloadHandler.text;
            Debug.Log($"[FirebaseAuth] OK {url} {responseText}");

            FirebaseAuthSession response = JsonUtility.FromJson<FirebaseAuthSession>(responseText);
            if (response == null || string.IsNullOrWhiteSpace(response.localId))
            {
                onError?.Invoke("Failed to parse Firebase auth response.");
                yield break;
            }

            onSuccess?.Invoke(response);
        }
    }

    private static string ParseFirebaseError(UnityWebRequest request)
    {
        string body = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                FirebaseAuthErrorEnvelope parsed = JsonUtility.FromJson<FirebaseAuthErrorEnvelope>(body);
                if (parsed != null && parsed.error != null && !string.IsNullOrWhiteSpace(parsed.error.message))
                {
                    return parsed.error.message;
                }
            }
            catch
            {
            }
        }

        return !string.IsNullOrWhiteSpace(request.error) ? request.error : "Firebase auth request failed.";
    }
}

[Serializable]
public class FirebaseEmailPasswordRequest
{
    public string email;
    public string password;
    public bool returnSecureToken;
}

[Serializable]
public class FirebaseUpdateProfileRequest
{
    public string idToken;
    public string displayName;
    public bool returnSecureToken;
}

[Serializable]
public class FirebaseAuthSession
{
    public string kind;
    public string localId;
    public string email;
    public string displayName;
    public string idToken;
    public string refreshToken;
    public string expiresIn;
    public bool registered;

    public long GetExpiresInSeconds()
    {
        return long.TryParse(expiresIn, out long value) ? Math.Max(0, value) : 0;
    }
}

[Serializable]
public class FirebaseAuthErrorEnvelope
{
    public FirebaseAuthErrorBody error;
}

[Serializable]
public class FirebaseAuthErrorBody
{
    public int code;
    public string message;
}
