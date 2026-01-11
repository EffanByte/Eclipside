using UnityEngine;
using System;

[RequireComponent(typeof(WaveManager))]
public class GameDirector : MonoBehaviour
{
    public static GameDirector Instance { get; private set; }

    [Header("Progression Settings")]
    [SerializeField] private float timeBetweenWaves = 30f;
    [SerializeField] private float difficultyScaling = 0.1f; // +10% per wave

    public bool IsWaveActive { get; private set; } = false;

    public event Action<bool> OnCombatStateChanged; 


    // --- State ---
    public bool IsPaused { get; private set; } = false; // Is player in Shop?
    public int CurrentWave { get; private set; } = 1;
    public float CurrentDifficulty { get; private set; } = 1.0f;
    
    private float waveTimer;
    private WaveManager waveManager;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        waveManager = GetComponent<WaveManager>();
    }

    private void Start()
    {
        waveTimer = timeBetweenWaves;
        
        // Start Wave 1 immediately
        waveManager.TriggerWave(CurrentWave, CurrentDifficulty);
    }

    private void Update()
    {
        // GDD: "Next wave timer will stop until player leaves shop zone"
        if (IsPaused) return;

        // Countdown
        waveTimer -= Time.deltaTime;

        if (waveTimer <= 0)
        {
            AdvanceWave();
        }
    }


    private void AdvanceWave()
    {
        // 1. Reset Timer
        waveTimer = timeBetweenWaves;

        // 2. Increase Progression
        CurrentWave++;
        CurrentDifficulty += difficultyScaling;

        // 3. Command the WaveManager
        waveManager.TriggerWave(CurrentWave, CurrentDifficulty);
    }

        public void NotifyWaveStarted()
    {
        IsWaveActive = true;
        // UI: "Wave Started! Shop Closed!"
    }

        public void NotifyWaveFinished()
    {
        IsWaveActive = false;
        // UI: "Wave Complete! Shop Open!"
    }

    // ---------------------------------------------------------
    // PUBLIC API (For ShopSafeZone.cs)
    // ---------------------------------------------------------
    public void SetSafeZoneState(bool isSafe)
    {
        IsPaused = isSafe;
        Debug.Log(isSafe ? "Timer PAUSED (Shop)" : "Timer RESUMED (Combat)");
    }
    
    // UI Helpers
    public float GetTimer() => waveTimer;
}