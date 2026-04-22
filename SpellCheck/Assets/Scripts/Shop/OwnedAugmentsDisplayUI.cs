using System.Collections.Generic;
using UnityEngine;

public class OwnedAugmentsDisplayUI : MonoBehaviour
{
    [Header("Refs")]
    public PlayerAugmentManager augmentManager;

    [Header("UI")]
    public Transform contentParent;
    public AugmentDisplayEntry entryPrefab;

    [Header("Refresh")]
    public bool refreshEveryFrame = true;

    private readonly List<AugmentDisplayEntry> spawnedEntries = new List<AugmentDisplayEntry>();

    private void Start()
    {
        RefreshDisplay();
    }

    private void Update()
    {
        if (refreshEveryFrame)
            RefreshDisplay();
    }

    public void RefreshDisplay()
    {
        if (augmentManager == null || contentParent == null || entryPrefab == null)
            return;

        ClearEntries();

        for (int i = 0; i < augmentManager.ownedAugments.Count; i++)
        {
            AugmentData augment = augmentManager.ownedAugments[i];
            if (augment == null)
                continue;

            AugmentDisplayEntry newEntry = Instantiate(entryPrefab, contentParent);
            spawnedEntries.Add(newEntry);

            string displayName = augmentManager.GetAugmentDisplayName(augment);

            string counterText;
            bool showCounter = augmentManager.TryGetAugmentCounterText(augment.augmentID, out counterText);

            string multiplierText;
            bool showMultiplier = augmentManager.TryGetAugmentMultiplierText(augment.augmentID, out multiplierText);

            newEntry.Setup(displayName, counterText, multiplierText, showCounter, showMultiplier);
        }
    }

    private void ClearEntries()
    {
        for (int i = 0; i < spawnedEntries.Count; i++)
        {
            if (spawnedEntries[i] != null)
                Destroy(spawnedEntries[i].gameObject);
        }

        spawnedEntries.Clear();
    }
}