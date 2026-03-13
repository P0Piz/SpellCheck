using UnityEngine;
using TMPro;

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

    private Camera cam;
    private string currentTargetWord = "";

    void Awake()
    {
        cam = Camera.main;

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

        for (int i = 0; i < currentTargetWord.Length; i++)
        {
            char targetChar = currentTargetWord[i];

            if (i >= typed.Length)
            {
                result += $"<color={untypedColor}>{targetChar}</color>";
            }
            else if (typed[i] == targetChar)
            {
                result += $"<color={correctColor}>{targetChar}</color>";
            }
            else
            {
                result += $"<color={incorrectColor}>{targetChar}</color>";
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

        if (promptText == null)
            return;

        promptText.text = "";
        promptText.gameObject.SetActive(false);
    }
}