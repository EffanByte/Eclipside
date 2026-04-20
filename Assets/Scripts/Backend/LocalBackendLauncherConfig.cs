using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Backend/Local Backend Launcher Config")]
public class LocalBackendLauncherConfig : ScriptableObject
{
    [SerializeField] private bool autoLaunchInWindowsBuild = true;
    [SerializeField] private string nodeExecutablePath = "node";
    [SerializeField] private string bundledBackendFolderName = "BackendQuick";
    [SerializeField] private float startupTimeoutSeconds = 4f;
    [SerializeField] private string googleApplicationCredentialsPath;
    [SerializeField] private string firebaseProjectId;

    public bool AutoLaunchInWindowsBuild => autoLaunchInWindowsBuild;
    public string NodeExecutablePath => string.IsNullOrWhiteSpace(nodeExecutablePath) ? "node" : nodeExecutablePath.Trim();
    public string BundledBackendFolderName => string.IsNullOrWhiteSpace(bundledBackendFolderName) ? "BackendQuick" : bundledBackendFolderName.Trim();
    public float StartupTimeoutSeconds => Mathf.Clamp(startupTimeoutSeconds, 0.5f, 20f);
    public string GoogleApplicationCredentialsPath => googleApplicationCredentialsPath != null ? googleApplicationCredentialsPath.Trim() : string.Empty;
    public string FirebaseProjectId => firebaseProjectId != null ? firebaseProjectId.Trim() : string.Empty;

    public static LocalBackendLauncherConfig Load()
    {
        return Resources.Load<LocalBackendLauncherConfig>("LocalBackendLauncherConfig");
    }
}
