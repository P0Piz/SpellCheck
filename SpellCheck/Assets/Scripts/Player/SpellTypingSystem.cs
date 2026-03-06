using UnityEngine;
using TMPro;

public class SpellTypingSystem : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField inputField;

    [Header("Spell Data")]
    public SpellDatabaseSO database;

    [Header("Where spells spawn from")]
    public Transform spawnPoint;

    string lastValidText = "";

    void Awake()
    {
        inputField.contentType = TMP_InputField.ContentType.Custom;
        inputField.characterValidation = TMP_InputField.CharacterValidation.None;
        inputField.onValidateInput += ValidateInput;

        inputField.richText = false;
    }

    private char ValidateInput(string text, int charIndex, char addedChar)
    {
        if (char.IsLetter(addedChar))
            return addedChar;

        if (addedChar == ' ')
        {
            if (text.Length == 0) return '\0';
            if (text.Length > 0 && text[text.Length - 1] == ' ') return '\0';
            return addedChar;
        }

        return '\0';
    }

    private string ValidateCommand(string text, int charIndex, char addedChar, int command)
    {
        const int Paste = 1;

        if (command == Paste)
            return null;

        return text;
    }

    void Update()
    {
        if (!inputField.isFocused)
            ForceFocus();

        // BLOCK PASTE SHORTCUTS
        if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) &&
            Input.GetKeyDown(KeyCode.V))
        {
            return; // ignore paste
        }

        if ((Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)) &&
            Input.GetKeyDown(KeyCode.V))
        {
            return; // mac paste
        }

        if (Input.GetKeyDown(KeyCode.Insert) && Input.GetKey(KeyCode.LeftShift))
        {
            return; // Shift + Insert paste
        }

        if (Input.GetKeyDown(KeyCode.Return))
            AttemptSpell();
    }

    void LateUpdate()
    {
        // If text suddenly jumps more than 1 character, assume paste
        if (inputField.text.Length > lastValidText.Length + 1)
        {
            inputField.text = lastValidText;
            inputField.caretPosition = inputField.text.Length;
        }

        lastValidText = inputField.text;
    }

    void AttemptSpell()
    {
        string typed = inputField.text;

        SpellDefinition spell = database ? database.GetSpell(typed) : null;

        if (spell == null)
        {
            Debug.Log("Invalid Spell: " + typed);
            ClearAndRefocus();
            return;
        }

        Debug.Log("Spell Cast: " + spell.spellName);

        if (spell.healAmount > 0)
        {
            PlayerHealth player = FindObjectOfType<PlayerHealth>();
            if (player != null)
            {
                player.Heal(spell.healAmount);
            }
        }

        if (spell.spawnPrefab != null && spawnPoint != null)
        {
            Vector3 pos = spawnPoint.TransformPoint(spell.spawnOffset);

            GameObject spawned = Instantiate(spell.spawnPrefab, pos, spawnPoint.rotation);

            if (spell.parentToSpawnPoint)
                spawned.transform.SetParent(spawnPoint, true);
        }

        ClearAndRefocus();
    }

    void ClearAndRefocus()
    {
        inputField.text = "";
        ForceFocus();
    }

    void ForceFocus()
    {
        inputField.Select();
        inputField.ActivateInputField();
        inputField.caretPosition = inputField.text.Length;
    }
}