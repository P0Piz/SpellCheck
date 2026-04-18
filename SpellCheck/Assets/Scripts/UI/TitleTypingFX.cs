using System.Collections;
using TMPro;
using UnityEngine;

public class TitleTypingFX : MonoBehaviour
{
    [Header("Reference")]
    public TMP_Text titleText;

    [Header("Words")]
    public string correctPart = "Spell";
    public string wrongPart = "Chek";
    public string fixedPart = "Check";

    [Header("Timing")]
    public float typeSpeed = 0.08f;
    public float deleteSpeed = 0.05f;
    public float pauseAfterMistake = 0.4f;

    private void Start()
    {
        StartCoroutine(PlayAnimation());
    }

    private IEnumerator PlayAnimation()
    {
        titleText.text = "";

        // TYPE CORRECT PART
        for (int i = 0; i < correctPart.Length; i++)
        {
            titleText.text = correctPart.Substring(0, i + 1);
            yield return new WaitForSeconds(typeSpeed);
        }

        // TYPE WRONG PART
        for (int i = 0; i < wrongPart.Length; i++)
        {
            titleText.text = correctPart + wrongPart.Substring(0, i + 1);
            yield return new WaitForSeconds(typeSpeed);
        }

        yield return new WaitForSeconds(pauseAfterMistake);

        // BACKSPACE WRONG PART
        for (int i = wrongPart.Length; i > 0; i--)
        {
            titleText.text = correctPart + wrongPart.Substring(0, i - 1);
            yield return new WaitForSeconds(deleteSpeed);
        }

        // TYPE CORRECT VERSION
        for (int i = 0; i < fixedPart.Length; i++)
        {
            titleText.text = correctPart + fixedPart.Substring(0, i + 1);
            yield return new WaitForSeconds(typeSpeed);
        }
    }
}