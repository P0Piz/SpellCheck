using System.Text;
using UnityEngine;
using TMPro;

public class SpellDatabaseDisplay : MonoBehaviour
{
    [Header("Database (assign ONE)")]
    public SpellDatabaseSO databaseSO;     // ScriptableObject database (SpellDefinition list)

    [Header("Optional UI Output")]
    public TMP_Text outputText;            // drag a TMP_Text here if you want it on screen

    [Header("Settings")]
    public bool printToConsoleOnStart = true;
    public bool updateEveryFrame = false;  // keep false unless you're editing spells at runtime

    void Start()
    {
        // On start, build the list once
        RefreshDisplay();
    }

    void Update()
    {
        // Only turn this on if you need live updating (usually you don't)
        if (updateEveryFrame)
            RefreshDisplay();
    }

    // Rebuilds the UI label (if assigned)
    public void RefreshDisplay()
    {
        if (!outputText) return;
        outputText.text = GetSpellListString();
    }

    // Returns a nice formatted string of spells from whichever database is assigned
    public string GetSpellListString()
    {
        var sb = new StringBuilder();
        sb.AppendLine("spell book");

        // ScriptableObject database path
        if (databaseSO != null)
        {
            if (databaseSO.spells == null || databaseSO.spells.Count == 0)
            {
                sb.AppendLine("(none)");
                return sb.ToString();
            }

            for (int i = 0; i < databaseSO.spells.Count; i++)
            {
                var spell = databaseSO.spells[i];
                if (!spell) continue;

                sb.Append($"{i + 1}. {spell.spellName}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        sb.AppendLine("No database assigned.");
        return sb.ToString();
    }

    // Handy button in inspector to test quickly
    [ContextMenu("Print Spells To Console")]
    void PrintSpellsToConsole()
    {
        Debug.Log(GetSpellListString());
    }
}