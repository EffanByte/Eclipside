using UnityEngine;

[CreateAssetMenu(menuName = "Eclipside/UI/Currency Sprite Catalog")]
public class CurrencySpriteCatalog : ScriptableObject
{
    [SerializeField] private Sprite goldSprite;
    [SerializeField] private Sprite orbSprite;

    public Sprite GetSprite(CurrencyType type)
    {
        switch (type)
        {
            case CurrencyType.Gold:
                return goldSprite;
            case CurrencyType.Orb:
                return orbSprite;
            default:
                return null;
        }
    }

    public static CurrencySpriteCatalog Load()
    {
        return Resources.Load<CurrencySpriteCatalog>("CurrencySpriteCatalog");
    }
}
