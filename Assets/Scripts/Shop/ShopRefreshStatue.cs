using UnityEngine;
using TMPro;

public class ShopRefreshStatue : MonoBehaviour
{
    [SerializeField] private TextMeshPro costText;
    private bool playerInRange = false;

    public void UpdateCost()
    {
        if (ShopManager.Instance == null) return;

        int cost = ShopManager.Instance.GetRefreshCost();
        if (costText != null) costText.text = $"Reroll: {cost}";
    }

    private void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (GameDirector.Instance != null && GameDirector.Instance.IsWaveActive)
            {
                return;
            }

            ShopManager.Instance.TryRefreshShop();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player")) playerInRange = false;
    }
}