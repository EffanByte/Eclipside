const crypto = require("crypto");

const BASE_URL = (process.env.BASE_URL || "http://localhost:8097").replace(/\/+$/, "");
const BANNER_ID = process.env.BANNER_ID || "dusty_meteorite";
const PULL_COUNT = Number(process.env.PULL_COUNT || 10);
const SEED_GOLD = Number(process.env.SEED_GOLD || 1000000);
const SEED_ORBS = Number(process.env.SEED_ORBS || 1000000);
const SAME_PLAYER_REQUESTS = Number(process.env.SAME_PLAYER_REQUESTS || 80);
const SAME_PLAYER_CONCURRENCY = Number(process.env.SAME_PLAYER_CONCURRENCY || 12);
const MULTI_PLAYER_COUNT = Number(process.env.MULTI_PLAYER_COUNT || 24);
const MULTI_PLAYER_REQUESTS = Number(process.env.MULTI_PLAYER_REQUESTS || 12);
const MULTI_PLAYER_CONCURRENCY = Number(process.env.MULTI_PLAYER_CONCURRENCY || 24);

const COST_BY_BANNER = {
  dusty_meteorite: { currency: "Gold", single: 500, ten: 5000 },
  shiny_meteorite: { currency: "Gold", single: 700, ten: 7000 },
  radiant_meteorite: { currency: "Gold", single: 900, ten: 9000 },
  arcane_meteorite: { currency: "Orbs", single: 100, ten: 1000 },
  runic_meteorite: { currency: "Orbs", single: 100, ten: 1000 },
  luminous_meteorite: { currency: "Orbs", single: 100, ten: 1000 },
  ethereal_meteorite: { currency: "Orbs", single: 100, ten: 1000 }
};

function assertNumber(value, label) {
  if (!Number.isFinite(value) || value < 0) {
    throw new Error(`Invalid numeric configuration for ${label}: ${value}`);
  }
}

async function requestJson(path, method = "GET", body) {
  const response = await fetch(`${BASE_URL}${path}`, {
    method,
    headers: {
      "Content-Type": "application/json"
    },
    body: body === undefined ? undefined : JSON.stringify(body)
  });

  const text = await response.text();
  let json = null;
  if (text) {
    try {
      json = JSON.parse(text);
    } catch (error) {
      throw new Error(`Failed to parse JSON from ${path}: ${error.message}\n${text}`);
    }
  }

  if (!response.ok) {
    const detail = json && json.error ? json.error : text || response.statusText;
    throw new Error(`${method} ${path} failed: ${response.status} ${detail}`);
  }

  return json;
}

async function initPlayer(playerId) {
  return requestJson("/player/init", "POST", {
    playerId,
    seedWallet: {
      Gold: SEED_GOLD,
      Orbs: SEED_ORBS,
      Ticket: 0
    },
    seedGacha: {
      totalPullsLifetime: 0,
      currentPityCounter: 0,
      consecutivePullsNoEpic: 0
    }
  });
}

async function pull(playerId) {
  const start = Date.now();
  const response = await requestJson("/gacha/pull", "POST", {
    playerId,
    bannerId: BANNER_ID,
    pullCount: PULL_COUNT
  });

  return {
    latencyMs: Date.now() - start,
    response
  };
}

async function fetchProfile(playerId) {
  return requestJson("/profile/sync", "POST", {
    playerId,
    lastKnownProfileVersion: 0,
    pushLocalChanges: false
  });
}

async function runPool(tasks, concurrency, worker) {
  const results = new Array(tasks.length);
  let cursor = 0;

  async function runOne() {
    while (true) {
      const index = cursor;
      cursor += 1;
      if (index >= tasks.length) {
        return;
      }

      results[index] = await worker(tasks[index], index);
    }
  }

  const workers = [];
  const workerCount = Math.max(1, Math.min(concurrency, tasks.length));
  for (let i = 0; i < workerCount; i += 1) {
    workers.push(runOne());
  }

  await Promise.all(workers);
  return results;
}

function percentile(sortedValues, fraction) {
  if (sortedValues.length === 0) {
    return 0;
  }

  const index = Math.min(sortedValues.length - 1, Math.max(0, Math.ceil(sortedValues.length * fraction) - 1));
  return sortedValues[index];
}

function summarizeLatencies(latencies) {
  const sorted = [...latencies].sort((a, b) => a - b);
  const total = sorted.reduce((sum, value) => sum + value, 0);
  return {
    minMs: sorted[0] || 0,
    avgMs: sorted.length > 0 ? Number((total / sorted.length).toFixed(2)) : 0,
    p95Ms: percentile(sorted, 0.95),
    maxMs: sorted[sorted.length - 1] || 0
  };
}

function buildGoldExpectation(seedGold, requestResults, costPerRequest) {
  let rewardGold = 0;

  for (const result of requestResults) {
    if (!result || !result.response || !Array.isArray(result.response.results)) {
      continue;
    }

    for (const pullResult of result.response.results) {
      if (pullResult?.reward?.type === "Gold") {
        rewardGold += Number(pullResult.reward.amount || 0);
      }

      if (pullResult?.duplicateConversion?.type === "Gold") {
        rewardGold += Number(pullResult.duplicateConversion.amount || 0);
      }
    }
  }

  return seedGold - requestResults.length * costPerRequest + rewardGold;
}

