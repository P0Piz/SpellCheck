using UnityEngine;

public class NextSpellModifierSelector : MonoBehaviour
{
    public enum SpellModifierType
    {
        None = 0,
        Unstable = 1,
        Greater = 2,
        Frozen = 3,
        Chained = 4,
        Rapid = 5,
        Delayed = 6
    }

    [Header("Current Selection")]
    [SerializeField] private SpellModifierType nextModifier = SpellModifierType.None;

    public SpellModifierType NextModifier => nextModifier;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetModifier(SpellModifierType.Unstable);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetModifier(SpellModifierType.Greater);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetModifier(SpellModifierType.Frozen);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            SetModifier(SpellModifierType.Chained);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            SetModifier(SpellModifierType.Rapid);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            SetModifier(SpellModifierType.Delayed);
        }
    }

    public void SetModifier(SpellModifierType modifier)
    {
        nextModifier = modifier;
        Debug.Log("Next spell modifier set to: " + nextModifier);
    }

    public void ClearModifier()
    {
        nextModifier = SpellModifierType.None;
    }

    public void ApplyModifierToProjectile(HomingProjectileBase projectile)
    {
        if (projectile == null)
            return;

        // Clear all first so only one is active
        projectile.unstable = false;
        projectile.greater = false;
        projectile.frozen = false;
        projectile.chained = false;
        projectile.rapid = false;
        projectile.delayed = false;

        switch (nextModifier)
        {
            case SpellModifierType.Unstable:
                projectile.unstable = true;
                break;

            case SpellModifierType.Greater:
                projectile.greater = true;
                break;

            case SpellModifierType.Frozen:
                projectile.frozen = true;
                break;

            case SpellModifierType.Chained:
                projectile.chained = true;
                break;

            case SpellModifierType.Rapid:
                projectile.rapid = true;
                break;

            case SpellModifierType.Delayed:
                projectile.delayed = true;
                break;
        }

        if (nextModifier != SpellModifierType.None)
        {
            Debug.Log("Applied modifier to spell: " + nextModifier);
        }

        // Consume it so it only affects the next cast
        ClearModifier();
    }
}