const http = require("http");
const fs = require("fs");
const path = require("path");
const crypto = require("crypto");

const PORT = Number(process.env.PORT || 8080);
const DATA_DIR = path.join(__dirname, "data");
const STORE_PATH = path.join(DATA_DIR, "store.json");
const LOG_PATH = path.join(DATA_DIR, "server.log");

ensureDir(DATA_DIR);
if (!fs.existsSync(LOG_PATH)) {
  fs.writeFileSync(LOG_PATH, "");
}

function log(message, payload) {
  const stamp = new Date().toISOString();
  const line = payload === undefined
    ? `[${stamp}] ${message}`
    : `[${stamp}] ${message} ${JSON.stringify(payload)}`;

  console.log(line);

  try {
    ensureDir(DATA_DIR);
    fs.appendFileSync(LOG_PATH, line + "\n");
  } catch (error) {
    console.error(`[${stamp}] Failed to write log file: ${error.message}`);
  }
}

const DAILY_MISSIONS = {
  Easy: [
    { id: "daily_easy_kill_120", title: "Defeat 120 enemies", statKey: "KILLS_REGULAR", targetValue: 120, reward: { type: "Gold", amount: 200 } },
    { id: "daily_easy_open_1_chest", title: "Open 1 chest", statKey: "CHESTS_OPENED", targetValue: 1, reward: { type: "Gold", amount: 180 } },
    { id: "daily_easy_buy_3_items", title: "Purchase 3 items", statKey: "ITEMS_PURCHASED", targetValue: 3, reward: { type: "Gold", amount: 220 } }
  ],
  Medium: [
    { id: "daily_medium_miniboss_1", title: "Defeat 1 miniboss", statKey: "KILLS_MINIBOSS", targetValue: 1, reward: { type: "Gold", amount: 350 } },
    { id: "daily_medium_open_3_chests", title: "Open 3 chests", statKey: "CHESTS_OPENED", targetValue: 3, reward: { type: "Gold", amount: 300 } },
    { id: "daily_medium_spend_250_rupees", title: "Spend 250 rupees", statKey: "RUPEE_SPENT", targetValue: 250, reward: { type: "Gold", amount: 400 } }
  ],
  Hard: [
    { id: "daily_hard_miniboss_2", title: "Defeat 2 minibosses", statKey: "KILLS_MINIBOSS", targetValue: 2, reward: { type: "Gold", amount: 500 } },
    { id: "daily_hard_portal_2", title: "Activate 2 portals", statKey: "PORTALS_OPENED", targetValue: 2, reward: { type: "Gold", amount: 450 } },
    { id: "daily_hard_run_time_900", title: "Reach 15 minutes in a run", statKey: "RUN_TIME", targetValue: 900, reward: { type: "Ticket", amount: 1 } }
  ]
};

const WEEKLY_MISSIONS = [
  { id: "weekly_miniboss_12", title: "Defeat 12 portal minibosses", statKey: "KILLS_MINIBOSS", targetValue: 12, reward: { type: "Gold", amount: 800 } },
  { id: "weekly_open_25_chests", title: "Open 25 chests", statKey: "CHESTS_OPENED", targetValue: 25, reward: { type: "Orbs", amount: 75 } },
  { id: "weekly_spend_2500_rupees", title: "Spend 2500 rupees", statKey: "RUPEE_SPENT", targetValue: 2500, reward: { type: "Gold", amount: 900 } }
];

const DAILY_BONUS = { type: "Orbs", amount: 15 };
const WEEKLY_BONUS = { type: "Orbs", amount: 75 };

function createBanner(id, currencyType, costs) {
  return {
    id,
    currencyType,
    singlePullCost: costs.single,
    tenPullCost: costs.ten,
    pityThreshold: 50,
    epicSoftPity: 10,
    probabilities: { Mythical: 1, Epic: 9, Rare: 30, Common: 60 },
    pools: {
      Common: [{ id: "gold_100", type: "Gold", amount: 100 }],
      Rare: [{ id: `${id}_rare_reward`, type: "Weapon", amount: 1 }],
      Epic: [{ id: `${id}_epic_reward`, type: "Weapon", amount: 1 }],
      Mythical: [{ id: `${id}_mythic_reward`, type: "Weapon", amount: 1 }]
    }
  };
}

const BANNERS = {
  dusty_meteorite: createBanner("dusty_meteorite", "Gold", { single: 500, ten: 5000 }),
  shiny_meteorite: createBanner("shiny_meteorite", "Gold", { single: 700, ten: 7000 }),
  radiant_meteorite: createBanner("radiant_meteorite", "Gold", { single: 900, ten: 9000 }),
  arcane_meteorite: createBanner("arcane_meteorite", "Orbs", { single: 100, ten: 1000 }),
  runic_meteorite: createBanner("runic_meteorite", "Orbs", { single: 100, ten: 1000 }),
  luminous_meteorite: createBanner("luminous_meteorite", "Orbs", { single: 100, ten: 1000 }),
  ethereal_meteorite: createBanner("ethereal_meteorite", "Orbs", { single: 100, ten: 1000 })
};

function ensureDir(dirPath) {
  if (!fs.existsSync(dirPath)) {
    fs.mkdirSync(dirPath, { recursive: true });
  }
}

function defaultStore() {
  return {
    players: {},
    accounts: {}
  };
}

