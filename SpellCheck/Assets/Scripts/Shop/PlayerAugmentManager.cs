using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerAugmentManager : MonoBehaviour
{
    public event Action OnAugmentsChanged;

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

        NotifyAugmentsChanged();
    }

    public void OnWaveStarted()
    {
        lastChanceUsedThisWave = false;
        NotifyAugmentsChanged();
    }

    public void OnSuccessfulSpellCast()
    {
        successfulSpellCount++;
        correctStreak++;

        if (HasAugment("echo_heal") && successfulSpellCount % 5 == 0 && playerManager != null)
            playerManager.Heal(1);

        if (HasAugment("you_shall_not_pass") && successfulSpellCount % 10 == 0 && waveSpawner != null)
            waveSpawner.StunAllLivingEnemies(2f);

        NotifyAugmentsChanged();
    }

    public void OnSpellMistake()
    {
        correctStreak = 0;

        if (HasAugment("glass_cannon") && playerManager != null)
            playerManager.TakeDamage(1);

        NotifyAugmentsChanged();
    }

    public bool CanUseLastChance()
    {
        return HasAugment("last_chance") && !lastChanceUsedThisWave;
    }

    public void ConsumeLastChance()
    {
        lastChanceUsedThisWave = true;
        NotifyAugmentsChanged();
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

    public string GetAugmentDisplayName(AugmentData augment)
    {
        if (augment == null)
            return "";

        return augment.augmentName;
    }

    public bool TryGetAugmentCounterText(string augmentID, out string counterText)
    {
        counterText = "";

        if (!HasAugment(augmentID))
            return false;

        switch (augmentID)
        {
            case "echo_heal":
                counterText = (successfulSpellCount % 5) + "/5";
                return true;

            case "you_shall_not_pass":
                counterText = (successfulSpellCount % 10) + "/10";
                return true;

            case "chain":
                counterText = (successfulSpellCount % 3) + "/3";
                return true;

            case "flow_state":
                counterText = "Streak: " + correctStreak;
                return true;

            default:
                return false;
        }
    }

    public bool TryGetAugmentMultiplierText(string augmentID, out string multiplierText)
    {
        multiplierText = "";

        if (!HasAugment(augmentID))
            return false;

        switch (augmentID)
        {
            case "flow_state":
                multiplierText = "+" + (correctStreak * 1.5f).ToString("0.0");
                return true;

            case "glass_cannon":
                multiplierText = "+8";
                return true;

            case "snails_pace":
                multiplierText = "x" + enemySpeedMultiplier.ToString("0.00");
                return true;

            default:
                return false;
        }
    }

    private void NotifyAugmentsChanged()
    {
        OnAugmentsChanged?.Invoke();
    }
}