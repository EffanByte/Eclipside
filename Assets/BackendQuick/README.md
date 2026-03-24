# Eclipside Quick Backend

Minimal local backend for fast testing of:

- server time sync
- deterministic mission assignment
- mission progress tracking
- mission claiming
- mission reroll
- gacha pulls with pity and wallet updates

It is intentionally simple:

- no auth
- no database
- no external dependencies
- JSON file persistence

## Run

```bash
cd D:\AstroApe\Eclipside\Assets\BackendQuick
npm start
```

The server starts on `http://localhost:8080`.

## Test Flow

1. Create or load a player:

```bash
curl -X POST http://localhost:8080/player/init ^
  -H "Content-Type: application/json" ^
  -d "{\"playerId\":\"test-player\"}"
```

2. Get server time:

```bash
curl "http://localhost:8080/time-sync?playerId=test-player"
```

3. Get current mission state:

```bash
curl "http://localhost:8080/missions/state?playerId=test-player"
```

4. Push mission progress:

```bash
curl -X POST http://localhost:8080/missions/progress ^
  -H "Content-Type: application/json" ^
  -d "{\"playerId\":\"test-player\",\"statKey\":\"KILLS_REGULAR\",\"amount\":120}"
```

5. Claim a completed mission:

```bash
curl -X POST http://localhost:8080/missions/claim ^
  -H "Content-Type: application/json" ^
  -d "{\"playerId\":\"test-player\",\"missionId\":\"daily_easy_kill_120\"}"
```

6. Do a gacha pull:

```bash
curl -X POST http://localhost:8080/gacha/pull ^
  -H "Content-Type: application/json" ^
  -d "{\"playerId\":\"test-player\",\"bannerId\":\"arcane_meteorite\",\"pullCount\":1}"
```

## Notes

- Daily reset uses `00:00 UTC`.
- Weekly reset uses `Monday 00:00 UTC`.
- Missions are assigned deterministically per player and reset cycle.
- Progress is stored server-side in this prototype so claiming can be validated without trusting the client for completion state.
- All state persists to `data/store.json`.