function loadStore() {
  ensureDir(DATA_DIR);
  if (!fs.existsSync(STORE_PATH)) {
    const initial = defaultStore();
    fs.writeFileSync(STORE_PATH, JSON.stringify(initial, null, 2));
    return initial;
  }

  try {
    const parsed = JSON.parse(fs.readFileSync(STORE_PATH, "utf8"));
    if (!parsed.players || typeof parsed.players !== "object") parsed.players = {};
    if (!parsed.accounts || typeof parsed.accounts !== "object") parsed.accounts = {};
    return parsed;
  } catch {
    return defaultStore();
  }
}

const store = loadStore();

function saveStore() {
  ensureDir(DATA_DIR);
  const tempPath = `${STORE_PATH}.tmp`;
  fs.writeFileSync(tempPath, JSON.stringify(store, null, 2));
  fs.renameSync(tempPath, STORE_PATH);
  log("Store saved", {
    path: STORE_PATH,
    playerCount: Object.keys(store.players).length,
    accountCount: Object.keys(store.accounts || {}).length
  });
}

function sendJson(res, statusCode, payload) {
  const body = JSON.stringify(payload, null, 2);
  res.writeHead(statusCode, {
    "Content-Type": "application/json; charset=utf-8",
    "Content-Length": Buffer.byteLength(body)
  });
  log("Response", { statusCode, payload });
  res.end(body);
}

function readJsonBody(req) {
  return new Promise((resolve, reject) => {
    let raw = "";
    req.on("data", chunk => {
      raw += chunk;
      if (raw.length > 1_000_000) {
        reject(new Error("Body too large"));
        req.destroy();
      }
    });
    req.on("end", () => {
      if (!raw) {
        resolve({});
        return;
      }
      try {
        resolve(JSON.parse(raw));
      } catch {
        reject(new Error("Invalid JSON"));
      }
    });
    req.on("error", reject);
  });
}

function getNow() {
  return new Date();
}

function getDayIndex(now) {
  return Math.floor(now.getTime() / 86400000);
}

function getWeekIndex(now) {
  const utcMidnight = Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate());
  const dayOfWeek = now.getUTCDay();
  const mondayOffset = (dayOfWeek + 6) % 7;
  const mondayUtc = utcMidnight - mondayOffset * 86400000;
  return Math.floor(mondayUtc / 86400000);
}

function makeSeed(...parts) {
  return crypto.createHash("sha256").update(parts.join(":")) .digest("hex");
}

function hashToUint32(text) {
  const hex = crypto.createHash("sha256").update(text).digest("hex").slice(0, 8);
  return parseInt(hex, 16) >>> 0;
}

function seededIndex(seed, length) {
  return hashToUint32(seed) % length;
}

function cloneJson(value, fallback) {
  if (value === undefined || value === null) {
    return JSON.parse(JSON.stringify(fallback));
  }

  return JSON.parse(JSON.stringify(value));
}

function defaultSyncableProfile(playerId, displayName = "") {
  return {
    username: displayName || `Guest-${String(playerId).slice(0, 8)}`,
    monthlyPass: {
      is_active: false,
      expiration_date: 0,
      current_exp_progress: 0
    },
    characters: {
      owned_character_ids: [],
      equipped_character_id: "",
      skins: [],
      character_progress: []
    },
    weapons: {
      unlocked_weapon_ids: [],
      weapon_skins: []
    },
    consumables: {
      stash: []
    },
    progression: {
      flags: {
        has_watched_intro_cutscene: false,
        has_completed_tutorial: false,
        has_defeated_final_boss: false,
        is_hard_mode_unlocked: false,
        is_arena_unlocked: false
      },
      tutorial_steps: {
        initial_selection_done: false,
        movement_done: false,
        attack_done: false,
        upgrade_done: false,
        special_attack_done: false,
        shop_purchase_done: false
      }
    }
  };
}

function normalizeSyncableProfile(rawProfile, playerId, displayName = "") {
  const defaults = defaultSyncableProfile(playerId, displayName);
  const profile = Object.assign(defaults, cloneJson(rawProfile, {}));
  profile.username = typeof profile.username === "string" && profile.username.trim()
    ? profile.username.trim()
    : displayName || `Guest-${String(playerId).slice(0, 8)}`;

  profile.monthlyPass = Object.assign(defaults.monthlyPass, cloneJson(profile.monthlyPass, {}));
  profile.characters = Object.assign(defaults.characters, cloneJson(profile.characters, {}));
  profile.weapons = Object.assign(defaults.weapons, cloneJson(profile.weapons, {}));
  profile.consumables = Object.assign(defaults.consumables, cloneJson(profile.consumables, {}));
  profile.progression = Object.assign(defaults.progression, cloneJson(profile.progression, {}));
  profile.progression.flags = Object.assign(defaults.progression.flags, cloneJson(profile.progression.flags, {}));
  profile.progression.tutorial_steps = Object.assign(defaults.progression.tutorial_steps, cloneJson(profile.progression.tutorial_steps, {}));

  if (!Array.isArray(profile.characters.owned_character_ids)) profile.characters.owned_character_ids = [];
  if (!Array.isArray(profile.characters.skins)) profile.characters.skins = [];
  if (!Array.isArray(profile.characters.character_progress)) profile.characters.character_progress = [];
  if (!Array.isArray(profile.weapons.unlocked_weapon_ids)) profile.weapons.unlocked_weapon_ids = [];
  if (!Array.isArray(profile.weapons.weapon_skins)) profile.weapons.weapon_skins = [];
  if (!Array.isArray(profile.consumables.stash)) profile.consumables.stash = [];

  return profile;
}

