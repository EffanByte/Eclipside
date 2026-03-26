using UnityEngine;

public static class RunSceneTransitionState
{
    public static bool HasActiveRun { get; private set; }
    public static int CurrentBiomeIndex { get; private set; }
    public static float CurrentDifficultyValue { get; private set; } = 1f;

    public static void BeginNewRun()
    {
        HasActiveRun = true;
        CurrentBiomeIndex = 0;
        CurrentDifficultyValue = 1f;
    }

    public static void SetBiomeState(int biomeIndex, float difficultyValue)
    {
        HasActiveRun = true;
        CurrentBiomeIndex = Mathf.Max(0, biomeIndex);
        CurrentDifficultyValue = Mathf.Max(1f, difficultyValue);
    }

    public static void Clear()
    {
        HasActiveRun = false;
        CurrentBiomeIndex = 0;
        CurrentDifficultyValue = 1f;
    }
}
