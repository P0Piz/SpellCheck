using System.Collections.Generic;
using UnityEngine;

public class PlayerAugmentManager : MonoBehaviour
{
    [Header("Refs")]
    public PlayerManager playerManager;
    public WaveSpawnerJson waveSpawner;

    [Header("Owned Augments")]
    public List<AugmentData> ownedAugments = new List<AugmentData>();

    [Header("Runtime")]
    public int successfulSpellCount = 0;
    public int correctStreak = 0;
    public bool lastChanceUsedThisWave = false;

    [Header("Wave Modifiers")]
    public float enemySpeedMultiplier = 1f;

    public bool HasAugment(string augmentID)
    {
        for (int i = 0; i < ownedAugments.Count; i++)
        {
            if (ownedAugments[i] != null && ownedAugments[i].augmentID == augmentID)
                return true;
        }

        return false;
    }

    public void ApplyAugment(AugmentData augment)
    {
        if (augment == null)
            return;

        if (HasAugment(augment.augmentID))
            return;

        ownedAugments.Add(augment);

        if (augment.augmentID == "health_pot" && playerManager != null)
            playerManager.AddTemporaryHeart(1);

        if (augment.augmentID == "snails_pace")
            enemySpeedMultiplier *= 0.8f;
    }

    public void OnWaveStarted()
    {
        lastChanceUsedThisWave = false;
    }

    public void OnSuccessfulSpellCast()
    {
        successfulSpellCount++;
        correctStreak++;

        if (HasAugment("echo_heal") && successfulSpellCount % 5 == 0 && playerManager != null)
            playerManager.Heal(1);

        if (HasAugment("you_shall_not_pass") && successfulSpellCount % 10 == 0 && waveSpawner != null)
            waveSpawner.StunAllLivingEnemies(2f);
    }

    public void OnSpellMistake()
    {
        correctStreak = 0;

        if (HasAugment("glass_cannon") && playerManager != null)
            playerManager.TakeDamage(1);
    }

    public bool CanUseLastChance()
    {
        return HasAugment("last_chance") && !lastChanceUsedThisWave;
    }

    public void ConsumeLastChance()
    {
        lastChanceUsedThisWave = true;
    }

    public float GetProjectileSpeedBonus()
    {
        float bonus = 0f;

        if (HasAugment("flow_state"))
            bonus += correctStreak * 1.5f;

        if (HasAugment("glass_cannon"))
            bonus += 8f;

        return bonus;
    }

    public bool ShouldChainThisShot()
    {
        return HasAugment("chain") && successfulSpellCount > 0 && successfulSpellCount % 3 == 0;
    }

    public bool ShouldPierceThisShot()
    {
        return HasAugment("pierce");
    }

    public bool ShouldSplitThisShot()
    {
        return HasAugment("split");
    }
}