function defaultProfileState(playerId, displayName = "") {
  return {
    remoteProfileId: `remote_${playerId}`,
    deviceProfileId: playerId,
    accountId: "",
    displayName: displayName || `Guest-${String(playerId).slice(0, 8)}`,
    isGuest: true,
    profileVersion: 0,
    lastSyncUnix: 0,
    data: defaultSyncableProfile(playerId, displayName)
  };
}

function ensureProfileState(player) {
  if (!player.profile) {
    player.profile = defaultProfileState(player.playerId);
  }

  const profile = player.profile;
  if (!profile.remoteProfileId) profile.remoteProfileId = `remote_${player.playerId}`;
  if (!profile.deviceProfileId) profile.deviceProfileId = player.playerId;
  if (typeof profile.accountId !== "string") profile.accountId = "";
  if (typeof profile.displayName !== "string" || !profile.displayName.trim()) {
    profile.displayName = `Guest-${String(player.playerId).slice(0, 8)}`;
  }
  profile.isGuest = !profile.accountId;
  profile.profileVersion = Number.isFinite(profile.profileVersion) ? Math.max(0, Math.floor(profile.profileVersion)) : 0;
  profile.lastSyncUnix = Number.isFinite(profile.lastSyncUnix) ? Math.max(0, Math.floor(profile.lastSyncUnix)) : 0;
  profile.data = normalizeSyncableProfile(profile.data, player.playerId, profile.displayName);
  return profile;
}

function updateProfileMetadata(player, body = {}) {
  const profile = ensureProfileState(player);
  let changed = false;

  if (typeof body.deviceProfileId === "string" && body.deviceProfileId.trim() && profile.deviceProfileId !== body.deviceProfileId.trim()) {
    profile.deviceProfileId = body.deviceProfileId.trim();
    changed = true;
  }

  if (typeof body.accountId === "string" && profile.accountId !== body.accountId.trim()) {
    profile.accountId = body.accountId.trim();
    changed = true;
  }

  if (typeof body.displayName === "string" && body.displayName.trim() && profile.displayName !== body.displayName.trim()) {
    profile.displayName = body.displayName.trim();
    changed = true;
  }

  profile.isGuest = !profile.accountId;
  return changed;
}

function buildProfileResponse(player, conflict = false) {
  const profile = ensureProfileState(player);
  return {
    playerId: player.playerId,
    deviceProfileId: profile.deviceProfileId,
    remoteProfileId: profile.remoteProfileId,
    accountId: profile.accountId,
    displayName: profile.displayName,
    isGuest: profile.isGuest,
    profileVersion: profile.profileVersion,
    serverUnixTime: Math.floor(Date.now() / 1000),
    conflict,
    profile: cloneJson(profile.data, defaultSyncableProfile(player.playerId, profile.displayName)),
    wallet: player.wallet,
    gacha: player.gacha
  };
}

function normalizeEmail(email) {
  return String(email || "").trim().toLowerCase();
}

function generateAccountId() {
  return `acct_${crypto.randomBytes(8).toString("hex")}`;
}

function hashPassword(password, salt) {
  return crypto.createHash("sha256").update(`${salt}:${password}`).digest("hex");
}

function ensureAccountsStore() {
  if (!store.accounts || typeof store.accounts !== "object") {
    store.accounts = {};
  }

  return store.accounts;
}

function sanitizeAccount(account) {
  if (!account) {
    return null;
  }

  return {
    accountId: account.accountId,
    email: account.email,
    displayName: account.displayName,
    linkedPlayerId: account.linkedPlayerId || "",
    createdAt: account.createdAt,
    lastLoginAt: account.lastLoginAt
  };
}

function getAccountByEmail(email) {
  const normalizedEmail = normalizeEmail(email);
  if (!normalizedEmail) {
    return null;
  }

  return ensureAccountsStore()[normalizedEmail] || null;
}

function buildAccountAuthResponse(account, player, conflict = false) {
  const response = {
    ok: true,
    account: sanitizeAccount(account)
  };

  if (player) {
    return Object.assign(response, buildProfileResponse(player, conflict));
  }

  return response;
}

function attachAccountToPlayer(account, player) {
  const profile = ensureProfileState(player);

  if (profile.accountId && profile.accountId !== account.accountId) {
    throw new Error("Profile is already linked to another account");
  }

  if (account.linkedPlayerId && account.linkedPlayerId !== player.playerId) {
    throw new Error("Account is already linked to another profile");
  }

  account.linkedPlayerId = player.playerId;
  profile.accountId = account.accountId;
  profile.isGuest = false;

  if (account.displayName) {
    profile.displayName = account.displayName;
    profile.data = normalizeSyncableProfile(profile.data, player.playerId, profile.displayName);
    profile.data.username = account.displayName;
  }

  profile.lastSyncUnix = Math.floor(Date.now() / 1000);
}

function createAccount(email, password, displayName = "") {
  const normalizedEmail = normalizeEmail(email);
  const trimmedPassword = String(password || "");


  if (!normalizedEmail || !normalizedEmail.includes("@")) {
    throw new Error("A valid email is required");
  }

  if (trimmedPassword.length < 4) {
    throw new Error("Password must be at least 4 characters for dev auth");
  }

  if (getAccountByEmail(normalizedEmail)) {
    throw new Error("Account already exists");
  }

  const now = Date.now();
  const passwordSalt = crypto.randomBytes(8).toString("hex");
  const account = {
    accountId: generateAccountId(),
    email: normalizedEmail,
    passwordSalt,
    passwordHash: hashPassword(trimmedPassword, passwordSalt),
    displayName: String(displayName || "").trim() || normalizedEmail.split("@")[0],
    linkedPlayerId: "",
    createdAt: now,
    lastLoginAt: now
  };

  ensureAccountsStore()[normalizedEmail] = account;
  return account;
}

