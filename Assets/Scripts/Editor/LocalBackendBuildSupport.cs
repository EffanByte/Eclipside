using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Eclipside.Editor
{
    public class LocalBackendBuildSupport : IPreprocessBuildWithReport
    {
        private const string SourceRoot = "Assets/BackendQuick";
        private const string DestinationRoot = "Assets/StreamingAssets/BackendQuick";

        public int callbackOrder => 0;

        [MenuItem("Tools/Eclipside/Backend/Sync Bundled Backend To StreamingAssets")]
        public static void SyncBundledBackend()
        {
            if (!Directory.Exists(SourceRoot))
            {
                Debug.LogWarning($"[LocalBackendBuild] Source backend folder not found at {SourceRoot}.");
                return;
            }

            if (Directory.Exists(DestinationRoot))
            {
                Directory.Delete(DestinationRoot, true);
            }

            Directory.CreateDirectory(DestinationRoot);
            CopyDirectory(SourceRoot, DestinationRoot);
            AssetDatabase.Refresh();
            Debug.Log($"[LocalBackendBuild] Synced backend from {SourceRoot} to {DestinationRoot}.");
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            SyncBundledBackend();
        }

        private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
        {
            Directory.CreateDirectory(destinationDirectory);

            string[] files = Directory.GetFiles(sourceDirectory);
            foreach (string file in files)
            {
                string fileName = Path.GetFileName(file);
                if (string.Equals(fileName, ".DS_Store"))
                {
                    continue;
                }

                if (fileName.EndsWith(".meta"))
                {
                    continue;
                }

                string targetPath = Path.Combine(destinationDirectory, fileName);
                File.Copy(file, targetPath, true);
            }

            string[] directories = Directory.GetDirectories(sourceDirectory);
            foreach (string directory in directories)
            {
                string folderName = Path.GetFileName(directory);
                if (string.Equals(folderName, "data"))
                {
                    continue;
                }

                string targetSubdirectory = Path.Combine(destinationDirectory, folderName);
                CopyDirectory(directory, targetSubdirectory);
            }
        }
    }
}
