using UnityEngine;
using TMPro;
public class UpdateCurrency : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI rupeeText;
    [SerializeField] private TextMeshProUGUI keyText;
    
    void Start()
    {
        PlayerController.Instance.onCurrencyUpdate += UpdateUI;
    }

    private void UpdateUI()
    {
        rupeeText.text = "x" + PlayerController.Instance.rupees.ToString();
        keyText.text = "x" + PlayerController.Instance.keys.ToString();
    }

    public void OnDestroy()
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.onCurrencyUpdate -= UpdateUI;
        }
    }
}
