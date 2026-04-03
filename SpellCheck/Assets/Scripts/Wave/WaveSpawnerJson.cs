using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaveSpawnerJson : MonoBehaviour
{
    [Header("Wave Order (Resources/Waves/)")]
    public string[] waveFiles = { "wave1", "wave2" };
    public int startIndex = 0;

    [Header("Enemy Prefabs")]
    public GameObject EarthPrefab;
    public GameObject FirePrefab;
    public GameObject WaterPrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("UI")]
    public Button nextWaveButton;
    public TMP_Text statusText;

    [Header("End Screen")]
    public GameObject endScreenPanel;
    public TMP_Text endScreenText;

    [Header("Behaviour")]
    public bool autoStartFirstWave = false;
    public bool loopWaves = false;

    [Header("Typing / Spells")]
    public SpellTypingSystem typing;
    public SpellDatabaseSO spellDatabase;

    [Header("Closest Targeting")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool updateClosestTargetEveryFrame = true;

    [Header("Augment Shop")]
    public bool openShopBetweenWaves = true;
    public AugmentShopManager augmentShop;
    public PlayerAugmentManager augmentManager;

    public int CurrentWaveIndex => currentIndex;
    public bool IsWaveRunning => isWaveRunning;

    private int currentIndex;
    private bool isWaveRunning;

    private readonly HashSet<GameObject> living = new HashSet<GameObject>();
    private readonly HashSet<EnemyBase> completedTypingTargets = new HashSet<EnemyBase>();

    private EnemyBase activeEnemy;
    private Transform player;

    void Awake()
    {
        currentIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, waveFiles.Length - 1));
        FindPlayer();
        SetButtonInteractable(true);
        SetStatus("Ready");
        HideEndScreen();
    }

    void Start()
    {
        if (autoStartFirstWave)
            StartNextWave();
    }

    void Update()
    {
        if (typing != null)
            typing.enabled = isWaveRunning;

        if (!isWaveRunning)
            return;

        if (player == null)
            FindPlayer();

        if (updateClosestTargetEveryFrame)
            RefreshClosestActiveEnemy();
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        player = playerObj != null ? playerObj.transform : null;
    }

    public EnemyBase GetActiveEnemy()
    {
        if (!isWaveRunning)
            return null;

        if (player == null)
            FindPlayer();

        CleanupCollections();
        RefreshClosestActiveEnemy();

        return activeEnemy;
    }

    public void StartNextWaveButton()
    {
        StartNextWave();
    }

    public void StartNextWave()
    {
        if (isWaveRunning)
            return;

        if (augmentShop != null && augmentShop.IsShopOpen)
            return;

        if (waveFiles == null || waveFiles.Length == 0)
        {
            SetStatus("No waves set");
            return;
        }

        if (currentIndex >= waveFiles.Length)
        {
            if (loopWaves)
            {
                currentIndex = 0;
                HideEndScreen();
            }
            else
            {
                SetStatus("All waves complete");
                SetButtonInteractable(false);
                ShowEndScreen("You Win!\nAll rounds complete.");
                return;
            }
        }

        if (augmentManager != null)
            augmentManager.OnWaveStarted();

        HideEndScreen();
        StartCoroutine(RunWaveRoutine(waveFiles[currentIndex]));
    }

    IEnumerator RunWaveRoutine(string fileName)
    {
        isWaveRunning = true;
        SetButtonInteractable(false);

        WaveJsonData wave = LoadWave(fileName);
        if (wave == null)
        {
            SetStatus($"Missing wave: {fileName}");
            isWaveRunning = false;
            SetButtonInteractable(true);
            yield break;
        }

        CleanupCollections();
        completedTypingTargets.Clear();
        ClearActiveEnemy();

        SetStatus(wave.waveName);

        if (wave.runGroupsSequentially)
        {
            foreach (WaveGroupJson g in wave.groups)
            {
                yield return new WaitForSeconds(Mathf.Max(0f, g.startDelay));
                yield return SpawnGroup(g);
            }
        }
        else
        {
            List<Coroutine> routines = new List<Coroutine>();

            foreach (WaveGroupJson g in wave.groups)
                routines.Add(StartCoroutine(SpawnGroupWithDelay(g)));

            foreach (Coroutine routine in routines)
                yield return routine;
        }

        RefreshClosestActiveEnemy();

        if (wave.requireKillAllToComplete)
        {
            while (true)
            {
                CleanupCollections();
                RefreshClosestActiveEnemy();

                if (living.Count == 0)
                    break;

                yield return null;
            }
        }

        if (wave.postWaveDelay > 0f)
        {
            SetStatus("Wave complete!");
            yield return new WaitForSeconds(wave.postWaveDelay);
        }

        currentIndex++;
        isWaveRunning = false;
        ClearActiveEnemy();
        completedTypingTargets.Clear();

        if (currentIndex >= waveFiles.Length && !loopWaves)
        {
            SetStatus("All waves complete");
            SetButtonInteractable(false);
            ShowEndScreen("You Win!");
        }
        else
        {
            if (openShopBetweenWaves && augmentShop != null)
            {
                SetStatus("Choose an augment");
                augmentShop.OpenShop();
            }
            else
            {
                ShowReadyForNextWave();
            }
        }
    }

    public void ShowReadyForNextWave()
    {
        SetStatus("Ready for next wave");
        SetButtonInteractable(true);
    }

    IEnumerator SpawnGroupWithDelay(WaveGroupJson g)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, g.startDelay));
        yield return SpawnGroup(g);
    }

    IEnumerator SpawnGroup(WaveGroupJson g)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            yield break;

        GameObject prefab = GetPrefab(g.enemyType);
        if (prefab == null)
            yield break;

        if (g.count == null || g.count.Length == 0)
            yield break;

        if (g.count.Length == 1 && g.interval != 0f)
        {
            int count = Mathf.Max(0, g.count[0]);
            float interval = Mathf.Max(0f, g.interval);

            for (int i = 0; i < count; i++)
            {
                SpawnOne(prefab);
                RefreshClosestActiveEnemy();

                if (interval > 0f)
                    yield return new WaitForSeconds(interval);
                else
                    yield return null;
            }
        }
        else
        {
            float interval = Mathf.Max(0f, g.interval);

            for (int i = 0; i < g.count.Length; i++)
            {
                int count = Mathf.Max(0, g.count[i]);
                SpawnRow(prefab, count);
                RefreshClosestActiveEnemy();

                if (interval > 0f)
                    yield return new WaitForSeconds(interval);
                else
                    yield return null;
            }
        }
    }

    void SpawnOne(GameObject prefab)
    {
        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject go = Instantiate(prefab, sp.position, sp.rotation);
        RegisterEnemy(go);
    }

    void SpawnRow(GameObject prefab, int count)
    {
        if (count <= 0)
            return;

        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Vector3 basePosition = sp.position;

        if (count == 1)
        {
            GameObject single = Instantiate(prefab, basePosition, sp.rotation);
            RegisterEnemy(single);
            return;
        }

        float totalWidth = 8f;
        float spacing = totalWidth / (count - 1);
        float startX = basePosition.x - 4f;

        for (int i = 0; i < count; i++)
        {
            Vector3 spawnPos = new Vector3(startX + i * spacing, basePosition.y, basePosition.z);
            GameObject go = Instantiate(prefab, spawnPos, sp.rotation);
            RegisterEnemy(go);
        }
    }

    void RegisterEnemy(GameObject go)
    {
        if (go == null)
            return;

        living.Add(go);

        EnemyBase enemy = go.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.spellDatabase = spellDatabase;

            if (augmentManager != null)
                enemy.moveSpeed *= augmentManager.enemySpeedMultiplier;
        }
    }

    public void NotifyEnemyDied(GameObject enemyObject)
    {
        if (enemyObject != null)
            living.Remove(enemyObject);

        EnemyBase enemy = null;
        if (enemyObject != null)
            enemy = enemyObject.GetComponent<EnemyBase>();

        if (enemy != null)
            completedTypingTargets.Remove(enemy);

        if (activeEnemy != null && activeEnemy.gameObject == enemyObject)
        {
            activeEnemy.SetActiveTarget(false);
            activeEnemy = null;
        }

        RefreshClosestActiveEnemy();
    }

    public void AdvanceToNextEnemyImmediately(EnemyBase justCompletedEnemy)
    {
        if (justCompletedEnemy != null)
        {
            completedTypingTargets.Add(justCompletedEnemy);
            justCompletedEnemy.SetActiveTarget(false);
        }

        if (activeEnemy == justCompletedEnemy)
            activeEnemy = null;

        RefreshClosestActiveEnemy();
    }

    public void StunAllLivingEnemies(float duration)
    {
        foreach (GameObject go in living)
        {
            if (go == null)
                continue;

            EnemyBase enemy = go.GetComponent<EnemyBase>();
            if (enemy != null)
                enemy.ApplyStun(duration);
        }
    }

    void RefreshClosestActiveEnemy()
    {
        CleanupCollections();

        EnemyBase closest = FindClosestEligibleEnemyToPlayer();

        if (closest == activeEnemy)
            return;

        if (activeEnemy != null)
            activeEnemy.SetActiveTarget(false);

        activeEnemy = closest;

        if (activeEnemy != null)
            activeEnemy.SetActiveTarget(true);
    }

    EnemyBase FindClosestEligibleEnemyToPlayer()
    {
        if (player == null)
            return null;

        EnemyBase closestEnemy = null;
        float bestDistSqr = float.PositiveInfinity;
        Vector3 playerPos = player.position;

        foreach (GameObject go in living)
        {
            if (go == null)
                continue;

            EnemyBase enemy = go.GetComponent<EnemyBase>();
            if (enemy == null)
                continue;

            if (completedTypingTargets.Contains(enemy))
                continue;

            float distSqr = (enemy.transform.position - playerPos).sqrMagnitude;
            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }

    void CleanupCollections()
    {
        living.RemoveWhere(go => go == null);
        completedTypingTargets.RemoveWhere(enemy => enemy == null);
    }

    void ClearActiveEnemy()
    {
        if (activeEnemy != null)
            activeEnemy.SetActiveTarget(false);

        activeEnemy = null;
    }

    WaveJsonData LoadWave(string fileName)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>($"Waves/{fileName}");
        if (jsonFile == null)
            return null;

        return JsonUtility.FromJson<WaveJsonData>(jsonFile.text);
    }

    GameObject GetPrefab(string enemyType)
    {
        if (string.IsNullOrWhiteSpace(enemyType))
            return null;

        enemyType = enemyType.Trim();

        switch (enemyType)
        {
            case "Earth": return EarthPrefab;
            case "Fire": return FirePrefab;
            case "Water": return WaterPrefab;
            default: return null;
        }
    }

    void SetButtonInteractable(bool on)
    {
        if (nextWaveButton != null)
            nextWaveButton.interactable = on;
    }

    void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
    }

    void ShowEndScreen(string message)
    {
        if (endScreenPanel != null)
            endScreenPanel.SetActive(true);

        if (endScreenText != null)
            endScreenText.text = message;
    }

    void HideEndScreen()
    {
        if (endScreenPanel != null)
            endScreenPanel.SetActive(false);
    }
}