function validateAccountCredentials(email, password) {
  const account = getAccountByEmail(email);
  if (!account) {
    return { account: null, error: "Account not found" };
  }

  const attemptedHash = hashPassword(String(password || ""), account.passwordSalt);
  if (attemptedHash !== account.passwordHash) {
    return { account: null, error: "Invalid password" };
  }

  account.lastLoginAt = Date.now();
  return { account, error: null };
}

function defaultPlayer(playerId, seed = {}) {
  const seedWallet = seed.wallet || {};
  const seedGacha = seed.gacha || {};
  return {
    playerId,
    createdAt: Date.now(),
    wallet: {
      Gold: Number.isFinite(seedWallet.Gold) ? Math.max(0, Math.floor(seedWallet.Gold)) : 5000,
      Orbs: Number.isFinite(seedWallet.Orbs) ? Math.max(0, Math.floor(seedWallet.Orbs)) : 5000,
      Ticket: Number.isFinite(seedWallet.Ticket) ? Math.max(0, Math.floor(seedWallet.Ticket)) : 3
    },
    gacha: {
      totalPullsLifetime: Number.isFinite(seedGacha.totalPullsLifetime) ? Math.max(0, Math.floor(seedGacha.totalPullsLifetime)) : 0,
      currentPityCounter: Number.isFinite(seedGacha.currentPityCounter) ? Math.max(0, Math.floor(seedGacha.currentPityCounter)) : 0,
      consecutivePullsNoEpic: Number.isFinite(seedGacha.consecutivePullsNoEpic) ? Math.max(0, Math.floor(seedGacha.consecutivePullsNoEpic)) : 0
    },
    inventory: {
      weapons: [],
      characters: [],
      consumables: {}
    },
    profile: defaultProfileState(playerId, seed.displayName || ""),
    missionCycles: {
      dayIndex: null,
      weekIndex: null,
      dailySeed: null,
      weeklySeed: null,
      dailyRerollUsed: false,
      dailyBonusClaimed: false,
      weeklyBonusClaimed: false,
      dailyMissions: [],
      weeklyMissions: [],
      statTotals: {}
    }
  };
}

function getPlayer(playerId) {
  if (!playerId) {
    return null;
  }

  if (!store.players[playerId]) {
    store.players[playerId] = defaultPlayer(playerId);
    saveStore();
    log("Created default player", { playerId });
  }

  ensureProfileState(store.players[playerId]);
  return store.players[playerId];
}

function assignReward(player, reward) {
  if (!reward || !reward.type) {
    log("Skipped reward assignment because reward was empty", { playerId: player?.playerId });
    return;
  }

  const walletBefore = { ...player.wallet };

  if (reward.type === "Weapon") {
    if (!player.inventory.weapons.includes(reward.id)) {
      player.inventory.weapons.push(reward.id);
      log("Granted weapon reward", { playerId: player.playerId, rewardId: reward.id });
    } else {
      player.wallet.Gold += 300;
      log("Weapon duplicate converted to gold", { playerId: player.playerId, rewardId: reward.id, convertedGold: 300 });
    }
    log("Wallet after weapon reward handling", { playerId: player.playerId, before: walletBefore, after: player.wallet });
    return;
  }

  if (reward.type === "Character") {
    if (!player.inventory.characters.includes(reward.id)) {
      player.inventory.characters.push(reward.id);
      log("Granted character reward", { playerId: player.playerId, rewardId: reward.id });
    } else {
      player.wallet.Orbs += 150;
      log("Character duplicate converted to orbs", { playerId: player.playerId, rewardId: reward.id, convertedOrbs: 150 });
    }
    log("Wallet after character reward handling", { playerId: player.playerId, before: walletBefore, after: player.wallet });
    return;
  }

  player.wallet[reward.type] = (player.wallet[reward.type] || 0) + (reward.amount || 0);
  log("Granted wallet reward", { playerId: player.playerId, rewardType: reward.type, rewardAmount: reward.amount || 0, before: walletBefore, after: player.wallet });
}

function createMissionEntry(mission, existingProgress) {
  const progress = existingProgress?.[mission.statKey] || 0;
  return {
    missionId: mission.id,
    title: mission.title,
    statKey: mission.statKey,
    targetValue: mission.targetValue,
    reward: mission.reward,
    currentProgress: Math.min(progress, mission.targetValue),
    isCompleted: progress >= mission.targetValue,
    isClaimed: false
  };
}

function refreshMissionCompletion(mission, statTotals) {
  const progress = statTotals[mission.statKey] || 0;
  mission.currentProgress = Math.min(progress, mission.targetValue);
  mission.isCompleted = progress >= mission.targetValue;
}

