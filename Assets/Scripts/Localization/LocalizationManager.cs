using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LocalizationManager : MonoBehaviour
{
    public const string DefaultTable = "UI";
    public const string EnglishCode = "en";
    public const string SpanishCode = "es";
    public const string JapaneseCode = "ja";
    public const string RussianCode = "ru";

    private static readonly string[] SupportedCodes =
    {
        EnglishCode,
        SpanishCode,
        JapaneseCode,
        RussianCode
    };

    private static LocalizationManager instance;
    private bool initialized;

    public static event Action LanguageChanged;

    public static bool IsReady => instance != null && instance.initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    public static void EnsureExists()
    {
        if (instance != null)
        {
            return;
        }

        GameObject root = new GameObject("LocalizationManager");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<LocalizationManager>();
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
    }

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;

        ApplySavedOrSystemLanguage();
        initialized = true;
        LanguageChanged?.Invoke();
    }

    public static string GetCurrentLanguageCode()
    {
        Locale locale = LocalizationSettings.SelectedLocale;
        return locale != null ? locale.Identifier.Code : GetSavedOrDefaultLanguageCode();
    }

    public static void SetLanguage(string code)
    {
        EnsureExists();
        if (instance != null)
        {
            instance.StartCoroutine(instance.SetLanguageRoutine(code));
        }
    }

    public static string GetString(string tableKey, string entryKey, string fallback, params object[] arguments)
    {
        if (string.IsNullOrWhiteSpace(entryKey))
        {
            return fallback ?? string.Empty;
        }

        if (!LocalizationSettings.HasSettings)
        {
            return fallback ?? entryKey;
        }

        try
        {
            LocalizedString localizedString = new LocalizedString(tableKey, entryKey);
            if (arguments != null && arguments.Length > 0)
            {
                localizedString.Arguments = arguments;
            }

            string value = localizedString.GetLocalizedString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback ?? entryKey;
            }

            return value;
        }
        catch
        {
            return fallback ?? entryKey;
        }
    }

    public static IReadOnlyList<string> GetSupportedLanguageCodes()
    {
        return SupportedCodes;
    }

    public static string GetSavedOrDefaultLanguageCode()
    {
        SaveFile_Settings settings = SaveManager.Settings;
        if (settings?.general != null && IsSupportedLanguageCode(settings.general.language))
        {
            return settings.general.language;
        }

        switch (Application.systemLanguage)
        {
            case SystemLanguage.Spanish:
                return SpanishCode;
            case SystemLanguage.Japanese:
                return JapaneseCode;
            case SystemLanguage.Russian:
                return RussianCode;
            default:
                return EnglishCode;
        }
    }

    public static bool IsSupportedLanguageCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        for (int i = 0; i < SupportedCodes.Length; i++)
        {
            if (string.Equals(SupportedCodes[i], code, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static string GetDisplayNameForCode(string code)
    {
        switch (code)
        {
            case SpanishCode:
                return "Espa\u00F1ol";
            case JapaneseCode:
                return "\u65E5\u672C\u8A9E";
            case RussianCode:
                return "\u0420\u0443\u0441\u0441\u043A\u0438\u0439";
            default:
                return "English";
        }
    }

    private void ApplySavedOrSystemLanguage()
    {
        string code = GetSavedOrDefaultLanguageCode();
        Locale locale = FindLocale(code) ?? FindLocale(EnglishCode);
        if (locale == null)
        {
            Debug.LogWarning("[Localization] No matching locale found. Localization will use Unity defaults.");
            return;
        }

        LocalizationSettings.SelectedLocale = locale;
        SaveLanguageCode(locale.Identifier.Code);
    }

    private IEnumerator SetLanguageRoutine(string code)
    {
        yield return LocalizationSettings.InitializationOperation;

        Locale locale = FindLocale(code);
        if (locale == null)
        {
            Debug.LogWarning($"[Localization] Locale '{code}' is not available.");
            yield break;
        }

        LocalizationSettings.SelectedLocale = locale;
        SaveLanguageCode(locale.Identifier.Code);
        initialized = true;
        LanguageChanged?.Invoke();
    }

    private static Locale FindLocale(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || !LocalizationSettings.HasSettings)
        {
            return null;
        }

        IList<Locale> locales = LocalizationSettings.AvailableLocales?.Locales;
        if (locales == null)
        {
            return null;
        }

        for (int i = 0; i < locales.Count; i++)
        {
            Locale locale = locales[i];
            if (locale != null && string.Equals(locale.Identifier.Code, code, StringComparison.OrdinalIgnoreCase))
            {
                return locale;
            }
        }

        return null;
    }

    private static void SaveLanguageCode(string code)
    {
        if (!IsSupportedLanguageCode(code))
        {
            code = EnglishCode;
        }

        SaveFile_Settings settings = SaveManager.Settings;
        settings.general.language = code;
        SaveManager.SaveSettings();
    }
}
