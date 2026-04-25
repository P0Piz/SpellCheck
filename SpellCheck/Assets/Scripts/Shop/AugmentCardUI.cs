using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AugmentCardUI : MonoBehaviour
{
    [Header("UI")]
    public Image augmentImage;
    public TMP_Text costText;
    public Button buyButton;

    private AugmentData currentAugment;
    private AugmentShopManager currentShop;

    public void Setup(AugmentData augment, AugmentShopManager shop)
    {
        currentAugment = augment;
        currentShop = shop;

        if (augmentImage != null)
            augmentImage.sprite = augment.augmentImage;

        if (costText != null)
            costText.text = augment.cost.ToString();

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(BuyThis);
        }
    }

    void BuyThis()
    {
        if (currentShop != null && currentAugment != null)
            currentShop.TryBuyAugment(currentAugment);
    }
}