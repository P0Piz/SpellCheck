using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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

    [Header("Next Round Panel")]
    public GameObject NextWavePanel;
    public Button nextWaveButton;

    public int CurrentWaveIndex => currentIndex;
    public bool IsWaveRunning => isWaveRunning;

    private int currentIndex;
    private bool isWaveRunning;

    private readonly HashSet<GameObject> living = new HashSet<GameObject>();

    private readonly HashSet<EnemyBase> usedEnemies = new HashSet<EnemyBase>();

    private EnemyBase activeEnemy;

    private Transform player;

    public GameObject tip;

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
        if (tip != null)
        {
            Destroy(tip);
        }

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
                SceneManager.LoadScene("WinScreen");
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

        if (WaveMusicManager.Instance != null)
            WaveMusicManager.Instance.PlayForWave(currentIndex + 1);

        if (NextWavePanel != null)
            NextWavePanel.SetActive(false);

        WaveJsonData wave = LoadWave(fileName);
        if (wave == null)
        {
            SetStatus($"Missing wave: {fileName}");
            isWaveRunning = false;

            if (NextWavePanel != null)
                NextWavePanel.SetActive(true);

            yield break;
        }

        living.Clear();
        usedEnemies.Clear();
        ClearActiveEnemy();

        float roundStartDelay = GetRoundStartDelay(wave);

        if (roundStartDelay > 0f)
            yield return StartCoroutine(ShowRoundCountdown(wave.waveName, roundStartDelay));
        else
            SetStatus("");

        SetStatus("");

        if (wave.groups != null && wave.groups.Count > 0)
        {
            if (wave.runGroupsSequentially)
            {
                for (int i = 0; i < wave.groups.Count; i++)
                    yield return StartCoroutine(SpawnGroup(wave.groups[i], i == 0));
            }
            else
            {
                List<Coroutine> routines = new List<Coroutine>();

                for (int i = 0; i < wave.groups.Count; i++)
                    routines.Add(StartCoroutine(SpawnGroup(wave.groups[i], i == 0)));

                foreach (Coroutine routine in routines)
                    yield return routine;
            }
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

        if (WaveMusicManager.Instance != null)
            WaveMusicManager.Instance.PlayChill();

        ClearActiveEnemy();
        usedEnemies.Clear();

        if (currentIndex >= waveFiles.Length && !loopWaves)
        {
            SceneManager.LoadScene("WinScreen");
        }
        else
        {
            if (openShopBetweenWaves && augmentShop != null)
            {
                SetStatus("");
                augmentShop.OpenShop();

                if (WaveMusicManager.Instance != null)
                    WaveMusicManager.Instance.PlayChill();
            }
            else
            {
                ShowReadyForNextWave();
            }
        }
    }

    float GetRoundStartDelay(WaveJsonData wave)
    {
        if (wave == null || wave.groups == null || wave.groups.Count == 0)
            return 0f;

        WaveGroupJson firstGroup = wave.groups[0];
        if (firstGroup == null)
            return 0f;

        return Mathf.Max(0f, firstGroup.startDelay);
    }

    IEnumerator ShowRoundCountdown(string waveName, float delay)
    {
        int secondsLeft = Mathf.CeilToInt(delay);

        while (secondsLeft > 0)
        {
            SetStatus($"{waveName}\nStarting in {secondsLeft}...");
            yield return new WaitForSeconds(1f);
            secondsLeft--;
        }

        SetStatus("GO!");
        yield return new WaitForSeconds(0.5f);
        SetStatus("");
    }

    public void ShowReadyForNextWave()
    {
        SetStatus("");

        if (NextWavePanel != null)
            NextWavePanel.SetActive(true);
    }

    IEnumerator SpawnGroup(WaveGroupJson group, bool isFirstGroup)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            yield break;

        if (group == null)
            yield break;

        GameObject prefab = GetPrefab(group.enemyType);
        if (prefab == null)
            yield break;

        if (group.count == null || group.count.Length == 0)
            yield break;

        float interval = Mathf.Max(0f, group.interval);

        if (!isFirstGroup && group.startDelay > 0f)
            yield return new WaitForSeconds(group.startDelay);

        if (group.count.Length == 1)
        {
            int count = Mathf.Max(0, group.count[0]);
            SpawnRow(prefab, count);

            if (interval > 0f)
                yield return new WaitForSeconds(interval);
            else
                yield return null;
        }
        else
        {
            for (int i = 0; i < group.count.Length; i++)
            {
                int count = Mathf.Max(0, group.count[i]);
                SpawnRow(prefab, count);

                if (interval > 0f)
                    yield return new WaitForSeconds(interval);
                else
                    yield return null;
            }
        }
    }

    void SpawnRow(GameObject prefab, int count)
    {
        if (prefab == null)
            return;

        if (spawnPoints == null || spawnPoints.Length == 0)
            return;

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
        float startX = basePosition.x - (totalWidth * 0.5f);

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

        RefreshClosestActiveEnemy();
    }

    public void NotifyEnemyDied(GameObject enemyObject)
    {
        if (enemyObject != null)
            living.Remove(enemyObject);

        EnemyBase enemy = null;
        if (enemyObject != null)
            enemy = enemyObject.GetComponent<EnemyBase>();

        if (enemy != null)
            usedEnemies.Remove(enemy);

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
            usedEnemies.Add(justCompletedEnemy);
            justCompletedEnemy.SetActiveTarget(false);
        }

        if (activeEnemy == justCompletedEnemy)
            activeEnemy = null;

        RefreshClosestActiveEnemy();
    }

    public void StunAllLivingEnemies(float duration)
    {
        CleanupCollections();

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

        EnemyBase closest = FindClosestUnusedLivingEnemyToPlayer();

        if (closest == activeEnemy)
            return;

        if (activeEnemy != null)
            activeEnemy.SetActiveTarget(false);

        activeEnemy = closest;

        if (activeEnemy != null)
            activeEnemy.SetActiveTarget(true);
    }

    EnemyBase FindClosestUnusedLivingEnemyToPlayer()
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

            if (usedEnemies.Contains(enemy))
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
        usedEnemies.RemoveWhere(enemy => enemy == null);
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
            case "Earth":
                return EarthPrefab;

            case "Fire":
                return FirePrefab;

            case "Water":
                return WaterPrefab;

            default:
                return null;
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