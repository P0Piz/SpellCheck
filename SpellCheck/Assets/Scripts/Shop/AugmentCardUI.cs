using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AugmentCardUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text titleText;
    public TMP_Text descText;
    public TMP_Text costText;
    public Button buyButton;

    private AugmentData currentAugment;
    private AugmentShopManager currentShop;

    public void Setup(AugmentData augment, AugmentShopManager shop)
    {
        currentAugment = augment;
        currentShop = shop;

        if (titleText != null)
            titleText.text = augment.augmentName;

        if (descText != null)
            descText.text = augment.description;

        if (costText != null)
            costText.text = "Cost: " + augment.cost;

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