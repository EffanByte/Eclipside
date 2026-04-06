using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Eclipside.Editor
{
    [InitializeOnLoad]
    public static class UiLocalizationTableSeeder
    {
        private const string TableName = "UI";
        private const string AssetDirectory = "Assets/Languages";
        private const string SessionSeedKey = "Eclipside.UiLocalizationTableSeeder.Ran";

        private static readonly Dictionary<string, string> EnglishEntries = new Dictionary<string, string>()
        {
            { "menu.title", "ECLIPSIDE" },
            { "menu.subtitle", "Descend through fractured biomes, shape your loadout, and survive the eclipse." },
            { "menu.section.play", "Choose Your Path" },
            { "menu.footer", "A bold run begins with a deliberate choice." },
            { "menu.start_run", "Start Run" },
            { "menu.start_run.subtitle", "Select your character and enter the next run." },
            { "menu.gacha", "Gacha" },
            { "menu.gacha.subtitle", "Spend meteorites and expand your roster." },
            { "menu.missions", "Missions" },
            { "menu.missions.subtitle", "Track daily goals and collect rewards." },
            { "menu.achievements", "Achievements" },
            { "menu.achievements.subtitle", "Review long-term progress and milestones." },
            { "menu.challenges", "Challenges" },
            { "menu.challenges.subtitle", "Enable run modifiers and earn bragging rights." },
            { "menu.settings", "Settings" },
            { "menu.settings.subtitle", "Adjust language and menu preferences." },
            { "menu.character_select.title", "Select Character" },
            { "menu.character_select.none", "Selected: None" },
            { "menu.character_select.selected_format", "Selected: {0} ({1})" },
            { "common.back", "Back" },
            { "common.close", "Close" },
            { "common.status.unlocked", "Unlocked" },
            { "common.status.locked", "Locked" },
            { "settings.title", "Settings" },
            { "settings.subtitle", "Adjust how the game presents itself before you begin the next run." },
            { "settings.language", "Language" },
            { "settings.language.current_format", "Current language: {0}" },
            { "auth.onboarding.title", "Welcome to Eclipside" },
            { "auth.onboarding.subtitle", "Create an account to keep your progress synced, or continue as a guest and decide later." },
            { "auth.sign_in", "Sign In" },
            { "auth.sign_up", "Sign Up" },
            { "auth.display_name", "Display Name" },
            { "auth.email", "Email" },
            { "auth.password", "Password" },
            { "auth.create_account", "Create Account" },
            { "auth.login_action", "Log In" },
            { "auth.continue_guest", "Continue as Guest" },
            { "auth.status.signing_up", "Creating your account..." },
            { "auth.status.signing_in", "Signing you in..." },
            { "auth.status.idle_signup", "Choose a display name, then create your account." },
            { "auth.status.idle_signin", "Sign in to load your account-linked profile." },
            { "auth.error.missing_credentials", "Enter both email and password." },
            { "auth.error.manager_missing", "Authentication manager is unavailable." }
        };

        private static readonly Dictionary<string, string> SpanishEntries = new Dictionary<string, string>()
        {
            { "menu.title", "ECLIPSIDE" },
            { "menu.subtitle", "Desciende por biomas fracturados, define tu equipamiento y sobrevive al eclipse." },
            { "menu.section.play", "Elige Tu Camino" },
            { "menu.footer", "Una gran aventura empieza con una eleccion consciente." },
            { "menu.start_run", "Iniciar Partida" },
            { "menu.start_run.subtitle", "Elige tu personaje y entra en la siguiente partida." },
            { "menu.gacha", "Gacha" },
            { "menu.gacha.subtitle", "Gasta meteoritos y amplia tu plantel." },
            { "menu.missions", "Misiones" },
            { "menu.missions.subtitle", "Sigue objetivos diarios y reclama recompensas." },
            { "menu.achievements", "Logros" },
            { "menu.achievements.subtitle", "Revisa el progreso a largo plazo y tus hitos." },
            { "menu.challenges", "Desafios" },
            { "menu.challenges.subtitle", "Activa modificadores de partida y gana prestigio." },
            { "menu.settings", "Ajustes" },
            { "menu.settings.subtitle", "Ajusta el idioma y las preferencias del menu." },
            { "menu.character_select.title", "Seleccionar Personaje" },
            { "menu.character_select.none", "Seleccionado: Ninguno" },
            { "menu.character_select.selected_format", "Seleccionado: {0} ({1})" },
            { "common.back", "Atras" },
            { "common.close", "Cerrar" },
            { "common.status.unlocked", "Desbloqueado" },
            { "common.status.locked", "Bloqueado" },
            { "settings.title", "Ajustes" },
            { "settings.subtitle", "Define como se presenta el juego antes de empezar la siguiente partida." },
            { "settings.language", "Idioma" },
            { "settings.language.current_format", "Idioma actual: {0}" },
            { "auth.onboarding.title", "Bienvenido a Eclipside" },
            { "auth.onboarding.subtitle", "Crea una cuenta para mantener tu progreso sincronizado o continua como invitado y decide mas tarde." },
            { "auth.sign_in", "Iniciar Sesion" },
            { "auth.sign_up", "Registrarse" },
            { "auth.display_name", "Nombre Visible" },
            { "auth.email", "Correo Electronico" },
            { "auth.password", "Contrasena" },
            { "auth.create_account", "Crear Cuenta" },
            { "auth.login_action", "Entrar" },
            { "auth.continue_guest", "Continuar como Invitado" },
            { "auth.status.signing_up", "Creando tu cuenta..." },
            { "auth.status.signing_in", "Iniciando sesion..." },
            { "auth.status.idle_signup", "Elige un nombre visible y luego crea tu cuenta." },
            { "auth.status.idle_signin", "Inicia sesion para cargar tu perfil vinculado." },
            { "auth.error.missing_credentials", "Introduce el correo y la contrasena." },
            { "auth.error.manager_missing", "El gestor de autenticacion no esta disponible." }
        };

        private static readonly Dictionary<string, string> JapaneseEntries = new Dictionary<string, string>()
        {
            { "menu.title", "ECLIPSIDE" },
            { "menu.subtitle", "砕けたバイオームを進み、装備を整え、蝕を生き延びよう。" },
            { "menu.section.play", "進む道を選ぶ" },
            { "menu.footer", "大胆な冒険は、ひとつの選択から始まる。" },
            { "menu.start_run", "ラン開始" },
            { "menu.start_run.subtitle", "キャラクターを選んで次のランに挑もう。" },
            { "menu.gacha", "ガチャ" },
            { "menu.gacha.subtitle", "メテオライトを使って戦力を広げよう。" },
            { "menu.missions", "ミッション" },
            { "menu.missions.subtitle", "デイリー目標を進めて報酬を受け取ろう。" },
            { "menu.achievements", "実績" },
            { "menu.achievements.subtitle", "長期的な進行状況と達成項目を確認しよう。" },
            { "menu.challenges", "チャレンジ" },
            { "menu.challenges.subtitle", "ランに制約を加えて特別な達成感を得よう。" },
            { "menu.settings", "設定" },
            { "menu.settings.subtitle", "言語とメニュー表示を調整します。" },
            { "menu.character_select.title", "キャラクター選択" },
            { "menu.character_select.none", "選択中: なし" },
            { "menu.character_select.selected_format", "選択中: {0} ({1})" },
            { "common.back", "戻る" },
            { "common.close", "閉じる" },
            { "common.status.unlocked", "解放済み" },
            { "common.status.locked", "未解放" },
            { "settings.title", "設定" },
            { "settings.subtitle", "次のランを始める前にゲームの表示を調整します。" },
            { "settings.language", "言語" },
            { "settings.language.current_format", "現在の言語: {0}" },
            { "auth.onboarding.title", "Eclipsideへようこそ" },
            { "auth.onboarding.subtitle", "アカウントを作成すると進行状況を同期できます。後で決めたい場合はゲストとして続行できます。" },
            { "auth.sign_in", "ログイン" },
            { "auth.sign_up", "新規登録" },
            { "auth.display_name", "表示名" },
            { "auth.email", "メールアドレス" },
            { "auth.password", "パスワード" },
            { "auth.create_account", "アカウント作成" },
            { "auth.login_action", "ログインする" },
            { "auth.continue_guest", "ゲストで続行" },
            { "auth.status.signing_up", "アカウントを作成しています..." },
            { "auth.status.signing_in", "サインインしています..." },
            { "auth.status.idle_signup", "表示名を決めてからアカウントを作成してください。" },
            { "auth.status.idle_signin", "サインインしてアカウント連携プロフィールを読み込みます。" },
            { "auth.error.missing_credentials", "メールアドレスとパスワードを入力してください。" },
            { "auth.error.manager_missing", "認証マネージャーを利用できません。" }
        };

        private static readonly Dictionary<string, string> RussianEntries = new Dictionary<string, string>()
        {
            { "menu.title", "ECLIPSIDE" },
            { "menu.subtitle", "Погружайтесь в расколотые биомы, собирайте билд и переживите затмение." },
            { "menu.section.play", "Выберите Свой Путь" },
            { "menu.footer", "Сильный забег начинается с осознанного выбора." },
            { "menu.start_run", "Начать Забег" },
            { "menu.start_run.subtitle", "Выберите персонажа и отправляйтесь в следующий забег." },
            { "menu.gacha", "Гача" },
            { "menu.gacha.subtitle", "Тратьте метеориты и расширяйте свой состав." },
            { "menu.missions", "Задания" },
            { "menu.missions.subtitle", "Следите за ежедневными целями и получайте награды." },
            { "menu.achievements", "Достижения" },
            { "menu.achievements.subtitle", "Просматривайте долгосрочный прогресс и важные вехи." },
            { "menu.challenges", "Испытания" },
            { "menu.challenges.subtitle", "Включайте модификаторы забега и зарабатывайте славу." },
            { "menu.settings", "Настройки" },
            { "menu.settings.subtitle", "Измените язык и параметры меню." },
            { "menu.character_select.title", "Выбор Персонажа" },
            { "menu.character_select.none", "Выбрано: Нет" },
            { "menu.character_select.selected_format", "Выбрано: {0} ({1})" },
            { "common.back", "Назад" },
            { "common.close", "Закрыть" },
            { "common.status.unlocked", "Открыто" },
            { "common.status.locked", "Закрыто" },
            { "settings.title", "Настройки" },
            { "settings.subtitle", "Настройте отображение игры перед следующим забегом." },
            { "settings.language", "Язык" },
            { "settings.language.current_format", "Текущий язык: {0}" },
            { "auth.onboarding.title", "Добро пожаловать в Eclipside" },
            { "auth.onboarding.subtitle", "Создайте аккаунт, чтобы синхронизировать прогресс, или продолжайте как гость и решите позже." },
            { "auth.sign_in", "Войти" },
            { "auth.sign_up", "Регистрация" },
            { "auth.display_name", "Отображаемое Имя" },
            { "auth.email", "Эл. почта" },
            { "auth.password", "Пароль" },
            { "auth.create_account", "Создать Аккаунт" },
            { "auth.login_action", "Войти" },
            { "auth.continue_guest", "Продолжить как Гость" },
            { "auth.status.signing_up", "Создаем ваш аккаунт..." },
            { "auth.status.signing_in", "Выполняем вход..." },
            { "auth.status.idle_signup", "Выберите отображаемое имя, затем создайте аккаунт." },
            { "auth.status.idle_signin", "Войдите, чтобы загрузить профиль, связанный с аккаунтом." },
            { "auth.error.missing_credentials", "Введите адрес почты и пароль." },
            { "auth.error.manager_missing", "Менеджер аутентификации недоступен." }
        };

        static UiLocalizationTableSeeder()
        {
            EditorApplication.delayCall += SeedMissingUiEntriesOnLoad;
        }

        [MenuItem("Tools/Eclipside/Localization/Seed UI Table")]
        public static void SeedUiTableFromMenu()
        {
            SeedUiTable(logSummary: true);
        }

        private static void SeedMissingUiEntriesOnLoad()
        {
            if (SessionState.GetBool(SessionSeedKey, false))
                return;

            SessionState.SetBool(SessionSeedKey, true);
            SeedUiTable(logSummary: false);
        }

        private static void SeedUiTable(bool logSummary)
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(TableName);
            if (collection == null)
            {
                collection = LocalizationEditorSettings.CreateStringTableCollection(TableName, AssetDirectory);
                if (collection == null)
                {
                    Debug.LogError($"[Localization] Failed to create the {TableName} string table collection.");
                    return;
                }
            }

            int addedEntries = 0;
            int filledValues = 0;
            int localizedReplacements = 0;

            foreach (Locale locale in LocalizationEditorSettings.GetLocales())
            {
                StringTable table = collection.GetTable(locale.Identifier) as StringTable;
                if (table == null)
                {
                    string tablePath = $"{AssetDirectory}/{TableName}_{locale.Identifier.Code}.asset";
                    table = collection.AddNewTable(locale.Identifier, tablePath) as StringTable;
                }

                if (table == null)
                    continue;

                Dictionary<string, string> localeEntries = GetEntriesForLocale(locale.Identifier.Code);

                foreach (KeyValuePair<string, string> entryPair in EnglishEntries)
                {
                    StringTableEntry entry = table.GetEntry(entryPair.Key);
                    string localizedValue = localeEntries.ContainsKey(entryPair.Key) ? localeEntries[entryPair.Key] : entryPair.Value;
                    if (entry == null)
                    {
                        entry = table.AddEntry(entryPair.Key, localizedValue);
                        addedEntries++;
                    }
                    else if (string.IsNullOrWhiteSpace(entry.Value))
                    {
                        entry.Value = localizedValue;
                        filledValues++;
                    }
                    else if (locale.Identifier.Code != "en" && entry.Value == entryPair.Value && localizedValue != entryPair.Value)
                    {
                        entry.Value = localizedValue;
                        localizedReplacements++;
                    }
                }

                EditorUtility.SetDirty(table);
            }

            EditorUtility.SetDirty(collection);
            EditorUtility.SetDirty(collection.SharedData);
            AssetDatabase.SaveAssets();

            if (logSummary)
            {
                Debug.Log($"[Localization] Seeded {TableName} table. Added {addedEntries} entries, filled {filledValues} empty values, and updated {localizedReplacements} localized placeholders.");
            }
        }

        private static Dictionary<string, string> GetEntriesForLocale(string localeCode)
        {
            switch (localeCode)
            {
                case "es":
                    return SpanishEntries;
                case "ja":
                    return JapaneseEntries;
                case "ru":
                    return RussianEntries;
                default:
                    return EnglishEntries;
            }
        }
    }
}
