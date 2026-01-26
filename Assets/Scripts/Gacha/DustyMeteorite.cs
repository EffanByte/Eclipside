using UnityEngine;

public class MeteoriteInteract : MonoBehaviour, IInteractable
{
    [SerializeField] private MeteoriteBanner banner;

    public void Interact(PlayerController player)
    {
        GachaManager.Instance.PerformPull(banner, false);
    }

    public string GetInteractionPrompt()
    {
        return "Use Meteorite";
    }
}
