using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Eclipside.Editor
{
    public static class LegacyTextToTmpConverter
    {
        private const string DefaultFontAssetPath = "Assets/Fonts/PixelifySans.asset";

        [MenuItem("Tools/Eclipside/UI/Convert Legacy Text To TMP In Open Scenes")]
        public static void ConvertOpenScenesMenu()
        {
            int convertedCount = ConvertOpenScenes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Converted {convertedCount} legacy UI Text component(s) to TMP in open scenes.");
        }

        [MenuItem("Tools/Eclipside/UI/Convert Legacy Text To TMP In Prefabs")]
        public static void ConvertPrefabsMenu()
        {
            int convertedCount = ConvertAllPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Converted {convertedCount} legacy UI Text component(s) to TMP in prefabs.");
        }

        [MenuItem("Tools/Eclipside/UI/Convert Legacy Text To TMP In Project")]
        public static void ConvertProjectMenu()
        {
            SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                int prefabConversions = ConvertAllPrefabs();
                int sceneConversions = ConvertAllScenesInProject();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"Converted {prefabConversions + sceneConversions} legacy UI Text component(s) to TMP across prefabs and scenes.");
            }
            finally
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        private static int ConvertOpenScenes()
        {
            int convertedCount = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                int sceneCount = ConvertHierarchy(scene.GetRootGameObjects());
                if (sceneCount > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }

                convertedCount += sceneCount;
            }

            return convertedCount;
        }

        private static int ConvertAllScenesInProject()
        {
            int convertedCount = 0;
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            for (int i = 0; i < sceneGuids.Length; i++)
            {
                string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                int sceneCount = ConvertHierarchy(scene.GetRootGameObjects());
                if (sceneCount > 0)
                {
                    EditorSceneManager.SaveScene(scene);
                }

                convertedCount += sceneCount;
            }

            return convertedCount;
        }

        private static int ConvertAllPrefabs()
        {
            int convertedCount = 0;
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
                int prefabCount = ConvertHierarchy(prefabRoot);
                if (prefabCount > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
                }

                PrefabUtility.UnloadPrefabContents(prefabRoot);
                convertedCount += prefabCount;
            }

            return convertedCount;
        }

        private static int ConvertHierarchy(params GameObject[] roots)
        {
            int convertedCount = 0;
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                {
                    continue;
                }

                Text[] legacyTexts = root.GetComponentsInChildren<Text>(true);
                for (int textIndex = 0; textIndex < legacyTexts.Length; textIndex++)
                {
                    if (ConvertTextComponent(legacyTexts[textIndex]))
                    {
                        convertedCount++;
                    }
                }
            }

            return convertedCount;
        }

        private static bool ConvertTextComponent(Text legacyText)
        {
            if (legacyText == null)
            {
                return false;
            }

            GameObject target = legacyText.gameObject;
            TextMeshProUGUI tmpText = target.GetComponent<TextMeshProUGUI>();
            if (tmpText == null)
            {
                tmpText = target.AddComponent<TextMeshProUGUI>();
            }

            CopyLegacyTextSettings(legacyText, tmpText);
            SyncLocalizedTextLabel(target, tmpText);

            Object.DestroyImmediate(legacyText);
            EditorUtility.SetDirty(target);
            return true;
        }

        private static void SyncLocalizedTextLabel(GameObject target, TMP_Text tmpText)
        {
            LocalizedTextLabel localizedLabel = target.GetComponent<LocalizedTextLabel>();
            if (localizedLabel == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(localizedLabel);
            serializedObject.FindProperty("tmpText").objectReferenceValue = tmpText;
            serializedObject.FindProperty("legacyText").objectReferenceValue = null;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(localizedLabel);
        }

        private static void CopyLegacyTextSettings(Text legacyText, TextMeshProUGUI tmpText)
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultFontAssetPath);
            if (fontAsset == null)
            {
                fontAsset = TMP_Settings.defaultFontAsset;
            }

            tmpText.font = LocalizedFontResolver.ResolveTmpFont(fontAsset);
            tmpText.text = legacyText.text;
            tmpText.fontSize = legacyText.fontSize;
            tmpText.fontStyle = ConvertFontStyle(legacyText.fontStyle);
            tmpText.alignment = ConvertAlignment(legacyText.alignment);
            tmpText.color = legacyText.color;
            tmpText.raycastTarget = legacyText.raycastTarget;
            tmpText.richText = legacyText.supportRichText;
            tmpText.lineSpacing = legacyText.lineSpacing;
            tmpText.enableWordWrapping = legacyText.horizontalOverflow != HorizontalWrapMode.Overflow;
            tmpText.enableAutoSizing = legacyText.resizeTextForBestFit;
            tmpText.fontSizeMin = legacyText.resizeTextMinSize;
            tmpText.fontSizeMax = legacyText.resizeTextMaxSize > 0 ? legacyText.resizeTextMaxSize : legacyText.fontSize;
            tmpText.overflowMode = legacyText.horizontalOverflow == HorizontalWrapMode.Overflow
                ? TextOverflowModes.Overflow
                : TextOverflowModes.Truncate;
        }

        private static FontStyles ConvertFontStyle(FontStyle fontStyle)
        {
            switch (fontStyle)
            {
                case FontStyle.Bold:
                    return FontStyles.Bold;
                case FontStyle.Italic:
                    return FontStyles.Italic;
                case FontStyle.BoldAndItalic:
                    return FontStyles.Bold | FontStyles.Italic;
                default:
                    return FontStyles.Normal;
            }
        }

        private static TextAlignmentOptions ConvertAlignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperLeft:
                    return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter:
                    return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight:
                    return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft:
                    return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter:
                    return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight:
                    return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft:
                    return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter:
                    return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight:
                    return TextAlignmentOptions.BottomRight;
                default:
                    return TextAlignmentOptions.Center;
            }
        }
    }
}
