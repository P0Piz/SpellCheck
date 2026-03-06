using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WaveSpawnerJson : MonoBehaviour
{
    [Header("Wave Order (Resources/Waves/)")]
    [Tooltip("Example values: wave1, wave2, wave3 (NO extension). Files must be in Assets/Resources/Waves/")]
    public string[] waveFiles = { "wave1", "wave2" };

    [Tooltip("Which wave index to start on.")]
    public int startIndex = 0;

    [Header("Enemy Prefabs")]
    public GameObject EarthPrefab;
    public GameObject FirePrefab;
    public GameObject WaterPrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("UI")]
    public Button nextWaveButton;
    public TMPro.TMP_Text statusText;

    [Header("Behaviour")]
    public bool autoStartFirstWave = false;
    public bool loopWaves = false;

    public int CurrentWaveIndex => currentIndex;
    public bool IsWaveRunning => isWaveRunning;

    private int currentIndex;
    private bool isWaveRunning = false;

    private readonly HashSet<GameObject> living = new HashSet<GameObject>();

    [Header("TypingSys")]
    public SpellTypingSystem typing;

    void Awake()
    {
        currentIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, waveFiles.Length - 1));
        SetButtonInteractable(true);
        SetStatus("Ready");
    }

    void Start()
    {
        if (autoStartFirstWave)
            StartNextWave();
    }

    private void Update()
    {
        typing.enabled = isWaveRunning;
    }

    public void StartNextWaveButton()
    {
        StartNextWave();
    }

    public void StartNextWave()
    {
        if (isWaveRunning) return;

        if (waveFiles == null || waveFiles.Length == 0)
        {
            Debug.LogError("WaveSpawnerJson: waveFiles is empty.");
            SetStatus("No waves set");
            return;
        }

        if (currentIndex >= waveFiles.Length)
        {
            if (loopWaves) currentIndex = 0;
            else
            {
                Debug.Log("WaveSpawnerJson: All waves complete.");
                SetStatus("All waves complete");
                SetButtonInteractable(false);
                return;
            }
        }

        StartCoroutine(RunWaveRoutine(waveFiles[currentIndex]));
    }

    public void ResetWaves(int index = 0)
    {
        if (isWaveRunning) return;

        currentIndex = Mathf.Clamp(index, 0, Mathf.Max(0, waveFiles.Length - 1));
        SetStatus("Ready");
        SetButtonInteractable(true);
    }

    private IEnumerator RunWaveRoutine(string fileName)
    {
        isWaveRunning = true;
        SetButtonInteractable(false);

        WaveJsonData wave = LoadWave(fileName);
        if (wave == null)
        {
            Debug.LogError($"WaveSpawnerJson: Could not load wave '{fileName}'.");
            SetStatus($"Missing wave: {fileName}");
            isWaveRunning = false;
            SetButtonInteractable(true);
            yield break;
        }

        living.RemoveWhere(go => go == null);

        SetStatus($"{wave.waveName}");

        if (wave.runGroupsSequentially)
        {
            foreach (var g in wave.groups)
            {
                yield return new WaitForSeconds(Mathf.Max(0f, g.startDelay));
                yield return SpawnGroup(g);
            }
        }
        else
        {
            var routines = new List<Coroutine>();
            foreach (var g in wave.groups)
                routines.Add(StartCoroutine(SpawnGroupWithDelay(g)));

            foreach (var r in routines)
                yield return r;
        }

        if (wave.requireKillAllToComplete)
        {
            SetStatus($"{wave.waveName}");
            while (true)
            {
                living.RemoveWhere(go => go == null);
                if (living.Count == 0) break;
                yield return null;
            }
        }

        if (wave.postWaveDelay > 0f)
        {
            SetStatus($"Wave complete!");
            yield return new WaitForSeconds(wave.postWaveDelay);
        }

        currentIndex++;
        isWaveRunning = false;

        if (currentIndex >= waveFiles.Length && !loopWaves)
        {
            SetStatus("All waves complete");
            SetButtonInteractable(false);
        }
        else
        {
            SetStatus("Ready for next wave");
            SetButtonInteractable(true);
        }
    }

    private IEnumerator SpawnGroupWithDelay(WaveGroupJson g)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, g.startDelay));
        yield return SpawnGroup(g);
    }

    private IEnumerator SpawnGroup(WaveGroupJson g)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("WaveSpawnerJson: No spawnPoints assigned.");
            yield break;
        }

        GameObject prefab = GetPrefab(g.enemyType);
        if (!prefab)
        {
            Debug.LogError($"WaveSpawnerJson: Unknown enemyType '{g.enemyType}'. Expected TypeA/TypeB/TypeC.");
            yield break;
        }

        int count = Mathf.Max(0, g.count);
        float interval = Mathf.Max(0f, g.interval);

        for (int i = 0; i < count; i++)
        {
            SpawnOne(prefab);

            if (interval > 0f) yield return new WaitForSeconds(interval);
            else yield return null;
        }
    }

    private void SpawnOne(GameObject prefab)
    {
        Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
        var go = Instantiate(prefab, sp.position, sp.rotation);
        living.Add(go);
    }

    public void NotifyEnemyDied(GameObject enemy)
    {
        if (enemy != null)
            living.Remove(enemy);
    }

    private WaveJsonData LoadWave(string fileName)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>($"Waves/{fileName}");
        if (jsonFile == null)
            return null;

        return JsonUtility.FromJson<WaveJsonData>(jsonFile.text);
    }

    private GameObject GetPrefab(string enemyType)
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

    private void SetButtonInteractable(bool on)
    {
        if (nextWaveButton != null)
            nextWaveButton.interactable = on;
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
    }
}