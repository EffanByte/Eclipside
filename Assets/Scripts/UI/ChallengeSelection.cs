using UnityEngine;
using System.Collections.Generic;

public class ChallengeSelection : MonoBehaviour
{
    public static ChallengeSelection Instance { get; private set; }
    
    // The list of what the player selected
    public HashSet<ChallengeType> activeChallenges = new HashSet<ChallengeType>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public void SetChallengeState(ChallengeType type, bool isActive)
    {
        if (isActive) activeChallenges.Add(type);
        else activeChallenges.Remove(type);
    }
}