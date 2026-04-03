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

    private bool shopOpen = false;

    public bool IsShopOpen => shopOpen;

    public void OpenShop()
    {
        RollOffers();
        RefreshUI();

        if (shopPanel != null)
            shopPanel.SetActive(true);

        shopOpen = true;

        Debug.Log("Augment shop opened.");
    }

    public void CloseShop()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);

        shopOpen = false;

        Debug.Log("Augment shop closed.");
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

        if (playerManager.currentScore < augment.cost)
        {
            Debug.Log("Not enough score to buy " + augment.augmentName);
            return;
        }

        playerManager.MinusScore(augment.cost);
        augmentManager.ApplyAugment(augment);

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
}