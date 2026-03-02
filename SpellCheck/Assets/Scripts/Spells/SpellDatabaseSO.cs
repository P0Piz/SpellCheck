using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Spell Database")]
public class SpellDatabaseSO : ScriptableObject
{
    public List<SpellDefinition> spells = new List<SpellDefinition>();

    // Find a spell by typed text. Returns null if not found.
    public SpellDefinition GetSpell(string typed)
    {
        if (string.IsNullOrWhiteSpace(typed)) return null;

        string key = typed.ToLower().Trim();

        for (int i = 0; i < spells.Count; i++)
        {
            if (!spells[i]) continue;
            if (spells[i].spellName.ToLower().Trim() == key)
                return spells[i];
        }

        return null;
    }
}