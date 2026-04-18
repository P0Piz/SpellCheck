using System.Collections.Generic;
using UnityEngine;

public class AugmentShopManager : MonoBehaviour
{
    [Header("Refs")]
    public PlayerManager playerManager;
    public PlayerAugmentManager augmentManager;
    public WaveSpawnerJson waveSpawner;

    [Header("Pool")]
    public List<AugmentData> allAugments = new List<AugmentData>();
    public List<AugmentData> currentOffers = new List<AugmentData>();

    [Header("UI")]
    public GameObject shopPanel;
    public AugmentCardUI[] cards;

    [Header("Settings")]
    public int offersPerShop = 3;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip successSfx;
    public AudioClip failSfx;
    [Range(0f, 1f)] public float successVolume = 1f;
    [Range(0f, 1f)] public float failVolume = 1f;

    private bool shopOpen = false;

    public bool IsShopOpen => shopOpen;

    public void OpenShop()
    {
        RollOffers();
        RefreshUI();

        if (shopPanel != null)
            shopPanel.SetActive(true);

        shopOpen = true;
    }

    public void CloseShop()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);

        shopOpen = false;
    }

    void RollOffers()
    {
        currentOffers.Clear();

        List<AugmentData> pool = new List<AugmentData>(allAugments);

        if (augmentManager != null)
        {
            pool.RemoveAll(a => a == null || augmentManager.HasAugment(a.augmentID));
        }

        for (int i = 0; i < offersPerShop && pool.Count > 0; i++)
        {
            int index = Random.Range(0, pool.Count);
            currentOffers.Add(pool[index]);
            pool.RemoveAt(index);
        }
    }

    void RefreshUI()
    {
        if (cards == null)
            return;

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null)
                continue;

            if (i < currentOffers.Count)
            {
                cards[i].gameObject.SetActive(true);
                cards[i].Setup(currentOffers[i], this);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }
    }

    public void TryBuyAugment(AugmentData augment)
    {
        if (augment == null || playerManager == null || augmentManager == null)
            return;

        // ? Not enough score
        if (playerManager.currentScore < augment.cost)
        {
            Debug.Log("Not enough score to buy " + augment.augmentName);
            PlayFailSound();
            return;
        }

        // ? Successful purchase
        playerManager.MinusScore(augment.cost);
        augmentManager.ApplyAugment(augment);

        PlaySuccessSound();

        CloseShop();

        if (waveSpawner != null)
            waveSpawner.ShowReadyForNextWave();
    }

    public void SkipShop()
    {
        CloseShop();

        if (waveSpawner != null)
            waveSpawner.ShowReadyForNextWave();
    }

    void PlaySuccessSound()
    {
        if (audioSource != null && successSfx != null)
            audioSource.PlayOneShot(successSfx, successVolume);
    }

    void PlayFailSound()
    {
        if (audioSource != null && failSfx != null)
            audioSource.PlayOneShot(failSfx, failVolume);
    }
}