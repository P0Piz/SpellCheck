using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance;

    [System.Serializable]
    public class DifficultyData
    {
        public string displayName = "Normal";
        public float enemySpeedMultiplier = 1f;
        public float scoreMultiplier = 1f;
    }

    [Header("Difficulty Values")]
    public DifficultyData easy = new DifficultyData
    {
        displayName = "Easy",
        enemySpeedMultiplier = 0.3f,
        scoreMultiplier = 0.3f
    };

    public DifficultyData normal = new DifficultyData
    {
        displayName = "Normal",
        enemySpeedMultiplier = 0.7f,
        scoreMultiplier = 0.7f
    };

    public DifficultyData hard = new DifficultyData
    {
        displayName = "Hard",
        enemySpeedMultiplier = 1.0f,
        scoreMultiplier = 1.0f
    };

    [Header("Current Difficulty")]
    [SerializeField] private GameDifficulty currentDifficulty = GameDifficulty.Normal;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetDifficulty(GameDifficulty difficulty)
    {
        currentDifficulty = difficulty;
        Debug.Log("Difficulty set to: " + GetCurrentDifficultyName());
    }

    public GameDifficulty GetCurrentDifficulty()
    {
        return currentDifficulty;
    }

    public string GetCurrentDifficultyName()
    {
        return GetCurrentDifficultyData().displayName;
    }

    public DifficultyData GetCurrentDifficultyData()
    {
        switch (currentDifficulty)
        {
            case GameDifficulty.Easy:
                return easy;

            case GameDifficulty.Hard:
                return hard;

            default:
                return normal;
        }
    }

    public float GetEnemySpeedMultiplier()
    {
        return GetCurrentDifficultyData().enemySpeedMultiplier;
    }

    public float GetScoreMultiplier()
    {
        return GetCurrentDifficultyData().scoreMultiplier;
    }
}