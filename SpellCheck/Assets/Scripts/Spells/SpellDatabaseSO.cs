using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Spell Database")]
public class SpellDatabaseSO : ScriptableObject
{
    public List<SpellDefinition> spells = new List<SpellDefinition>();

    public SpellDefinition GetSpell(string typed)
    {
        if (string.IsNullOrWhiteSpace(typed))
            return null;

        string key = typed.ToLower().Trim();

        for (int i = 0; i < spells.Count; i++)
        {
            SpellDefinition spell = spells[i];
            if (spell == null) continue;

            if (spell.spellName.ToLower().Trim() == key)
                return spell;
        }

        return null;
    }

    public List<SpellDefinition> GetSpellsByElement(Elements.elements element)
    {
        List<SpellDefinition> results = new List<SpellDefinition>();

        for (int i = 0; i < spells.Count; i++)
        {
            SpellDefinition spell = spells[i];
            if (spell == null) continue;

            if (spell.element == element)
                results.Add(spell);
        }

        return results;
    }

    public SpellDefinition GetRandomSpellByElement(Elements.elements element)
    {
        List<SpellDefinition> matches = GetSpellsByElement(element);

        if (matches.Count == 0)
            return null;

        return matches[Random.Range(0, matches.Count)];
    }
}