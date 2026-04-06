using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Backend/Firebase Auth Config")]
public class FirebaseAuthConfig : ScriptableObject
{
    [SerializeField] private string webApiKey;
    [SerializeField] private string identityToolkitBaseUrl = "https://identitytoolkit.googleapis.com/v1";

    public string WebApiKey => webApiKey != null ? webApiKey.Trim() : string.Empty;
    public string IdentityToolkitBaseUrl => string.IsNullOrWhiteSpace(identityToolkitBaseUrl)
        ? "https://identitytoolkit.googleapis.com/v1"
        : identityToolkitBaseUrl.TrimEnd('/');

    public static FirebaseAuthConfig Load()
    {
        return Resources.Load<FirebaseAuthConfig>("FirebaseAuthConfig");
    }
}
