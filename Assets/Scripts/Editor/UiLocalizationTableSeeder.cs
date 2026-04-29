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
            { "menu.status_equipped", "Equipped: {0}" },
            { "menu.status", "Equipped: {0}    Gold: {1}    Orbs: {2}" },
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
            { "pause.title", "Paused" },
            { "pause.subtitle", "Take a breath, adjust your plan, and jump back in when you're ready." },
            { "pause.controls.title", "Controls" },
            { "pause.controls.subtitle", "Quick reference for the current build while you're paused." },
            { "pause.resume", "Resume" },
            { "pause.retry", "Retry Run" },
            { "pause.main_menu", "Return to Main Menu" },
            { "pause.quit", "Quit Game" },
            { "levelup.title", "Level Up" },
            { "levelup.subtitle", "The eclipse bends for no one. Choose one upgrade before the next wave crashes in." },
            { "levelup.prompt", "Choose One Blessing" },
            { "levelup.base_damage.title", "Melee Damage" },
            { "levelup.base_damage.desc", "+10% to your melee weapon damage." },
            { "levelup.magic_damage.title", "Magic Damage" },
            { "levelup.magic_damage.desc", "+10% to your magic weapon damage." },
            { "levelup.heavy_damage.title", "Heavy Damage" },
            { "levelup.heavy_damage.desc", "+10% to your heavy weapon damage." },
            { "levelup.max_health.title", "Max Health" },
            { "levelup.max_health.desc", "+1 maximum heart and a matching heal." },
            { "levelup.speed.title", "Move Speed" },
            { "levelup.speed.desc", "+5% movement speed for the rest of the run." },
            { "levelup.attack_speed.title", "Attack Speed" },
            { "levelup.attack_speed.desc", "+5% attack speed for all weapons." },
            { "controls.move", "Move" },
            { "controls.attack", "Attack" },
            { "controls.dash", "Dash" },
            { "controls.special", "Special" },
            { "controls.interact", "Interact" },
            { "controls.items", "Items" },
            { "controls.pause", "Pause" },
            { "pickup.item", "Item Acquired" },
            { "pickup.weapon", "Weapon Acquired" },
            { "pickup.no_description", "No description is available for this item yet." },
            { "portal.enter", "Enter Portal" },
            { "portal.boss_active", "Boss Active" },
            { "portal.challenge_boss", "Challenge Boss" },
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
            { "menu.status_equipped", "Equipado: {0}" },
            { "menu.status", "Equipado: {0}    Oro: {1}    Orbes: {2}" },
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
            { "pause.title", "Pausa" },
            { "pause.subtitle", "Respira, ajusta tu plan y vuelve cuando estes listo." },
            { "pause.controls.title", "Controles" },
            { "pause.controls.subtitle", "Referencia rapida para la compilacion actual mientras el juego esta en pausa." },
            { "pause.resume", "Continuar" },
            { "pause.retry", "Reintentar Partida" },
            { "pause.main_menu", "Volver al Menu Principal" },
            { "pause.quit", "Salir del Juego" },
            { "levelup.title", "Subir de Nivel" },
            { "levelup.subtitle", "El eclipse no espera a nadie. Elige una mejora antes de que llegue la siguiente oleada." },
            { "levelup.prompt", "Elige Una Bendicion" },
            { "levelup.base_damage.title", "Dano Cuerpo a Cuerpo" },
            { "levelup.base_damage.desc", "+10% al dano de tus armas cuerpo a cuerpo." },
            { "levelup.magic_damage.title", "Dano Magico" },
            { "levelup.magic_damage.desc", "+10% al dano de tus armas magicas." },
            { "levelup.heavy_damage.title", "Dano Pesado" },
            { "levelup.heavy_damage.desc", "+10% al dano de tus armas pesadas." },
            { "levelup.max_health.title", "Vida Maxima" },
            { "levelup.max_health.desc", "+1 corazon maximo y una curacion equivalente." },
            { "levelup.speed.title", "Velocidad" },
            { "levelup.speed.desc", "+5% de velocidad de movimiento durante el resto de la partida." },
            { "levelup.attack_speed.title", "Velocidad de Ataque" },
            { "levelup.attack_speed.desc", "+5% de velocidad de ataque para todas tus armas." },
            { "controls.move", "Moverse" },
            { "controls.attack", "Atacar" },
            { "controls.dash", "Esquivar" },
            { "controls.special", "Especial" },
            { "controls.interact", "Interactuar" },
            { "controls.items", "Objetos" },
            { "controls.pause", "Pausa" },
            { "pickup.item", "Objeto Obtenido" },
            { "pickup.weapon", "Arma Obtenida" },
            { "pickup.no_description", "Todavia no hay una descripcion disponible para este objeto." },
            { "portal.enter", "Entrar al Portal" },
            { "portal.boss_active", "Jefe Activo" },
            { "portal.challenge_boss", "Desafiar al Jefe" },
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

        private static readonly Dictionary<string, string> SupplementalEnglishEntries = new Dictionary<string, string>()
        {
            { "settings.display", "Display" },
            { "settings.controls", "Controls" },
            { "settings.display.current_format", "Display Mode: {0}" },
            { "settings.display.windowed", "Windowed" },
            { "settings.display.fullscreen", "Fullscreen" },
            { "settings.controls.rebind_hint", "Select a control and press a new key." },
            { "settings.controls.rebinding", "Listening... press a key (Esc to cancel)." },
            { "settings.controls.listening", "Listening..." },
            { "settings.controls.saved", "Control updated." },
            { "settings.controls.unbound", "Unbound" },
            { "controls.move_up", "Move Up" },
            { "controls.move_down", "Move Down" },
            { "controls.move_left", "Move Left" },
            { "controls.move_right", "Move Right" },
            { "controls.item1", "Item Slot 1" },
            { "controls.item2", "Item Slot 2" },
            { "controls.item3", "Item Slot 3" },
            { "achievements.empty", "No achievements configured." },
            { "achievements.state.complete", "Complete" },
            { "missions.daily", "Daily Missions" },
            { "missions.weekly", "Weekly Missions" },
            { "missions.empty", "No missions available." },
            { "missions.state.claimed", "Claimed" },
            { "missions.state.ready_to_claim", "Ready to claim" },
            { "missions.state.in_progress", "In progress" },
            { "menu.challenges.instructions", "Choose modifiers before starting a run." },
            { "menu.challenges.state.active", "Active" },
            { "menu.challenges.state.inactive", "Inactive" },
            { "menu.challenges.action.toggle", "Click to toggle" },
            { "menu.challenges.type.fragile_crystal", "Fragile Crystal" },
            { "menu.challenges.type.the_purge", "The Purge" },
            { "menu.challenges.type.endless_greed", "Endless Greed" },
            { "menu.challenges.type.the_gladiator", "The Gladiator" },
            { "menu.challenges.type.blood_for_power", "Blood for Power" },
            { "menu.challenges.type.crossfire", "Crossfire" },
            { "menu.challenges.type.total_confusion", "Total Confusion" },
            { "menu.challenges.type.rain_of_fire", "Rain of Fire" },
            { "menu.challenges.type.the_unlucky", "The Unlucky" },
            { "menu.challenges.type.last_breath", "Last Breath" },
            { "menu.gacha.panel_subtitle", "Browse meteorite banners, inspect the pool, and pull from the main menu." },
            { "menu.gacha.meteor_rewards", "Meteor Rewards" },
            { "menu.gacha.rewards", "Rewards" },
            { "menu.gacha.empty", "No meteor banners configured." },
            { "menu.gacha.empty_result", "Add meteorite banners to the game database to enable pulls." },
            { "menu.gacha.result_default", "Choose a meteor, inspect its rewards, and pull when you are ready." },
            { "menu.gacha.single_pull", "1 Pull" },
            { "menu.gacha.ten_pull", "10 Pull" },
            { "menu.gacha.unavailable", "Unavailable" },
            { "menu.gacha.no_rewards", "No rewards configured." },
            { "menu.gacha.reward_character", "Character" },
            { "menu.gacha.reward_weapon", "Weapon" },
            { "menu.gacha.reward_consumable", "Consumable" },
            { "menu.gacha.reward_currency", "Currency" },
            { "menu.gacha.reward_unknown", "Unknown reward" },
            { "menu.gacha.rarity.common", "Common" },
            { "menu.gacha.rarity.rare", "Rare" },
            { "menu.gacha.rarity.epic", "Epic" },
            { "menu.gacha.rarity.mythic", "Mythic" },
            { "common.rarity.default", "Default" },
            { "common.rarity.epic", "Epic" },
            { "common.rarity.mythic", "Mythic" },
            { "currency.gold", "Gold" },
            { "currency.orb", "Orbs" },
            { "currency.ticket", "Tickets" },
            { "currency.key", "Keys" },
            { "currency.xp", "XP" },
            { "currency.rupee", "Rupees" }
        };

        private static readonly Dictionary<string, string> SupplementalSpanishEntries = new Dictionary<string, string>()
        {
            { "settings.display", "Pantalla" },
            { "settings.controls", "Controles" },
            { "settings.display.current_format", "Modo de pantalla: {0}" },
            { "settings.display.windowed", "Ventana" },
            { "settings.display.fullscreen", "Pantalla completa" },
            { "settings.controls.rebind_hint", "Selecciona un control y pulsa una tecla nueva." },
            { "settings.controls.rebinding", "Esperando entrada... pulsa una tecla (Esc para cancelar)." },
            { "settings.controls.listening", "Esperando entrada..." },
            { "settings.controls.saved", "Control actualizado." },
            { "settings.controls.unbound", "Sin asignar" },
            { "controls.move_up", "Mover arriba" },
            { "controls.move_down", "Mover abajo" },
            { "controls.move_left", "Mover izquierda" },
            { "controls.move_right", "Mover derecha" },
            { "controls.item1", "Ranura 1" },
            { "controls.item2", "Ranura 2" },
            { "controls.item3", "Ranura 3" },
            { "achievements.empty", "No hay logros configurados." },
            { "achievements.state.complete", "Completado" },
            { "missions.daily", "Misiones Diarias" },
            { "missions.weekly", "Misiones Semanales" },
            { "missions.empty", "No hay misiones disponibles." },
            { "missions.state.claimed", "Reclamada" },
            { "missions.state.ready_to_claim", "Lista para reclamar" },
            { "missions.state.in_progress", "En progreso" },
            { "menu.challenges.instructions", "Elige modificadores antes de empezar una partida." },
            { "menu.challenges.state.active", "Activo" },
            { "menu.challenges.state.inactive", "Inactivo" },
            { "menu.challenges.action.toggle", "Haz clic para cambiar" },
            { "menu.challenges.type.fragile_crystal", "Cristal Fragil" },
            { "menu.challenges.type.the_purge", "La Purga" },
            { "menu.challenges.type.endless_greed", "Codicia Infinita" },
            { "menu.challenges.type.the_gladiator", "El Gladiador" },
            { "menu.challenges.type.blood_for_power", "Sangre por Poder" },
            { "menu.challenges.type.crossfire", "Fuego Cruzado" },
            { "menu.challenges.type.total_confusion", "Confusion Total" },
            { "menu.challenges.type.rain_of_fire", "Lluvia de Fuego" },
            { "menu.challenges.type.the_unlucky", "El Desafortunado" },
            { "menu.challenges.type.last_breath", "Ultimo Aliento" },
            { "menu.gacha.panel_subtitle", "Explora meteoritos, revisa las recompensas y tira desde el menu principal." },
            { "menu.gacha.meteor_rewards", "Recompensas del Meteorito" },
            { "menu.gacha.rewards", "Recompensas" },
            { "menu.gacha.empty", "No hay meteoritos configurados." },
            { "menu.gacha.empty_result", "Agrega meteoritos a la base de datos del juego para habilitar tiradas." },
            { "menu.gacha.result_default", "Elige un meteorito, revisa sus recompensas y tira cuando estes listo." },
            { "menu.gacha.single_pull", "1 Tirada" },
            { "menu.gacha.ten_pull", "10 Tiradas" },
            { "menu.gacha.unavailable", "No disponible" },
            { "menu.gacha.no_rewards", "No hay recompensas configuradas." },
            { "menu.gacha.reward_character", "Personaje" },
            { "menu.gacha.reward_weapon", "Arma" },
            { "menu.gacha.reward_consumable", "Consumible" },
            { "menu.gacha.reward_currency", "Moneda" },
            { "menu.gacha.reward_unknown", "Recompensa desconocida" },
            { "menu.gacha.rarity.common", "Comun" },
            { "menu.gacha.rarity.rare", "Raro" },
            { "menu.gacha.rarity.epic", "Epico" },
            { "menu.gacha.rarity.mythic", "Mitico" },
            { "common.rarity.default", "Base" },
            { "common.rarity.epic", "Epico" },
            { "common.rarity.mythic", "Mitico" },
            { "currency.gold", "Oro" },
            { "currency.orb", "Orbes" },
            { "currency.ticket", "Boletos" },
            { "currency.key", "Llaves" },
            { "currency.xp", "XP" },
            { "currency.rupee", "Rupias" }
        };

        private static readonly Dictionary<string, string> SupplementalJapaneseEntries = new Dictionary<string, string>()
        {
            { "settings.display", "\u8868\u793A" },
            { "settings.controls", "\u64CD\u4F5C" },
            { "settings.display.current_format", "\u8868\u793A\u30E2\u30FC\u30C9: {0}" },
            { "settings.display.windowed", "\u30A6\u30A3\u30F3\u30C9\u30A6" },
            { "settings.display.fullscreen", "\u30D5\u30EB\u30B9\u30AF\u30EA\u30FC\u30F3" },
            { "settings.controls.rebind_hint", "\u5909\u66F4\u3057\u305F\u3044\u64CD\u4F5C\u3092\u9078\u3093\u3067\u3001\u65B0\u3057\u3044\u30AD\u30FC\u3092\u62BC\u3057\u3066\u304F\u3060\u3055\u3044\u3002" },
            { "settings.controls.rebinding", "\u5165\u529B\u5F85\u6A5F\u4E2D... \u30AD\u30FC\u3092\u62BC\u3057\u3066\u304F\u3060\u3055\u3044\uff08Esc\u3067\u30AD\u30E3\u30F3\u30BB\u30EB\uff09\u3002" },
            { "settings.controls.listening", "\u5165\u529B\u5F85\u6A5F\u4E2D..." },
            { "settings.controls.saved", "\u64CD\u4F5C\u3092\u66F4\u65B0\u3057\u307E\u3057\u305F\u3002" },
            { "settings.controls.unbound", "\u672A\u8A2D\u5B9A" },
            { "controls.move_up", "\u4E0A\u3078\u79FB\u52D5" },
            { "controls.move_down", "\u4E0B\u3078\u79FB\u52D5" },
            { "controls.move_left", "\u5DE6\u3078\u79FB\u52D5" },
            { "controls.move_right", "\u53F3\u3078\u79FB\u52D5" },
            { "controls.item1", "\u30A2\u30A4\u30C6\u30E0\u30B9\u30ED\u30C3\u30C81" },
            { "controls.item2", "\u30A2\u30A4\u30C6\u30E0\u30B9\u30ED\u30C3\u30C82" },
            { "controls.item3", "\u30A2\u30A4\u30C6\u30E0\u30B9\u30ED\u30C3\u30C83" },
            { "achievements.empty", "\u5B9F\u7E3E\u304C\u307E\u3060\u8A2D\u5B9A\u3055\u308C\u3066\u3044\u307E\u305B\u3093\u3002" },
            { "achievements.state.complete", "\u9054\u6210" },
            { "missions.daily", "\u30C7\u30A4\u30EA\u30FC\u30DF\u30C3\u30B7\u30E7\u30F3" },
            { "missions.weekly", "\u30A6\u30A3\u30FC\u30AF\u30EA\u30FC\u30DF\u30C3\u30B7\u30E7\u30F3" },
            { "missions.empty", "\u5229\u7528\u53EF\u80FD\u306A\u30DF\u30C3\u30B7\u30E7\u30F3\u306F\u3042\u308A\u307E\u305B\u3093\u3002" },
            { "missions.state.claimed", "\u53D7\u53D6\u6E08\u307F" },
            { "missions.state.ready_to_claim", "\u53D7\u53D6\u53EF\u80FD" },
            { "missions.state.in_progress", "\u9032\u884C\u4E2D" },
            { "menu.challenges.instructions", "\u30E9\u30F3\u3092\u59CB\u3081\u308B\u524D\u306B\u4FEE\u98FE\u5B50\u3092\u9078\u3093\u3067\u304F\u3060\u3055\u3044\u3002" },
            { "menu.challenges.state.active", "\u6709\u52B9" },
            { "menu.challenges.state.inactive", "\u7121\u52B9" },
            { "menu.challenges.action.toggle", "\u30AF\u30EA\u30C3\u30AF\u3067\u5207\u66FF" },
            { "menu.challenges.type.fragile_crystal", "\u8106\u3044\u7D50\u6676" },
            { "menu.challenges.type.the_purge", "\u7CA7\u6E05" },
            { "menu.challenges.type.endless_greed", "\u5C3D\u304D\u306C\u5F37\u6B32" },
            { "menu.challenges.type.the_gladiator", "\u5263\u95D8\u58EB" },
            { "menu.challenges.type.blood_for_power", "\u529B\u306E\u4EE3\u511F" },
            { "menu.challenges.type.crossfire", "\u5341\u5B57\u7832\u706B" },
            { "menu.challenges.type.total_confusion", "\u5B8C\u5168\u6DF7\u4E71" },
            { "menu.challenges.type.rain_of_fire", "\u706B\u306E\u96E8" },
            { "menu.challenges.type.the_unlucky", "\u4E0D\u904B" },
            { "menu.challenges.type.last_breath", "\u6700\u5F8C\u306E\u606F" },
            { "menu.gacha.panel_subtitle", "\u30E1\u30C6\u30AA\u30E9\u30A4\u30C8\u3092\u78BA\u8A8D\u3057\u3001\u5831\u916C\u3092\u898B\u3066\u3001\u30E1\u30A4\u30F3\u30E1\u30CB\u30E5\u30FC\u304B\u3089\u5F15\u3053\u3046\u3002" },
            { "menu.gacha.meteor_rewards", "\u30E1\u30C6\u30AA\u5831\u916C" },
            { "menu.gacha.rewards", "\u5831\u916C" },
            { "menu.gacha.empty", "\u5229\u7528\u53EF\u80FD\u306A\u30E1\u30C6\u30AA\u30D0\u30CA\u30FC\u304C\u3042\u308A\u307E\u305B\u3093\u3002" },
            { "menu.gacha.empty_result", "\u30AC\u30C1\u30E3\u3092\u6709\u52B9\u306B\u3059\u308B\u306B\u306F\u30B2\u30FC\u30E0\u30C7\u30FC\u30BF\u30D9\u30FC\u30B9\u306B\u30E1\u30C6\u30AA\u30D0\u30CA\u30FC\u3092\u8FFD\u52A0\u3057\u3066\u304F\u3060\u3055\u3044\u3002" },
            { "menu.gacha.result_default", "\u30E1\u30C6\u30AA\u3092\u9078\u3073\u3001\u5831\u916C\u3092\u78BA\u8A8D\u3057\u3066\u3001\u6E96\u5099\u304C\u3067\u304D\u305F\u3089\u5F15\u3044\u3066\u304F\u3060\u3055\u3044\u3002" },
            { "menu.gacha.single_pull", "1\u56DE\u5F15\u304F" },
            { "menu.gacha.ten_pull", "10\u56DE\u5F15\u304F" },
            { "menu.gacha.unavailable", "\u5229\u7528\u4E0D\u53EF" },
            { "menu.gacha.no_rewards", "\u8A2D\u5B9A\u3055\u308C\u305F\u5831\u916C\u304C\u3042\u308A\u307E\u305B\u3093\u3002" },
            { "menu.gacha.reward_character", "\u30AD\u30E3\u30E9\u30AF\u30BF\u30FC" },
            { "menu.gacha.reward_weapon", "\u6B66\u5668" },
            { "menu.gacha.reward_consumable", "\u6D88\u8017\u54C1" },
            { "menu.gacha.reward_currency", "\u901A\u8CA8" },
            { "menu.gacha.reward_unknown", "\u4E0D\u660E\u306A\u5831\u916C" },
            { "menu.gacha.rarity.common", "\u30B3\u30E2\u30F3" },
            { "menu.gacha.rarity.rare", "\u30EC\u30A2" },
            { "menu.gacha.rarity.epic", "\u30A8\u30D4\u30C3\u30AF" },
            { "menu.gacha.rarity.mythic", "\u30DF\u30B7\u30C3\u30AF" },
            { "common.rarity.default", "\u57FA\u672C" },
            { "common.rarity.epic", "\u30A8\u30D4\u30C3\u30AF" },
            { "common.rarity.mythic", "\u30DF\u30B7\u30C3\u30AF" },
            { "currency.gold", "\u30B4\u30FC\u30EB\u30C9" },
            { "currency.orb", "\u30AA\u30FC\u30D6" },
            { "currency.ticket", "\u30C1\u30B1\u30C3\u30C8" },
            { "currency.key", "\u9375" },
            { "currency.xp", "XP" },
            { "currency.rupee", "\u30EB\u30D4\u30FC" }
        };

        private static readonly Dictionary<string, string> SupplementalRussianEntries = new Dictionary<string, string>()
        {
            { "settings.display", "\u042D\u043A\u0440\u0430\u043D" },
            { "settings.controls", "\u0423\u043F\u0440\u0430\u0432\u043B\u0435\u043D\u0438\u0435" },
            { "settings.display.current_format", "\u0420\u0435\u0436\u0438\u043C \u043E\u0442\u043E\u0431\u0440\u0430\u0436\u0435\u043D\u0438\u044F: {0}" },
            { "settings.display.windowed", "\u041E\u043A\u043D\u043E" },
            { "settings.display.fullscreen", "\u041F\u043E\u043B\u043D\u044B\u0439 \u044D\u043A\u0440\u0430\u043D" },
            { "settings.controls.rebind_hint", "\u0412\u044B\u0431\u0435\u0440\u0438\u0442\u0435 \u0434\u0435\u0439\u0441\u0442\u0432\u0438\u0435 \u0438 \u043D\u0430\u0436\u043C\u0438\u0442\u0435 \u043D\u043E\u0432\u0443\u044E \u043A\u043B\u0430\u0432\u0438\u0448\u0443." },
            { "settings.controls.rebinding", "\u041E\u0436\u0438\u0434\u0430\u043D\u0438\u0435 \u0432\u0432\u043E\u0434\u0430... \u043D\u0430\u0436\u043C\u0438\u0442\u0435 \u043A\u043B\u0430\u0432\u0438\u0448\u0443 (Esc \u0434\u043B\u044F \u043E\u0442\u043C\u0435\u043D\u044B)." },
            { "settings.controls.listening", "\u041E\u0436\u0438\u0434\u0430\u043D\u0438\u0435 \u0432\u0432\u043E\u0434\u0430..." },
            { "settings.controls.saved", "\u0423\u043F\u0440\u0430\u0432\u043B\u0435\u043D\u0438\u0435 \u043E\u0431\u043D\u043E\u0432\u043B\u0435\u043D\u043E." },
            { "settings.controls.unbound", "\u041D\u0435 \u043D\u0430\u0437\u043D\u0430\u0447\u0435\u043D\u043E" },
            { "controls.move_up", "\u0414\u0432\u0438\u0436\u0435\u043D\u0438\u0435 \u0432\u0432\u0435\u0440\u0445" },
            { "controls.move_down", "\u0414\u0432\u0438\u0436\u0435\u043D\u0438\u0435 \u0432\u043D\u0438\u0437" },
            { "controls.move_left", "\u0414\u0432\u0438\u0436\u0435\u043D\u0438\u0435 \u0432\u043B\u0435\u0432\u043E" },
            { "controls.move_right", "\u0414\u0432\u0438\u0436\u0435\u043D\u0438\u0435 \u0432\u043F\u0440\u0430\u0432\u043E" },
            { "controls.item1", "\u0421\u043B\u043E\u0442 \u043F\u0440\u0435\u0434\u043C\u0435\u0442\u0430 1" },
            { "controls.item2", "\u0421\u043B\u043E\u0442 \u043F\u0440\u0435\u0434\u043C\u0435\u0442\u0430 2" },
            { "controls.item3", "\u0421\u043B\u043E\u0442 \u043F\u0440\u0435\u0434\u043C\u0435\u0442\u0430 3" },
            { "achievements.empty", "\u0414\u043E\u0441\u0442\u0438\u0436\u0435\u043D\u0438\u044F \u043D\u0435 \u043D\u0430\u0441\u0442\u0440\u043E\u0435\u043D\u044B." },
            { "achievements.state.complete", "\u0412\u044B\u043F\u043E\u043B\u043D\u0435\u043D\u043E" },
            { "missions.daily", "\u0415\u0436\u0435\u0434\u043D\u0435\u0432\u043D\u044B\u0435 \u0437\u0430\u0434\u0430\u043D\u0438\u044F" },
            { "missions.weekly", "\u0415\u0436\u0435\u043D\u0435\u0434\u0435\u043B\u044C\u043D\u044B\u0435 \u0437\u0430\u0434\u0430\u043D\u0438\u044F" },
            { "missions.empty", "\u041D\u0435\u0442 \u0434\u043E\u0441\u0442\u0443\u043F\u043D\u044B\u0445 \u0437\u0430\u0434\u0430\u043D\u0438\u0439." },
            { "missions.state.claimed", "\u041F\u043E\u043B\u0443\u0447\u0435\u043D\u043E" },
            { "missions.state.ready_to_claim", "\u041C\u043E\u0436\u043D\u043E \u0437\u0430\u0431\u0440\u0430\u0442\u044C" },
            { "missions.state.in_progress", "\u0412 \u043F\u0440\u043E\u0446\u0435\u0441\u0441\u0435" },
            { "menu.challenges.instructions", "\u0412\u044B\u0431\u0435\u0440\u0438\u0442\u0435 \u043C\u043E\u0434\u0438\u0444\u0438\u043A\u0430\u0442\u043E\u0440\u044B \u043F\u0435\u0440\u0435\u0434 \u043D\u0430\u0447\u0430\u043B\u043E\u043C \u0437\u0430\u0431\u0435\u0433\u0430." },
            { "menu.challenges.state.active", "\u0410\u043A\u0442\u0438\u0432\u043D\u043E" },
            { "menu.challenges.state.inactive", "\u041D\u0435\u0430\u043A\u0442\u0438\u0432\u043D\u043E" },
            { "menu.challenges.action.toggle", "\u041D\u0430\u0436\u043C\u0438\u0442\u0435, \u0447\u0442\u043E\u0431\u044B \u043F\u0435\u0440\u0435\u043A\u043B\u044E\u0447\u0438\u0442\u044C" },
            { "menu.challenges.type.fragile_crystal", "\u0425\u0440\u0443\u043F\u043A\u0438\u0439 \u043A\u0440\u0438\u0441\u0442\u0430\u043B\u043B" },
            { "menu.challenges.type.the_purge", "\u0427\u0438\u0441\u0442\u043A\u0430" },
            { "menu.challenges.type.endless_greed", "\u0411\u0435\u0441\u043A\u043E\u043D\u0435\u0447\u043D\u0430\u044F \u0436\u0430\u0434\u043D\u043E\u0441\u0442\u044C" },
            { "menu.challenges.type.the_gladiator", "\u0413\u043B\u0430\u0434\u0438\u0430\u0442\u043E\u0440" },
            { "menu.challenges.type.blood_for_power", "\u041A\u0440\u043E\u0432\u044C \u0437\u0430 \u0441\u0438\u043B\u0443" },
            { "menu.challenges.type.crossfire", "\u041F\u0435\u0440\u0435\u043A\u0440\u0435\u0441\u0442\u043D\u044B\u0439 \u043E\u0433\u043E\u043D\u044C" },
            { "menu.challenges.type.total_confusion", "\u041F\u043E\u043B\u043D\u0430\u044F \u043D\u0435\u0440\u0430\u0437\u0431\u0435\u0440\u0438\u0445\u0430" },
            { "menu.challenges.type.rain_of_fire", "\u041E\u0433\u043D\u0435\u043D\u043D\u044B\u0439 \u0434\u043E\u0436\u0434\u044C" },
            { "menu.challenges.type.the_unlucky", "\u041D\u0435\u0432\u0435\u0437\u0443\u0447\u0438\u0439" },
            { "menu.challenges.type.last_breath", "\u041F\u043E\u0441\u043B\u0435\u0434\u043D\u0438\u0439 \u0432\u0437\u0434\u043E\u0445" },
            { "menu.gacha.panel_subtitle", "\u041F\u0440\u043E\u0441\u043C\u0430\u0442\u0440\u0438\u0432\u0430\u0439\u0442\u0435 \u043C\u0435\u0442\u0435\u043E\u0440\u0438\u0442\u043D\u044B\u0435 \u0431\u0430\u043D\u043D\u0435\u0440\u044B, \u0438\u0437\u0443\u0447\u0430\u0439\u0442\u0435 \u043D\u0430\u0433\u0440\u0430\u0434\u044B \u0438 \u043A\u0440\u0443\u0442\u0438\u0442\u0435 \u0438\u0437 \u0433\u043B\u0430\u0432\u043D\u043E\u0433\u043E \u043C\u0435\u043D\u044E." },
            { "menu.gacha.meteor_rewards", "\u041D\u0430\u0433\u0440\u0430\u0434\u044B \u043C\u0435\u0442\u0435\u043E\u0440\u0438\u0442\u0430" },
            { "menu.gacha.rewards", "\u041D\u0430\u0433\u0440\u0430\u0434\u044B" },
            { "menu.gacha.empty", "\u0411\u0430\u043D\u043D\u0435\u0440\u044B \u043C\u0435\u0442\u0435\u043E\u0440\u0438\u0442\u043E\u0432 \u043D\u0435 \u043D\u0430\u0441\u0442\u0440\u043E\u0435\u043D\u044B." },
            { "menu.gacha.empty_result", "\u0414\u043E\u0431\u0430\u0432\u044C\u0442\u0435 \u043C\u0435\u0442\u0435\u043E\u0440\u0438\u0442\u043D\u044B\u0435 \u0431\u0430\u043D\u043D\u0435\u0440\u044B \u0432 \u0431\u0430\u0437\u0443 \u0434\u0430\u043D\u043D\u044B\u0445 \u0438\u0433\u0440\u044B, \u0447\u0442\u043E\u0431\u044B \u043E\u0442\u043A\u0440\u044B\u0442\u044C \u043A\u0440\u0443\u0442\u043A\u0438." },
            { "menu.gacha.result_default", "\u0412\u044B\u0431\u0435\u0440\u0438\u0442\u0435 \u043C\u0435\u0442\u0435\u043E\u0440\u0438\u0442, \u0438\u0437\u0443\u0447\u0438\u0442\u0435 \u043D\u0430\u0433\u0440\u0430\u0434\u044B \u0438 \u043A\u0440\u0443\u0442\u0438\u0442\u0435, \u043A\u043E\u0433\u0434\u0430 \u0431\u0443\u0434\u0435\u0442\u0435 \u0433\u043E\u0442\u043E\u0432\u044B." },
            { "menu.gacha.single_pull", "1 \u043A\u0440\u0443\u0442\u043A\u0430" },
            { "menu.gacha.ten_pull", "10 \u043A\u0440\u0443\u0442\u043E\u043A" },
            { "menu.gacha.unavailable", "\u041D\u0435\u0434\u043E\u0441\u0442\u0443\u043F\u043D\u043E" },
            { "menu.gacha.no_rewards", "\u041D\u0430\u0433\u0440\u0430\u0434\u044B \u043D\u0435 \u043D\u0430\u0441\u0442\u0440\u043E\u0435\u043D\u044B." },
            { "menu.gacha.reward_character", "\u041F\u0435\u0440\u0441\u043E\u043D\u0430\u0436" },
            { "menu.gacha.reward_weapon", "\u041E\u0440\u0443\u0436\u0438\u0435" },
            { "menu.gacha.reward_consumable", "\u0420\u0430\u0441\u0445\u043E\u0434\u043D\u0438\u043A" },
            { "menu.gacha.reward_currency", "\u0412\u0430\u043B\u044E\u0442\u0430" },
            { "menu.gacha.reward_unknown", "\u041D\u0435\u0438\u0437\u0432\u0435\u0441\u0442\u043D\u0430\u044F \u043D\u0430\u0433\u0440\u0430\u0434\u0430" },
            { "menu.gacha.rarity.common", "\u041E\u0431\u044B\u0447\u043D\u044B\u0439" },
            { "menu.gacha.rarity.rare", "\u0420\u0435\u0434\u043A\u0438\u0439" },
            { "menu.gacha.rarity.epic", "\u042D\u043F\u0438\u0447\u0435\u0441\u043A\u0438\u0439" },
            { "menu.gacha.rarity.mythic", "\u041C\u0438\u0444\u0438\u0447\u0435\u0441\u043A\u0438\u0439" },
            { "common.rarity.default", "\u0411\u0430\u0437\u043E\u0432\u044B\u0439" },
            { "common.rarity.epic", "\u042D\u043F\u0438\u0447\u0435\u0441\u043A\u0438\u0439" },
            { "common.rarity.mythic", "\u041C\u0438\u0444\u0438\u0447\u0435\u0441\u043A\u0438\u0439" },
            { "currency.gold", "\u0417\u043E\u043B\u043E\u0442\u043E" },
            { "currency.orb", "\u0421\u0444\u0435\u0440\u044B" },
            { "currency.ticket", "\u0411\u0438\u043B\u0435\u0442\u044B" },
            { "currency.key", "\u041A\u043B\u044E\u0447\u0438" },
            { "currency.xp", "\u041E\u043F\u044B\u0442" },
            { "currency.rupee", "\u0420\u0443\u043F\u0438\u0438" }
        };

        private static readonly Dictionary<string, string> AllEnglishEntries = MergeEntries(EnglishEntries, SupplementalEnglishEntries);
        private static readonly Dictionary<string, string> AllSpanishEntries = MergeEntries(SpanishEntries, SupplementalSpanishEntries);
        private static readonly Dictionary<string, string> AllJapaneseEntries = MergeEntries(JapaneseEntries, SupplementalJapaneseEntries);
        private static readonly Dictionary<string, string> AllRussianEntries = MergeEntries(RussianEntries, SupplementalRussianEntries);

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

                foreach (KeyValuePair<string, string> entryPair in AllEnglishEntries)
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
                    return AllSpanishEntries;
                case "ja":
                    return AllJapaneseEntries;
                case "ru":
                    return AllRussianEntries;
                default:
                    return AllEnglishEntries;
            }
        }

        private static Dictionary<string, string> MergeEntries(Dictionary<string, string> baseEntries, Dictionary<string, string> supplementalEntries)
        {
            Dictionary<string, string> merged = new Dictionary<string, string>(baseEntries);
            foreach (KeyValuePair<string, string> entry in supplementalEntries)
            {
                merged[entry.Key] = entry.Value;
            }

            return merged;
        }
    }
}
