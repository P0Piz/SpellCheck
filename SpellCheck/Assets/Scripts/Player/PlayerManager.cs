using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class PlayerManager : MonoBehaviour
{
    [Header("Delete On Death")]
    public GameObject inputfield;

    [Header("Difficulty")]
    public Dictionary<string, float> difficultyValues = new Dictionary<string, float>
    {
        {"Easy", 0.3f},
        {"Normal", 0.7f},
        {"Hard", 1.0f}
    };
    public string currentDifficulty = "Normal";

    [Header("Health")]
    public int maxLives = 4;
    public int currentLives;

    [Header("Temporary Hearts")]
    public int temporaryLives = 0;

    [Header("Invincibility")]
    public float invincibleDuration = 1f;
    public float invincibleAlpha = 0.5f;
    private bool invincible = false;

    [Header("Score")]
    public int currentScore = 0;
    public TMP_Text scoreDisplay;

    [Header("Augments")]
    public PlayerAugmentManager augmentManager;

    [Header("Heart UI")]
    public Image[] hearts;
    public Sprite fullHeart;
    public Sprite emptyHeart;
    public Sprite tempHeart;

    [Header("Death Flow UI")]
    public GameObject nameEntryPanel;
    public GameObject leaderboardPanel;
    public GameObject restartPanel;

    [Header("Name Entry UI")]
    public TMP_InputField nameInputField;
    public Button submitScoreButton;

    [Header("Leaderboard UI")]
    public TMP_Text leaderboardDisplay;
    public Button continueButton;

    [Header("Player Visuals")]
    public MeshRenderer[] playerRenderers;

    [Header("Screen Shake")]
    public Transform cameraTransform;
    public float shakeDuration = 0.15f;
    public float shakeMagnitude = 0.15f;

    [Header("Hit Feedback")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private Vector2 hitPitchRange = new Vector2(0.9f, 1.1f);
    [SerializeField] private float hitVolume = 1f;

    private Vector3 cameraOriginalLocalPos;
    private Coroutine shakeRoutine;

    private bool waitingForName = false;
    private bool scoreSubmitted = false;

    private int lastSubmittedScore = -1;
    private string lastSubmittedName = "";

    public static Leaderboard leaderboard;

    void Awake()
    {
        if (leaderboard == null)
            leaderboard = new Leaderboard();

        if (augmentManager == null)
            augmentManager = FindObjectOfType<PlayerAugmentManager>();
    }

    [System.Serializable]
    public class Leaderboard
    {
        private LeaderboardEntry[] entries = new LeaderboardEntry[10];

        [System.Serializable]
        public class LeaderboardEntry
        {
            public string playerName;
            public int score;
            public string difficulty;
        }

        public Leaderboard()
        {
            for (int i = 0; i < entries.Length; i++)
            {
                entries[i] = new LeaderboardEntry
                {
                    playerName = "Empty",
                    score = 0
                };
            }
        }

        public void AddEntry(string playerName, int score, string difficulty)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (score > entries[i].score)
                {
                    for (int j = entries.Length - 1; j > i; j--)
                    {
                        entries[j] = entries[j - 1];
                    }

                    entries[i] = new LeaderboardEntry
                    {
                        playerName = playerName,
                        score = score,
                        difficulty = difficulty
                    };
                    return;
                }
            }
        }

        public LeaderboardEntry[] GetLeaderboard()
        {
            return entries.Clone() as LeaderboardEntry[];
        }
    }

    void Start()
    {
        currentLives = maxLives;

        if (playerRenderers == null || playerRenderers.Length == 0)
            playerRenderers = GetComponentsInChildren<MeshRenderer>();

        HideAllDeathUI();

        if (submitScoreButton != null)
            submitScoreButton.onClick.AddListener(SubmitLeaderboardName);

        if (continueButton != null)
            continueButton.onClick.AddListener(ShowRestartPanel);

        if (cameraTransform != null)
            cameraOriginalLocalPos = cameraTransform.localPosition;

        RefreshHearts();
        SetOpacity(1f);
        UpdateScoreUI();
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (waitingForName && !scoreSubmitted && Input.GetKeyDown(KeyCode.Return))
        {
            SubmitLeaderboardName();
        }
    }

    public void AddScore(int amount)
    {
        currentScore += (int)System.Math.Round(amount * difficultyValues[currentDifficulty]);
        UpdateScoreUI();
    }

    public void MinusScore(int amount)
    {
        if (currentScore >= amount)
        {
            currentScore -= amount;
            UpdateScoreUI();
        }
    }

    public void ResetScore()
    {
        currentScore = 0;
        UpdateScoreUI();
    }

    public bool CanAfford(int amount)
    {
        return currentScore >= amount;
    }

    void UpdateScoreUI()
    {
        if (scoreDisplay != null)
            scoreDisplay.text = "Score: " + currentScore;
    }

    public void AddTemporaryHeart(int amount)
    {
        temporaryLives += amount;
        if (temporaryLives < 0)
            temporaryLives = 0;

        RefreshHearts();
    }

    public void TakeDamage(int amount)
    {
        if (invincible)
            return;

        for (int i = 0; i < amount; i++)
        {
            // temp hearts get eaten first
            if (temporaryLives > 0)
            {
                temporaryLives--;
                continue;
            }

            // check lethal hit before applying it
            bool wouldDie = currentLives - 1 <= 0;

            if (wouldDie && augmentManager != null && augmentManager.CanUseLastChance())
            {
                currentLives = 1;
                augmentManager.ConsumeLastChance();

                RefreshHearts();
                TriggerScreenShake();
                PlayHitSound();
                StartCoroutine(InvincibilityFrames());
                return;
            }

            currentLives--;
        }

        if (currentLives < 0)
            currentLives = 0;

        RefreshHearts();
        TriggerScreenShake();
        PlayHitSound();

        if (currentLives <= 0)
        {
            PlayerDied();
            return;
        }

        StartCoroutine(InvincibilityFrames());
    }

    void TriggerScreenShake()
    {
        if (cameraTransform == null)
            return;

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        cameraTransform.localPosition = cameraOriginalLocalPos;
        shakeRoutine = StartCoroutine(ScreenShake());
    }

    void PlayHitSound()
    {
        if (audioSource == null || hitSound == null)
            return;

        audioSource.pitch = Random.Range(hitPitchRange.x, hitPitchRange.y);
        audioSource.PlayOneShot(hitSound, hitVolume);
    }

    IEnumerator ScreenShake()
    {
        float timer = 0f;

        while (timer < shakeDuration)
        {
            Vector3 randomOffset = Random.insideUnitSphere * shakeMagnitude;
            randomOffset.z = 0f;

            cameraTransform.localPosition = cameraOriginalLocalPos + randomOffset;

            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        cameraTransform.localPosition = cameraOriginalLocalPos;
        shakeRoutine = null;
    }

    public void Heal(int amount)
    {
        currentLives += amount;

        if (currentLives > maxLives)
            currentLives = maxLives;

        RefreshHearts();
    }

    IEnumerator InvincibilityFrames()
    {
        invincible = true;
        SetOpacity(invincibleAlpha);

        yield return new WaitForSeconds(invincibleDuration);

        invincible = false;
        SetOpacity(1f);
    }

    void PlayerDied()
    {
        SetOpacity(1f);

        SpellTypingSystem typingSystem = GetComponent<SpellTypingSystem>();
        if (typingSystem != null)
            typingSystem.enabled = false;

        if (inputfield != null)
            Destroy(inputfield);

        waitingForName = true;
        scoreSubmitted = false;

        lastSubmittedScore = currentScore;
        lastSubmittedName = "";

        HideAllDeathUI();

        if (nameEntryPanel != null)
            nameEntryPanel.SetActive(true);

        if (nameInputField != null)
        {
            nameInputField.text = "";
            nameInputField.ActivateInputField();
            nameInputField.Select();
        }

        Time.timeScale = 0f;
    }

    public void SubmitLeaderboardName()
    {
        if (!waitingForName || scoreSubmitted)
            return;

        string playerName = "";

        if (nameInputField != null)
            playerName = nameInputField.text.Trim().ToUpper();

        if (playerName.Length < 3)
            return;

        if (playerName.Length > 3)
            playerName = playerName.Substring(0, 3);

        leaderboard.AddEntry(playerName, currentScore, currentDifficulty);

        lastSubmittedName = playerName;
        scoreSubmitted = true;
        waitingForName = false;

        UpdateLeaderboardUI();

        HideAllDeathUI();

        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(true);
    }

    void UpdateLeaderboardUI()
    {
        if (leaderboardDisplay == null)
            return;

        Leaderboard.LeaderboardEntry[] entries = leaderboard.GetLeaderboard();

        string text = "=== Leaderboard ===\n\n";

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] == null)
                continue;

            if (entries[i].score <= 0)
                continue;

            bool isCurrentRun =
                entries[i].playerName == lastSubmittedName &&
                entries[i].score == lastSubmittedScore;

            if (isCurrentRun)
                text += "<color=orange>";

            text += (i + 1) + ". " + entries[i].playerName + " - " + entries[i].score;

            if (isCurrentRun)
                text += " Current Run</color>";

            text += "\n";
        }

        leaderboardDisplay.text = text;
    }

    public void ShowRestartPanel()
    {
        HideAllDeathUI();

        if (restartPanel != null)
            restartPanel.SetActive(true);
    }

    void HideAllDeathUI()
    {
        if (nameEntryPanel != null)
            nameEntryPanel.SetActive(false);

        if (leaderboardPanel != null)
            leaderboardPanel.SetActive(false);

        if (restartPanel != null)
            restartPanel.SetActive(false);
    }

    void RefreshHearts()
    {
        if (hearts == null || hearts.Length == 0)
            return;

        int totalDisplayedLives = currentLives + temporaryLives;

        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null)
                continue;

            if (i < currentLives)
            {
                hearts[i].sprite = fullHeart;
            }
            else if (i < totalDisplayedLives)
            {
                hearts[i].sprite = tempHeart != null ? tempHeart : fullHeart;
            }
            else
            {
                hearts[i].sprite = emptyHeart;
            }
        }
    }

    void SetOpacity(float alpha)
    {
        if (playerRenderers == null)
            return;

        foreach (MeshRenderer renderer in playerRenderers)
        {
            if (renderer == null)
                continue;

            foreach (Material mat in renderer.materials)
            {
                Color c = mat.color;
                c.a = alpha;
                mat.color = c;
            }
        }
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }
}