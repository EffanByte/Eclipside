using UnityEngine;

public class DustyMeteorite : MonoBehaviour, IInteractable
{
    [SerializeField] private MeteoriteBanner dustyBanner;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Interact(PlayerController player)
    {
        GachaManager.Instance.PerformPull(dustyBanner, false);
    }

    public string GetInteractionPrompt()
    {
        return "Use Meteorite";
    }
}
