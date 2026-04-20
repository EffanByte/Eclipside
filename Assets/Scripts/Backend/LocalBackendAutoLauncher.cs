using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class LocalBackendAutoLauncher : MonoBehaviour
{
    private static LocalBackendAutoLauncher instance;

    private Process launchedProcess;
    private bool launchedByThisProcess;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        #if UNITY_EDITOR
        return;
        #endif

        if (!BackendRuntimeSettings.IsEnabled)
        {
            return;
        }

        if (Application.platform != RuntimePlatform.WindowsPlayer)
        {
            return;
        }

        if (instance != null)
        {
            return;
        }

        GameObject root = new GameObject("LocalBackendAutoLauncher");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<LocalBackendAutoLauncher>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        TryLaunchBundledBackend();
    }

    private void OnApplicationQuit()
    {
        if (!launchedByThisProcess || launchedProcess == null)
        {
            return;
        }

        try
        {
            if (!launchedProcess.HasExited)
            {
                launchedProcess.Kill();
            }
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogWarning($"[LocalBackendLauncher] Failed to stop local backend process cleanly. {exception.Message}");
        }
    }

    private void TryLaunchBundledBackend()
    {
        if (!TryGetLocalBackendEndpoint(out string host, out int port))
        {
            UnityEngine.Debug.Log("[LocalBackendLauncher] Backend base URL is not local. Skipping local backend launch.");
            return;
        }

        if (IsPortOpen(host, port))
        {
            UnityEngine.Debug.Log($"[LocalBackendLauncher] Backend already reachable at {host}:{port}.");
            return;
        }

        LocalBackendLauncherConfig config = LocalBackendLauncherConfig.Load();
        if (config != null && !config.AutoLaunchInWindowsBuild)
        {
            UnityEngine.Debug.Log("[LocalBackendLauncher] Auto-launch is disabled in LocalBackendLauncherConfig.");
            return;
        }

        string backendFolderName = config != null ? config.BundledBackendFolderName : "BackendQuick";
        string serverPath = ResolveServerPath(backendFolderName);
        if (string.IsNullOrWhiteSpace(serverPath))
        {
            UnityEngine.Debug.LogWarning("[LocalBackendLauncher] Could not find a bundled backend server.js file.");
            return;
        }

        string nodeExecutable = config != null ? config.NodeExecutablePath : "node";
        string workingDirectory = Path.GetDirectoryName(serverPath);
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            UnityEngine.Debug.LogWarning("[LocalBackendLauncher] Could not resolve backend working directory.");
            return;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = nodeExecutable,
            Arguments = $"\"{serverPath}\"",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        if (config != null)
        {
            if (!string.IsNullOrWhiteSpace(config.GoogleApplicationCredentialsPath))
            {
                startInfo.Environment["GOOGLE_APPLICATION_CREDENTIALS"] = config.GoogleApplicationCredentialsPath;
            }

            if (!string.IsNullOrWhiteSpace(config.FirebaseProjectId))
            {
                startInfo.Environment["FIREBASE_PROJECT_ID"] = config.FirebaseProjectId;
            }
        }

        try
        {
            launchedProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            launchedProcess.OutputDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    UnityEngine.Debug.Log($"[LocalBackendLauncher][stdout] {args.Data}");
                }
            };
            launchedProcess.ErrorDataReceived += (_, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    UnityEngine.Debug.LogWarning($"[LocalBackendLauncher][stderr] {args.Data}");
                }
            };

            launchedProcess.Start();
            launchedProcess.BeginOutputReadLine();
            launchedProcess.BeginErrorReadLine();
            launchedByThisProcess = true;

            float timeoutSeconds = config != null ? config.StartupTimeoutSeconds : 4f;
            bool ready = WaitForPort(host, port, timeoutSeconds);
            UnityEngine.Debug.Log(ready
                ? $"[LocalBackendLauncher] Started bundled backend at {host}:{port}."
                : $"[LocalBackendLauncher] Backend launch attempted, but {host}:{port} did not become ready within {timeoutSeconds:0.0}s.");
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogWarning($"[LocalBackendLauncher] Failed to start bundled backend using '{nodeExecutable}'. {exception.Message}");
        }
    }

    private static bool TryGetLocalBackendEndpoint(out string host, out int port)
    {
        host = "localhost";
        port = 8080;

        if (!Uri.TryCreate(BackendApiClient.BaseUrl, UriKind.Absolute, out Uri uri))
        {
            return false;
        }

        if (!string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        host = uri.Host;
        port = uri.IsDefaultPort ? 80 : uri.Port;
        return true;
    }

    private static string ResolveServerPath(string backendFolderName)
    {
        string[] candidates =
        {
            Path.Combine(Application.streamingAssetsPath, backendFolderName, "server.js"),
            Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? string.Empty, backendFolderName, "server.js")
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            if (File.Exists(candidates[i]))
            {
                return candidates[i];
            }
        }

        return string.Empty;
    }

    private static bool WaitForPort(string host, int port, float timeoutSeconds)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed.TotalSeconds < timeoutSeconds)
        {
            if (IsPortOpen(host, port))
            {
                return true;
            }

            Thread.Sleep(100);
        }

        return false;
    }

    private static bool IsPortOpen(string host, int port)
    {
        try
        {
            using (TcpClient client = new TcpClient())
            {
                IAsyncResult result = client.BeginConnect(host, port, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(150));
                if (!success)
                {
                    return false;
                }

                client.EndConnect(result);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }
}
