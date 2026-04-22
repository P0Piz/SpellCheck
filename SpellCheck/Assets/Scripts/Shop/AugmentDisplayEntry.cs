using TMPro;
using UnityEngine;

public class AugmentDisplayEntry : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text augmentNameText;
    public TMP_Text counterText;
    public TMP_Text multiplierText;

    public void Setup(string augmentName, string counter, string multiplier, bool showCounter, bool showMultiplier)
    {
        if (augmentNameText != null)
            augmentNameText.text = augmentName;

        if (counterText != null)
        {
            counterText.gameObject.SetActive(showCounter);
            if (showCounter)
                counterText.text = counter;
        }

        if (multiplierText != null)
        {
            multiplierText.gameObject.SetActive(showMultiplier);
            if (showMultiplier)
                multiplierText.text = multiplier;
        }
    }
}