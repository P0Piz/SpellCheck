using System.Collections.Generic;
using UnityEngine;

public class SpellDatabase : MonoBehaviour
{
    [Header("Valid Spells")]
    //This is just the list of spells that are allowed to be cast
    public List<string> validSpells = new List<string>()
    {
        "fireball",
        "earthshatter",
        "seismic",
        "stonewall",
        "quake"
    };

    // Checks if the typed spell exists in the list returns true if it's valid, false if not
    public bool IsValidSpell(string spellName)
    {
        // Make sure we compare clean lowercase values so casing doesn't matter
        return validSpells.Contains(spellName.ToLower().Trim());
    }
}