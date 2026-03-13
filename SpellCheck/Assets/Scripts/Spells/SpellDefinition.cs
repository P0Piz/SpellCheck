using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Spell Definition")]
public class SpellDefinition : ScriptableObject
{
    [Header("ID")]
    public string spellName;

    [Header("Description")]
    [TextArea]
    public string description;

    [Header("Spell Info")]
    public Elements.elements element = Elements.elements.Null;

    [Header("Spawn")]
    public GameObject spawnPrefab;
    public Vector3 spawnOffset;
    public bool parentToSpawnPoint = false;

    [Header("Healing")]
    public int healAmount;
}