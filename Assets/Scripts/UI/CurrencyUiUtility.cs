using UnityEngine;
using UnityEngine.UI;

public static class CurrencyUiUtility
{
    private static CurrencySpriteCatalog cachedCatalog;

    private static CurrencySpriteCatalog Catalog
    {
        get
        {
            if (cachedCatalog == null)
            {
                cachedCatalog = CurrencySpriteCatalog.Load();
            }

            return cachedCatalog;
        }
    }

    public static string FormatAmount(int amount)
    {
        return amount.ToString("N0");
    }

    public static Sprite GetSprite(CurrencyType type)
    {
        return Catalog != null ? Catalog.GetSprite(type) : null;
    }

    public static void ApplyCurrencySprite(Image image, CurrencyType type, bool hideWhenMissing = true)
    {
        ApplySprite(image, GetSprite(type), hideWhenMissing);
    }

    public static void ApplySprite(Image image, Sprite sprite, bool hideWhenMissing = true)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = sprite;
        image.preserveAspect = true;

        bool hasSprite = sprite != null;
        image.enabled = hasSprite || !hideWhenMissing;

        if (image.gameObject != null)
        {
            image.gameObject.SetActive(hasSprite || !hideWhenMissing);
        }
    }
}
