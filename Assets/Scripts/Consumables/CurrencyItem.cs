using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/Items/Currency Bundle")]
public class CurrencyItem : ItemData
{
    public CurrencyType currencyType; // Key or Rupee or XP
    public int amount = 1;            // How much you get
}