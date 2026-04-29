using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class LocalizedFontResolver
{
#if UNITY_EDITOR
    private const string JapaneseTmpFontAssetPath = "Assets/Languages/NotoSerifJP-VariableFont_wght SDF.asset";
#endif

    private static readonly string[] JapaneseOsFonts =
    {
        "Yu Gothic UI",
        "Yu Gothic",
        "Meiryo UI",
        "Meiryo",
        "MS Gothic",
        "Noto Sans CJK JP",
        "Arial Unicode MS"
    };

    private static readonly string[] RussianOsFonts =
    {
        "Segoe UI",
        "Arial",
        "Tahoma",
        "Noto Sans",
        "Arial Unicode MS"
    };

    private static readonly Dictionary<string, Font> LegacyFontCache = new Dictionary<string, Font>();
    private static readonly Dictionary<string, TMP_FontAsset> TmpFontCache = new Dictionary<string, TMP_FontAsset>();

    private const string JapaneseCoverageSample = "\u30AC\u30C1\u30E3\u30DF\u30C3\u30B7\u30E7\u30F3\u30C1\u30E3\u30EC\u30F3\u30B8\u8A2D\u5B9A\u5B9F\u7E3E\u30E9\u30F3\u958B\u59CB";
    private const string RussianCoverageSample = "\u0410\u0411\u0412\u0413\u0414\u0415\u0416\u0417\u0418\u0419\u041A\u041B\u041C\u041D\u041E\u041F\u0420\u0421\u0422\u0423\u0424\u0425\u0426\u0427\u0428\u0429\u042A\u042B\u042C\u042D\u042E\u042F";

    public static Font ResolveLegacyFont(Font preferredFont = null)
    {
        string languageCode = LocalizationManager.GetCurrentLanguageCode();
        if (!RequiresLocalizedFallback(languageCode))
        {
            return preferredFont != null ? preferredFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        Font resolved = GetOrCreateLegacyFont(languageCode);
        if (resolved != null)
        {
            return resolved;
        }

        return preferredFont != null ? preferredFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    public static TMP_FontAsset ResolveTmpFont(TMP_FontAsset preferredFont = null)
    {
        string languageCode = LocalizationManager.GetCurrentLanguageCode();
        if (RequiresLocalizedFallback(languageCode))
        {
            TMP_FontAsset localized = GetOrCreateTmpFont(languageCode);
            if (localized != null)
            {
                return localized;
            }
        }

        TMP_FontAsset resolved = preferredFont != null ? preferredFont : TMP_Settings.defaultFontAsset;
#if UNITY_EDITOR
        EnsureEditorLocaleFontFallbacks(resolved, TMP_Settings.defaultFontAsset);
#endif
        return resolved;
    }

    public static void ApplyTo(Text legacyText, Font preferredFont = null)
    {
        if (legacyText == null)
        {
            return;
        }

        legacyText.font = ResolveLegacyFont(preferredFont);
    }

    public static void ApplyTo(TMP_Text tmpText, TMP_FontAsset preferredFont = null)
    {
        if (tmpText == null)
        {
            return;
        }

        tmpText.font = ResolveTmpFont(preferredFont);
    }

    private static bool RequiresLocalizedFallback(string languageCode)
    {
        string normalizedCode = NormalizeLanguageCode(languageCode);
        return normalizedCode == LocalizationManager.JapaneseCode
            || normalizedCode == LocalizationManager.RussianCode;
    }

    private static Font GetOrCreateLegacyFont(string languageCode)
    {
        string normalizedCode = NormalizeLanguageCode(languageCode);
        if (LegacyFontCache.TryGetValue(normalizedCode, out Font cachedFont) && cachedFont != null)
        {
            if (HasCoverage(cachedFont, GetRequiredSampleForLanguage(normalizedCode)))
            {
                return cachedFont;
            }

            LegacyFontCache.Remove(normalizedCode);
        }

        Font font = CreateBestOsFont(normalizedCode);
        if (font == null)
        {
            return null;
        }

        font.name = $"Localized_{normalizedCode}_Legacy";
        LegacyFontCache[normalizedCode] = font;
        return font;
    }

    private static TMP_FontAsset GetOrCreateTmpFont(string languageCode)
    {
        string normalizedCode = NormalizeLanguageCode(languageCode);
        if (TmpFontCache.TryGetValue(normalizedCode, out TMP_FontAsset cachedFont) && cachedFont != null)
        {
            Font cachedSource = cachedFont.sourceFontFile;
            if (cachedSource != null && HasCoverage(cachedSource, GetRequiredSampleForLanguage(normalizedCode)))
            {
                return cachedFont;
            }

            TmpFontCache.Remove(normalizedCode);
        }

        Font sourceFont = CreateBestOsFont(normalizedCode);
        if (sourceFont == null)
        {
            return null;
        }

        TMP_FontAsset tmpFont;
        try
        {
            tmpFont = TMP_FontAsset.CreateFontAsset(sourceFont);
        }
        catch
        {
            return null;
        }

        if (tmpFont == null)
        {
            return null;
        }

        tmpFont.name = $"Localized_{normalizedCode}_TMP";
        tmpFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;

        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont != null && defaultFont != tmpFont && !tmpFont.fallbackFontAssetTable.Contains(defaultFont))
        {
            tmpFont.fallbackFontAssetTable.Add(defaultFont);
        }

        TmpFontCache[normalizedCode] = tmpFont;
        return tmpFont;
    }

    private static Font CreateBestOsFont(string languageCode)
    {
        string[] fontNames = GetOsFontsForLanguage(languageCode);
        if (fontNames == null || fontNames.Length == 0)
        {
            return null;
        }

        string requiredSample = GetRequiredSampleForLanguage(languageCode);
        for (int i = 0; i < fontNames.Length; i++)
        {
            string fontName = fontNames[i];
            if (string.IsNullOrWhiteSpace(fontName))
            {
                continue;
            }

            Font candidate = CreateOsFontSafe(fontName);
            if (candidate == null)
            {
                continue;
            }

            if (HasCoverage(candidate, requiredSample))
            {
                return candidate;
            }
        }

        try
        {
            return Font.CreateDynamicFontFromOSFont(fontNames, 32);
        }
        catch
        {
            return null;
        }
    }

    private static Font CreateOsFontSafe(string fontName)
    {
        try
        {
            return Font.CreateDynamicFontFromOSFont(fontName, 32);
        }
        catch
        {
            return null;
        }
    }

    private static bool HasCoverage(Font font, string sample)
    {
        if (font == null || string.IsNullOrEmpty(sample))
        {
            return true;
        }

        for (int i = 0; i < sample.Length; i++)
        {
            if (!font.HasCharacter(sample[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static string GetRequiredSampleForLanguage(string languageCode)
    {
        switch (NormalizeLanguageCode(languageCode))
        {
            case LocalizationManager.JapaneseCode:
                return JapaneseCoverageSample;
            case LocalizationManager.RussianCode:
                return RussianCoverageSample;
            default:
                return string.Empty;
        }
    }

    private static string[] GetOsFontsForLanguage(string languageCode)
    {
        switch (NormalizeLanguageCode(languageCode))
        {
            case LocalizationManager.JapaneseCode:
                return JapaneseOsFonts;
            case LocalizationManager.RussianCode:
                return RussianOsFonts;
            default:
                return null;
        }
    }

    private static string NormalizeLanguageCode(string languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return string.Empty;
        }

        string normalized = languageCode.Trim().ToLowerInvariant();
        int separatorIndex = normalized.IndexOf('-');
        if (separatorIndex < 0)
        {
            separatorIndex = normalized.IndexOf('_');
        }

        if (separatorIndex > 0)
        {
            normalized = normalized.Substring(0, separatorIndex);
        }

        return normalized;
    }

#if UNITY_EDITOR
    public static void EnsureEditorLocaleFontFallbacks(params TMP_FontAsset[] primaryFonts)
    {
        TMP_FontAsset japaneseFallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(JapaneseTmpFontAssetPath);
        if (japaneseFallback == null)
        {
            return;
        }

        for (int i = 0; i < primaryFonts.Length; i++)
        {
            TMP_FontAsset primary = primaryFonts[i];
            if (primary == null || primary == japaneseFallback)
            {
                continue;
            }

            if (primary.fallbackFontAssetTable.Contains(japaneseFallback))
            {
                continue;
            }

            primary.fallbackFontAssetTable.Add(japaneseFallback);
            EditorUtility.SetDirty(primary);
        }
    }
#endif
}