function flattenGrantedWeapons(requestResults) {
  const granted = new Set();
  for (const result of requestResults) {
    if (!result?.response?.results) {
      continue;
    }

    for (const pullResult of result.response.results) {
      if (pullResult?.granted && pullResult?.reward?.type === "Weapon" && pullResult.reward.id) {
        granted.add(pullResult.reward.id);
      }
    }
  }

  return [...granted].sort();
}

async function runSamePlayerScenario() {
  const playerId = `stress-same-${crypto.randomUUID()}`;
  await initPlayer(playerId);

  const tasks = Array.from({ length: SAME_PLAYER_REQUESTS }, (_, index) => index);
  const failures = [];
  const results = await runPool(tasks, SAME_PLAYER_CONCURRENCY, async () => {
    try {
      return await pull(playerId);
    } catch (error) {
      failures.push(error.message);
      return null;
    }
  });

  const successful = results.filter(Boolean);
  const profile = await fetchProfile(playerId);
  const bannerCost = COST_BY_BANNER[BANNER_ID][PULL_COUNT === 10 ? "ten" : "single"];
  const expectedPulls = successful.length * PULL_COUNT;
  const expectedGold = buildGoldExpectation(SEED_GOLD, successful, bannerCost);
  const finalGold = Number(profile.wallet?.Gold || 0);
  const grantedWeapons = flattenGrantedWeapons(successful);
  const profileWeapons = [...new Set(profile.profile?.weapons?.unlocked_weapon_ids || [])].sort();

  return {
    scenario: "same-player-concurrent",
    playerId,
    requests: SAME_PLAYER_REQUESTS,
    concurrency: SAME_PLAYER_CONCURRENCY,
    successes: successful.length,
    failures: failures.length,
    failureSamples: failures.slice(0, 5),
    latency: summarizeLatencies(successful.map(entry => entry.latencyMs)),
    checks: {
      totalPullsExpected: expectedPulls,
      totalPullsActual: Number(profile.gacha?.totalPullsLifetime || 0),
      goldExpected: expectedGold,
      goldActual: finalGold,
      grantedWeapons,
      profileWeapons,
      profileWeaponsMatchGranted: JSON.stringify(grantedWeapons) === JSON.stringify(profileWeapons)
    }
  };
}

async function runMultiPlayerScenario() {
  const playerIds = Array.from({ length: MULTI_PLAYER_COUNT }, () => `stress-multi-${crypto.randomUUID()}`);
  await Promise.all(playerIds.map(playerId => initPlayer(playerId)));

  const tasks = [];
  for (const playerId of playerIds) {
    for (let i = 0; i < MULTI_PLAYER_REQUESTS; i += 1) {
      tasks.push(playerId);
    }
  }

  const failures = [];
  const results = await runPool(tasks, MULTI_PLAYER_CONCURRENCY, async playerId => {
    try {
      const pullResult = await pull(playerId);
      return {
        playerId,
        ...pullResult
      };
    } catch (error) {
      failures.push(error.message);
      return null;
    }
  });

  const successful = results.filter(Boolean);
  const bannerCost = COST_BY_BANNER[BANNER_ID][PULL_COUNT === 10 ? "ten" : "single"];
  const profileSummaries = [];

  for (const playerId of playerIds) {
    const profile = await fetchProfile(playerId);
    const playerResults = successful.filter(entry => entry.playerId === playerId);
    profileSummaries.push({
      playerId,
      expectedPulls: playerResults.length * PULL_COUNT,
      actualPulls: Number(profile.gacha?.totalPullsLifetime || 0),
      expectedGold: buildGoldExpectation(SEED_GOLD, playerResults, bannerCost),
      actualGold: Number(profile.wallet?.Gold || 0)
    });
  }

  const mismatches = profileSummaries.filter(summary =>
    summary.expectedPulls !== summary.actualPulls || summary.expectedGold !== summary.actualGold
  );

  return {
    scenario: "multi-player-concurrent",
    players: MULTI_PLAYER_COUNT,
    requestsPerPlayer: MULTI_PLAYER_REQUESTS,
    totalRequests: tasks.length,
    concurrency: MULTI_PLAYER_CONCURRENCY,
    successes: successful.length,
    failures: failures.length,
    failureSamples: failures.slice(0, 5),
    latency: summarizeLatencies(successful.map(entry => entry.latencyMs)),
    mismatchedPlayers: mismatches.slice(0, 10),
    mismatchCount: mismatches.length
  };
}

async function main() {
  assertNumber(PULL_COUNT, "PULL_COUNT");
  assertNumber(SEED_GOLD, "SEED_GOLD");
  assertNumber(SEED_ORBS, "SEED_ORBS");

  if (!COST_BY_BANNER[BANNER_ID]) {
    throw new Error(`Unsupported banner id: ${BANNER_ID}`);
  }

  if (![1, 10].includes(PULL_COUNT)) {
    throw new Error(`PULL_COUNT must be 1 or 10. Received ${PULL_COUNT}`);
  }

  const health = await requestJson("/health");
  const startedAt = Date.now();
  const samePlayer = await runSamePlayerScenario();
  const multiPlayer = await runMultiPlayerScenario();
  const finishedAt = Date.now();

  const summary = {
    ok: true,
    service: health.service,
    baseUrl: BASE_URL,
    bannerId: BANNER_ID,
    pullCount: PULL_COUNT,
    durationMs: finishedAt - startedAt,
    samePlayer,
    multiPlayer
  };

  console.log(JSON.stringify(summary, null, 2));
}

main().catch(error => {
  console.error(JSON.stringify({
    ok: false,
    error: error.message,
    stack: error.stack
  }, null, 2));
  process.exitCode = 1;
});
