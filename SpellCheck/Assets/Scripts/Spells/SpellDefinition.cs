using UnityEngine;

[CreateAssetMenu(menuName = "Spells/Spell Definition")]
public class SpellDefinition : ScriptableObject
{
    [Header("ID")]
    public string spellName; // what the player types (e.g. "fireball")

    [Header("Description")]
    public string Description; // what the spell does

    [Header("Spawn (optional)")]
    public GameObject spawnPrefab;          // leave null if this spell doesn't spawn anything
    public Vector3 spawnOffset;             // local offset from spawn point
    public bool parentToSpawnPoint = false; // useful for VFX sticking to hand/ground point

    [Header("Spawn Settings")]
    public bool rotateToSpawnPoint = true;  // if true, prefab matches spawn point rotation
}