function ensureMissionCycles(player) {
  const now = getNow();
  const dayIndex = getDayIndex(now);
  const weekIndex = getWeekIndex(now);
  const cycles = player.missionCycles;

  if (cycles.dayIndex !== dayIndex) {
    cycles.dayIndex = dayIndex;
    cycles.dailySeed = makeSeed(player.playerId, "daily", dayIndex);
    cycles.dailyRerollUsed = false;
    cycles.dailyBonusClaimed = false;

    const easy = DAILY_MISSIONS.Easy[seededIndex(`${cycles.dailySeed}:easy`, DAILY_MISSIONS.Easy.length)];
    const medium = DAILY_MISSIONS.Medium[seededIndex(`${cycles.dailySeed}:medium`, DAILY_MISSIONS.Medium.length)];
    const hard = DAILY_MISSIONS.Hard[seededIndex(`${cycles.dailySeed}:hard`, DAILY_MISSIONS.Hard.length)];
    cycles.dailyMissions = [
      createMissionEntry(easy, cycles.statTotals),
      createMissionEntry(medium, cycles.statTotals),
      createMissionEntry(hard, cycles.statTotals)
    ];
    log("Generated daily missions", { playerId: player.playerId, dayIndex, missionIds: cycles.dailyMissions.map(m => m.missionId) });
  }

  if (cycles.weekIndex !== weekIndex) {
    cycles.weekIndex = weekIndex;
    cycles.weeklySeed = makeSeed(player.playerId, "weekly", weekIndex);
    cycles.weeklyBonusClaimed = false;
    const weekly = WEEKLY_MISSIONS[seededIndex(cycles.weeklySeed, WEEKLY_MISSIONS.length)];
    cycles.weeklyMissions = [createMissionEntry(weekly, cycles.statTotals)];
    log("Generated weekly missions", { playerId: player.playerId, weekIndex, missionIds: cycles.weeklyMissions.map(m => m.missionId) });
  }

  for (const mission of [...cycles.dailyMissions, ...cycles.weeklyMissions]) {
    refreshMissionCompletion(mission, cycles.statTotals);
  }
}

function findMission(player, missionId) {
  const missions = [...player.missionCycles.dailyMissions, ...player.missionCycles.weeklyMissions];
  return missions.find(m => m.missionId === missionId) || null;
}

function applyMissionProgress(player, statKey, amount) {
  const before = player.missionCycles.statTotals[statKey] || 0;
  const total = Math.max(0, before + amount);
  player.missionCycles.statTotals[statKey] = total;
  for (const mission of [...player.missionCycles.dailyMissions, ...player.missionCycles.weeklyMissions]) {
    if (mission.statKey === statKey) {
      refreshMissionCompletion(mission, player.missionCycles.statTotals);
    }
  }
  log("Applied mission progress", { playerId: player.playerId, statKey, delta: amount, before, after: total });
}

function maybeGrantMissionBonus(player) {
  const cycles = player.missionCycles;

  if (!cycles.dailyBonusClaimed && cycles.dailyMissions.length > 0 && cycles.dailyMissions.every(m => m.isClaimed)) {
    assignReward(player, DAILY_BONUS);
    cycles.dailyBonusClaimed = true;
    log("Granted daily mission bonus", { playerId: player.playerId, reward: DAILY_BONUS });
  }

  if (!cycles.weeklyBonusClaimed && cycles.weeklyMissions.length > 0 && cycles.weeklyMissions.every(m => m.isClaimed)) {
    assignReward(player, WEEKLY_BONUS);
    cycles.weeklyBonusClaimed = true;
    log("Granted weekly mission bonus", { playerId: player.playerId, reward: WEEKLY_BONUS });
  }
}

function chooseRarity(banner, gachaState, pullSeed) {
  const hardPityActive = gachaState.currentPityCounter >= banner.pityThreshold;
  const epicPityActive = gachaState.consecutivePullsNoEpic >= banner.epicSoftPity;
  const roll = (hashToUint32(pullSeed) % 10000) / 100;

  const mythicChance = hardPityActive ? 50 : banner.probabilities.Mythical;
  if (roll < mythicChance) return "Mythical";
  if (epicPityActive) return "Epic";
  if (roll < mythicChance + banner.probabilities.Epic) return "Epic";
  if (roll < mythicChance + banner.probabilities.Epic + banner.probabilities.Rare) return "Rare";
  return "Common";
}

function pickReward(banner, rarity, pullSeed) {
  const pool = banner.pools[rarity] || [];
  if (pool.length === 0) {
    return { id: "fallback_gold", type: "Gold", amount: 100 };
  }
  return pool[seededIndex(`${pullSeed}:${rarity}`, pool.length)];
}

function routeNotFound(res) {
  sendJson(res, 404, { error: "Not found" });
}

