using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(menuName = "Eclipside/Effects/Global Tempo On Kill")]
public class WeaponGlobalTempoOnKillEffect : WeaponEffect
{
    public float slowPerKill = 0.005f;
    public float minTempoMultiplier = 0.75f;
    public int resetEveryWaveCount = 5;

    private PlayerController owner;
    private GameDirector boundDirector;
    private int waveCounter;
    private int killCounter;

    public override void OnEquip(PlayerController player)
    {
        EnemyBase.OnEnemyKilled -= HandleEnemyKilled;
        SceneManager.activeSceneChanged -= HandleSceneChanged;
        UnbindDirector();
        owner = player;
        killCounter = 0;
        waveCounter = 0;
        EnemyBase.OnEnemyKilled += HandleEnemyKilled;
        SceneManager.activeSceneChanged += HandleSceneChanged;
        BindDirector();
        ApplyTempo();
    }

    public override void OnUnequip(PlayerController player)
    {
        EnemyBase.OnEnemyKilled -= HandleEnemyKilled;
        SceneManager.activeSceneChanged -= HandleSceneChanged;
        UnbindDirector();
        EnemyBase.ResetGlobalTempoMultipliers();

        if (owner == player)
        {
            owner = null;
            killCounter = 0;
            waveCounter = 0;
        }
    }

    private void HandleEnemyKilled(EnemyBase enemy)
    {
        if (owner == null)
        {
            return;
        }

        killCounter++;
        ApplyTempo();
    }

    private void HandleSceneChanged(Scene previousScene, Scene nextScene)
    {
        killCounter = 0;
        waveCounter = 0;
        BindDirector();
        ApplyTempo();
    }

    private void HandleWaveAdvanced()
    {
        waveCounter++;
        if (resetEveryWaveCount > 0 && waveCounter >= resetEveryWaveCount)
        {
            waveCounter = 0;
            killCounter = 0;
            ApplyTempo();
        }
    }

    private void ApplyTempo()
    {
        float multiplier = Mathf.Clamp(1f - (killCounter * slowPerKill), minTempoMultiplier, 1f);
        EnemyBase.SetGlobalTempoMultipliers(multiplier, multiplier);
    }

    private void BindDirector()
    {
        UnbindDirector();
        boundDirector = GameDirector.Instance;
        if (boundDirector != null)
        {
            boundDirector.OnWaveAdvanced += HandleWaveAdvanced;
        }
    }

    private void UnbindDirector()
    {
        if (boundDirector != null)
        {
            boundDirector.OnWaveAdvanced -= HandleWaveAdvanced;
            boundDirector = null;
        }
    }
}
