using UnityEngine;
using TMPro;
using System.Collections;

public class EnemySpellPrompt : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text promptText;

    [Header("Position")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);

    [Header("Behaviour")]
    [SerializeField] private bool faceCamera = true;

    [Header("Colors")]
    [SerializeField] private string untypedColor = "#FFFFFF";
    [SerializeField] private string correctColor = "#00FF00";
    [SerializeField] private string incorrectColor = "#FF8800";

    [Header("Current Letter Highlight")]
    [SerializeField] private float currentLetterScale = 1.35f;

    [Header("Shake")]
    [SerializeField] private float shakeDuration = 0.12f;
    [SerializeField] private float shakeAmount = 0.08f;

    private Camera cam;
    private string currentTargetWord = "";
    private Coroutine shakeRoutine;
    private Vector3 baseOffset;

    void Awake()
    {
        cam = Camera.main;
        baseOffset = worldOffset;

        if (promptText != null)
        {
            promptText.text = "";
            promptText.gameObject.SetActive(false);
        }
    }

    void LateUpdate()
    {
        if (promptText == null)
            return;

        promptText.transform.position = transform.position + worldOffset;

        if (faceCamera)
        {
            if (cam == null)
                cam = Camera.main;

            if (cam != null)
                promptText.transform.forward = cam.transform.forward;
        }
    }

    public void ShowSpell(string spellName)
    {
        if (promptText == null)
        {
            Debug.LogWarning($"{name}: EnemySpellPrompt has no TMP_Text assigned.");
            return;
        }

        currentTargetWord = string.IsNullOrWhiteSpace(spellName) ? "" : spellName.ToLower().Trim();

        if (string.IsNullOrEmpty(currentTargetWord))
        {
            HideSpell();
            return;
        }

        promptText.gameObject.SetActive(true);
        SetTypedText("");
    }

    public void SetTypedText(string typed)
    {
        if (promptText == null)
            return;

        if (string.IsNullOrEmpty(currentTargetWord))
        {
            promptText.text = "";
            promptText.gameObject.SetActive(false);
            return;
        }

        typed = typed == null ? "" : typed.ToLower();

        string result = "";
        int currentIndex = Mathf.Clamp(typed.Length, 0, currentTargetWord.Length - 1);

        for (int i = 0; i < currentTargetWord.Length; i++)
        {
            char targetChar = currentTargetWord[i];
            string charString = targetChar.ToString();

            if (i >= typed.Length)
            {
                if (i == currentIndex)
                {
                    result += $"<size={Mathf.RoundToInt(promptText.fontSize * currentLetterScale)}><color={untypedColor}>{charString}</color></size>";
                }
                else
                {
                    result += $"<color={untypedColor}>{charString}</color>";
                }
            }
            else if (typed[i] == targetChar)
            {
                result += $"<color={correctColor}>{charString}</color>";
            }
            else
            {
                result += $"<color={incorrectColor}>{charString}</color>";
            }
        }

        promptText.text = result;
        promptText.gameObject.SetActive(true);
    }

    public void ClearTypedText()
    {
        SetTypedText("");
    }

    public void HideSpell()
    {
        currentTargetWord = "";
        worldOffset = baseOffset;

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        if (promptText == null)
            return;

        promptText.text = "";
        promptText.gameObject.SetActive(false);
    }

    public void PlayWrongShake()
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(WrongShakeRoutine());
    }

    private IEnumerator WrongShakeRoutine()
    {
        float timer = 0f;

        while (timer < shakeDuration)
        {
            timer += Time.deltaTime;

            Vector3 randomOffset = new Vector3(
                Random.Range(-shakeAmount, shakeAmount),
                Random.Range(-shakeAmount, shakeAmount),
                0f
            );

            worldOffset = baseOffset + randomOffset;
            yield return null;
        }

        worldOffset = baseOffset;
        shakeRoutine = null;
    }
}