const server = http.createServer(async (req, res) => {
  try {
    const url = new URL(req.url, `http://${req.headers.host}`);
    const pathname = url.pathname;
    log("Incoming request", { method: req.method, pathname, query: Object.fromEntries(url.searchParams.entries()) });

    if (req.method === "GET" && pathname === "/health") {
      sendJson(res, 200, { ok: true, service: "eclipside-backend-quick" });
      return;
    }

    if (req.method === "POST" && pathname === "/player/init") {
      const body = await readJsonBody(req);
      log("Player init body", body);
      const playerId = body.playerId || `player-${Date.now()}`;
      if (!store.players[playerId]) {
        store.players[playerId] = defaultPlayer(playerId, {
          wallet: body.seedWallet,
          gacha: body.seedGacha
        });
        log("Created seeded player", { playerId, seedWallet: body.seedWallet, seedGacha: body.seedGacha });
      }
      const player = getPlayer(playerId);
      ensureMissionCycles(player);
      saveStore();
      log("Player init result", { playerId: player.playerId, wallet: player.wallet, dayIndex: player.missionCycles.dayIndex, weekIndex: player.missionCycles.weekIndex });
      sendJson(res, 200, { playerId: player.playerId, wallet: player.wallet, missionCycles: player.missionCycles });
      return;
    }

    if (req.method === "POST" && pathname === "/auth/register") {
      const body = await readJsonBody(req);
      const normalizedEmail = normalizeEmail(body.email);
      log("Auth register request", { email: normalizedEmail, playerId: body.playerId, linkCurrentPlayer: body.linkCurrentPlayer === true });

      try {
        const account = createAccount(body.email, body.password, body.displayName);
        let player = null;

        if (body.linkCurrentPlayer === true && body.playerId) {
          player = getPlayer(body.playerId);
          ensureMissionCycles(player);
          attachAccountToPlayer(account, player);
        }

        saveStore();
        log("Auth register success", { email: account.email, accountId: account.accountId, linkedPlayerId: account.linkedPlayerId || "" });
        sendJson(res, 200, buildAccountAuthResponse(account, player, false));
      } catch (error) {
        log("Auth register failed", { email: normalizedEmail, error: error.message });
        sendJson(res, error.message === "Account already exists" ? 409 : 400, { error: error.message });
      }
      return;
    }

    if (req.method === "POST" && pathname === "/auth/login") {
      const body = await readJsonBody(req);
      const normalizedEmail = normalizeEmail(body.email);
      log("Auth login request", { email: normalizedEmail, playerId: body.playerId, linkCurrentPlayer: body.linkCurrentPlayer === true });

      const { account, error } = validateAccountCredentials(body.email, body.password);
      if (error) {
        log("Auth login failed", { email: normalizedEmail, error });
        sendJson(res, error === "Account not found" ? 404 : 401, { error });
        return;
      }

      let player = account.linkedPlayerId ? getPlayer(account.linkedPlayerId) : null;
      if (player) {
        ensureMissionCycles(player);
      }

      if (body.linkCurrentPlayer === true && body.playerId) {
        const localPlayer = getPlayer(body.playerId);
        ensureMissionCycles(localPlayer);

        if (!account.linkedPlayerId) {
          attachAccountToPlayer(account, localPlayer);
          player = localPlayer;
          log("Auth login linked account to current player", { email: account.email, accountId: account.accountId, linkedPlayerId: localPlayer.playerId });
        } else if (account.linkedPlayerId === localPlayer.playerId) {
          player = localPlayer;
        } else {
          log("Auth login returned existing linked profile", { email: account.email, accountId: account.accountId, requestedPlayerId: localPlayer.playerId, linkedPlayerId: account.linkedPlayerId });
        }
      }

      saveStore();
      log("Auth login success", { email: account.email, accountId: account.accountId, linkedPlayerId: account.linkedPlayerId || "" });
      sendJson(res, 200, buildAccountAuthResponse(account, player, false));
      return;
    }

    if (req.method === "POST" && pathname === "/profile/link-account") {
      const body = await readJsonBody(req);
      const normalizedEmail = normalizeEmail(body.email);
      log("Profile link-account request", { email: normalizedEmail, playerId: body.playerId });

      if (!body.playerId) {
        sendJson(res, 400, { error: "playerId is required" });
        return;
      }

      const player = getPlayer(body.playerId);
      ensureMissionCycles(player);
      const profile = ensureProfileState(player);
      const auth = validateAccountCredentials(body.email, body.password);

      if (auth.error) {
        log("Profile link-account failed", { email: normalizedEmail, playerId: body.playerId, error: auth.error });
        sendJson(res, auth.error === "Account not found" ? 404 : 401, { error: auth.error });
        return;
      }

      const account = auth.account;
      if (account.linkedPlayerId && account.linkedPlayerId !== player.playerId) {
        log("Profile link-account failed", { email: normalizedEmail, playerId: body.playerId, accountId: account.accountId, linkedPlayerId: account.linkedPlayerId, error: "Account is already linked to another profile" });
        sendJson(res, 409, { error: "Account is already linked to another profile" });
        return;
      }

      if (profile.accountId && profile.accountId !== account.accountId) {
        log("Profile link-account failed", { email: normalizedEmail, playerId: body.playerId, profileAccountId: profile.accountId, error: "Profile is already linked to another account" });
        sendJson(res, 409, { error: "Profile is already linked to another account" });
        return;
      }

      attachAccountToPlayer(account, player);
      saveStore();
      log("Profile link-account success", { email: account.email, accountId: account.accountId, linkedPlayerId: player.playerId });
      sendJson(res, 200, buildAccountAuthResponse(account, player, false));
      return;
    }

    if (req.method === "POST" && pathname === "/profile/bootstrap") {
      const body = await readJsonBody(req);
      log("Profile bootstrap body", body);
      const playerId = body.playerId || body.deviceProfileId || `player-${Date.now()}`;

      if (!store.players[playerId]) {
        store.players[playerId] = defaultPlayer(playerId, {
          wallet: body.seedWallet,
          gacha: body.seedGacha,
          displayName: body.displayName
        });
        log("Created seeded player during profile bootstrap", { playerId, seedWallet: body.seedWallet, seedGacha: body.seedGacha });
      }

      const player = getPlayer(playerId);
      ensureMissionCycles(player);
      updateProfileMetadata(player, body);
      const profile = ensureProfileState(player);

      if (body.profile && profile.profileVersion === 0) {
        profile.data = normalizeSyncableProfile(body.profile, player.playerId, profile.displayName);
        profile.profileVersion = 1;
        profile.lastSyncUnix = Math.floor(Date.now() / 1000);
        if (profile.data.username) {
          profile.displayName = profile.data.username;
        }
        log("Profile bootstrap seeded server profile", { playerId: player.playerId, profileVersion: profile.profileVersion, remoteProfileId: profile.remoteProfileId });
      } else {
        log("Profile bootstrap returned existing server profile", { playerId: player.playerId, profileVersion: profile.profileVersion, remoteProfileId: profile.remoteProfileId });
      }

      saveStore();
      sendJson(res, 200, buildProfileResponse(player, false));
      return;
    }

    if (req.method === "POST" && pathname === "/profile/sync") {
      const body = await readJsonBody(req);
      log("Profile sync body", body);
      const player = getPlayer(body.playerId || body.deviceProfileId);

      if (!player) {
        sendJson(res, 400, { error: "playerId or deviceProfileId is required" });
        return;
      }

      ensureMissionCycles(player);
      updateProfileMetadata(player, body);
      const profile = ensureProfileState(player);
      const clientVersion = Number.isFinite(Number(body.lastKnownProfileVersion))
        ? Math.max(0, Math.floor(Number(body.lastKnownProfileVersion)))
        : 0;
      const pushLocalChanges = body.pushLocalChanges === true && body.profile;
      let conflict = false;

      if (pushLocalChanges) {
        if (clientVersion !== profile.profileVersion) {
          conflict = true;
          log("Profile sync conflict", { playerId: player.playerId, clientVersion, serverVersion: profile.profileVersion });
        } else {
          profile.data = normalizeSyncableProfile(body.profile, player.playerId, profile.displayName);
          if (profile.data.username) {
            profile.displayName = profile.data.username;
          }
          profile.profileVersion += 1;
          profile.lastSyncUnix = Math.floor(Date.now() / 1000);
          log("Profile sync push accepted", { playerId: player.playerId, profileVersion: profile.profileVersion, remoteProfileId: profile.remoteProfileId });
        }
      } else {
        log("Profile sync pull", { playerId: player.playerId, profileVersion: profile.profileVersion, remoteProfileId: profile.remoteProfileId });
      }

      saveStore();
      sendJson(res, 200, buildProfileResponse(player, conflict));
      return;
    }

    if (req.method === "GET" && pathname === "/time-sync") {
      const player = getPlayer(url.searchParams.get("playerId"));
      if (!player) {
        sendJson(res, 400, { error: "playerId is required" });
        return;
      }
      ensureMissionCycles(player);
      saveStore();
      const now = getNow();
      log("Time sync response", { playerId: player.playerId, serverUnixTime: Math.floor(now.getTime() / 1000), serverDayIndex: player.missionCycles.dayIndex, serverWeekIndex: player.missionCycles.weekIndex });
      sendJson(res, 200, {
        playerId: player.playerId,
        serverUnixTime: Math.floor(now.getTime() / 1000),
        serverDayIndex: player.missionCycles.dayIndex,
        serverWeekIndex: player.missionCycles.weekIndex,
        dailySeed: player.missionCycles.dailySeed,
        weeklySeed: player.missionCycles.weeklySeed
      });
      return;
    }

    if (req.method === "GET" && pathname === "/missions/state") {
      const player = getPlayer(url.searchParams.get("playerId"));
      if (!player) {
        sendJson(res, 400, { error: "playerId is required" });
        return;
      }
      ensureMissionCycles(player);
      saveStore();
      log("Mission state response", { playerId: player.playerId, dailyMissionIds: player.missionCycles.dailyMissions.map(m => m.missionId), weeklyMissionIds: player.missionCycles.weeklyMissions.map(m => m.missionId), wallet: player.wallet });
      sendJson(res, 200, {
        playerId: player.playerId,
        dayIndex: player.missionCycles.dayIndex,
        weekIndex: player.missionCycles.weekIndex,
        dailyRerollUsed: player.missionCycles.dailyRerollUsed,
        dailyBonusClaimed: player.missionCycles.dailyBonusClaimed,
        weeklyBonusClaimed: player.missionCycles.weeklyBonusClaimed,
        dailyMissions: player.missionCycles.dailyMissions,
        weeklyMissions: player.missionCycles.weeklyMissions,
        wallet: player.wallet
      });
      return;
    }

    if (req.method === "POST" && pathname === "/missions/progress") {
      const body = await readJsonBody(req);
      log("Mission progress body", body);
      const player = getPlayer(body.playerId);
      const statKey = body.statKey;
      const amount = Number(body.amount || 0);

      if (!player || !statKey || !Number.isFinite(amount)) {
        sendJson(res, 400, { error: "playerId, statKey, and numeric amount are required" });
        return;
      }

      ensureMissionCycles(player);
      applyMissionProgress(player, statKey, amount);
      saveStore();
      log("Mission progress response", { playerId: player.playerId, statKey, total: player.missionCycles.statTotals[statKey], dailyMissions: player.missionCycles.dailyMissions, weeklyMissions: player.missionCycles.weeklyMissions });
      sendJson(res, 200, {
        ok: true,
        statKey,
        total: player.missionCycles.statTotals[statKey],
        dailyMissions: player.missionCycles.dailyMissions,
        weeklyMissions: player.missionCycles.weeklyMissions
      });
      return;
    }

    if (req.method === "POST" && pathname === "/missions/claim") {
      const body = await readJsonBody(req);
      log("Mission claim body", body);
      const player = getPlayer(body.playerId);
      const missionId = body.missionId;

      if (!player || !missionId) {
        sendJson(res, 400, { error: "playerId and missionId are required" });
        return;
      }

      ensureMissionCycles(player);
      const mission = findMission(player, missionId);
      if (!mission) {
        log("Mission claim failed: mission not found", { playerId: body.playerId, missionId });
        sendJson(res, 404, { error: "Mission not found" });
        return;
      }
      if (mission.isClaimed) {
        log("Mission claim failed: already claimed", { playerId: player.playerId, missionId });
        sendJson(res, 409, { error: "Mission already claimed" });
        return;
      }
      if (!mission.isCompleted) {
        log("Mission claim failed: not completed", { playerId: player.playerId, missionId, currentProgress: mission.currentProgress, targetValue: mission.targetValue });
        sendJson(res, 409, { error: "Mission is not completed" });
        return;
      }

      const walletBefore = { ...player.wallet };
      mission.isClaimed = true;
      assignReward(player, mission.reward);
      maybeGrantMissionBonus(player);
      saveStore();
      log("Mission claim success", { playerId: player.playerId, missionId, reward: mission.reward, walletBefore, walletAfter: player.wallet, dailyBonusClaimed: player.missionCycles.dailyBonusClaimed, weeklyBonusClaimed: player.missionCycles.weeklyBonusClaimed });

      sendJson(res, 200, {
        ok: true,
        claimedMissionId: missionId,
        reward: mission.reward,
        wallet: player.wallet,
        dailyBonusClaimed: player.missionCycles.dailyBonusClaimed,
        weeklyBonusClaimed: player.missionCycles.weeklyBonusClaimed
      });
      return;
    }

    if (req.method === "POST" && pathname === "/missions/reroll") {
      const body = await readJsonBody(req);
      log("Mission reroll body", body);
      const player = getPlayer(body.playerId);
      const missionId = body.missionId;

      if (!player || !missionId) {
        sendJson(res, 400, { error: "playerId and missionId are required" });
        return;
      }

      ensureMissionCycles(player);
      if (player.missionCycles.dailyRerollUsed) {
        log("Mission reroll failed: already used", { playerId: player.playerId, missionId });
        sendJson(res, 409, { error: "Daily reroll already used" });
        return;
      }

      const index = player.missionCycles.dailyMissions.findIndex(m => m.missionId === missionId);
      if (index === -1) {
        log("Mission reroll failed: daily mission not found", { playerId: player.playerId, missionId });
        sendJson(res, 404, { error: "Daily mission not found" });
        return;
      }

      const oldMission = player.missionCycles.dailyMissions[index];
      const difficulty = index === 0 ? "Easy" : index === 1 ? "Medium" : "Hard";
      const pool = DAILY_MISSIONS[difficulty];
      const alternatives = pool.filter(m => m.id !== oldMission.missionId);
      const seed = makeSeed(player.playerId, "reroll", player.missionCycles.dayIndex, oldMission.missionId);
      const replacement = alternatives[seededIndex(seed, alternatives.length)];

      player.missionCycles.dailyMissions[index] = createMissionEntry(replacement, player.missionCycles.statTotals);
      player.missionCycles.dailyRerollUsed = true;
      saveStore();
      log("Mission reroll success", { playerId: player.playerId, replacedMissionId: missionId, newMissionId: player.missionCycles.dailyMissions[index].missionId });

      sendJson(res, 200, {
        ok: true,
        replacedMissionId: missionId,
        newMission: player.missionCycles.dailyMissions[index],
        dailyRerollUsed: true
      });
      return;
    }

    if (req.method === "POST" && pathname === "/gacha/pull") {
      const body = await readJsonBody(req);
      log("Gacha pull body", body);
      const player = getPlayer(body.playerId);
      const banner = BANNERS[body.bannerId];
      const pullCount = Number(body.pullCount || 1);

      if (!player || !banner) {
        sendJson(res, 400, { error: "Valid playerId and bannerId are required" });
        return;
      }
      if (![1, 10].includes(pullCount)) {
        sendJson(res, 400, { error: "pullCount must be 1 or 10" });
        return;
      }

      const cost = pullCount === 10 ? banner.tenPullCost : banner.singlePullCost;
      const currency = banner.currencyType;
      if ((player.wallet[currency] || 0) < cost) {
        log("Gacha pull failed: insufficient currency", { playerId: player.playerId, bannerId: body.bannerId, currency, cost, balance: player.wallet[currency] || 0 });
        sendJson(res, 409, { error: `Not enough ${currency}` });
        return;
      }

      const walletBefore = { ...player.wallet };
      player.wallet[currency] -= cost;

      const results = [];
      for (let i = 0; i < pullCount; i += 1) {
        player.gacha.totalPullsLifetime += 1;
        player.gacha.currentPityCounter += 1;
        player.gacha.consecutivePullsNoEpic += 1;

        const pullSeed = makeSeed(player.playerId, banner.id, player.gacha.totalPullsLifetime, i);
        const rarity = chooseRarity(banner, player.gacha, pullSeed);

        if (rarity === "Mythical") {
          player.gacha.currentPityCounter = 0;
        }
        if (rarity === "Epic" || rarity === "Mythical") {
          player.gacha.consecutivePullsNoEpic = 0;
        }

        const reward = pickReward(banner, rarity, pullSeed);
        assignReward(player, reward);
        results.push({ rarity, reward });
      }

      saveStore();
      log("Gacha pull success", { playerId: player.playerId, bannerId: banner.id, pullCount, currencySpent: { type: currency, amount: cost }, walletBefore, walletAfter: player.wallet, gacha: player.gacha, results });
      sendJson(res, 200, {
        ok: true,
        bannerId: banner.id,
        pullCount,
        currencySpent: { type: currency, amount: cost },
        wallet: player.wallet,
        gacha: player.gacha,
        results
      });
      return;
    }

    routeNotFound(res);
  } catch (error) {
    log("Unhandled server error", { message: error.message, stack: error.stack });
    sendJson(res, 500, { error: error.message || "Internal server error" });
  }
});

server.listen(PORT, () => {
  log("Eclipside quick backend listening", {
    url: `http://localhost:${PORT}`,
    storePath: STORE_PATH,
    logPath: LOG_PATH
  });
});










