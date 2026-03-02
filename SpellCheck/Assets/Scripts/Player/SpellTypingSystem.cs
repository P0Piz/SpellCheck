using UnityEngine;
using TMPro;

public class SpellTypingSystem : MonoBehaviour
{
    [Header("References")]
    // The input field where the player types the spell
    public TMP_InputField inputField;

    // Reference to our spell database so we can check valid spells
    public SpellDatabase spellDatabase;

    void Start()
    {
        // Auto focus the input field at the start so player can type immediately
        inputField.ActivateInputField();
    }

    void Update()
    {
        // If player presses Enter, attempt to cast whatever they typed
        if (Input.GetKeyDown(KeyCode.Return))
        {
            AttemptSpell();
        }
    }

    // This runs when we try to cast a spell
    void AttemptSpell()
    {
        // Get whatever the player typed
        string typedSpell = inputField.text;

        // Ask the database if this spell is valid
        if (spellDatabase.IsValidSpell(typedSpell))
        {
            Debug.Log("Spell Cast: " + typedSpell);
        }
        else
        {
            Debug.Log("Invalid Spell: " + typedSpell);
        }

        // Clear the input field after attempting
        inputField.text = "";

        // Re-focus it so player can immediately type again
        inputField.ActivateInputField();
    }
}