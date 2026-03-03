using UnityEngine;
using TMPro;

public class SpellTypingSystem : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField inputField;

    [Header("Spell Data")]
    public SpellDatabaseSO database;

    [Header("Where spells spawn from")]
    public Transform spawnPoint; // player hand, staff tip, center, ground marker, etc.

    void Start()
    {
        inputField.ActivateInputField();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
            AttemptSpell();
    }

    void AttemptSpell()
    {
        string typed = inputField.text;

        // 1) Look up the spell
        SpellDefinition spell = database ? database.GetSpell(typed) : null;

        if (spell == null)
        {
            Debug.Log("Invalid Spell: " + typed);
            ClearAndRefocus();
            return;
        }

        Debug.Log("Spell Cast: " + spell.spellName);

        // 2) Spawn prefab if this spell has one
        if (spell.spawnPrefab != null && spawnPoint != null)
        {
            // Spawn position (still using offset relative to spawn point)
            Vector3 pos = spawnPoint.TransformPoint(spell.spawnOffset);

            // ALWAYS use the prefab's saved rotation
            GameObject spawned = Instantiate(
                spell.spawnPrefab,
                pos,
                spell.spawnPrefab.transform.rotation
            );

            if (spell.parentToSpawnPoint)
                spawned.transform.SetParent(spawnPoint, true);
        }
        else
        {
            Debug.Log("(No prefab assigned for this spell)");
        }

        ClearAndRefocus();
    }

    void ClearAndRefocus()
    {
        inputField.text = "";
        inputField.ActivateInputField();
    }
}