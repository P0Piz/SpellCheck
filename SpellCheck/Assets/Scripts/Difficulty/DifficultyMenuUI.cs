using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class DifficultyMenuUI : MonoBehaviour
{
    [Header("Optional UI")]
    [SerializeField] private TMP_Text currentDifficultyText;

    [Header("Scene Loading")]
    [SerializeField] private bool loadSceneAfterSelection = true;
    [SerializeField] private string gameplaySceneName = "Game";

    void Start()
    {
        RefreshDifficultyText();
    }

    public void SelectEasy()
    {
        SetDifficulty(GameDifficulty.Easy);
    }

    public void SelectNormal()
    {
        SetDifficulty(GameDifficulty.Normal);
    }

    public void SelectHard()
    {
        SetDifficulty(GameDifficulty.Hard);
    }

    void SetDifficulty(GameDifficulty difficulty)
    {
        if (DifficultyManager.Instance == null)
        {
            Debug.LogError("No DifficultyManager found in scene.");
            return;
        }

        DifficultyManager.Instance.SetDifficulty(difficulty);
        RefreshDifficultyText();

        if (loadSceneAfterSelection)
            SceneManager.LoadScene(gameplaySceneName);
    }

    void RefreshDifficultyText()
    {
        if (currentDifficultyText == null)
            return;

        if (DifficultyManager.Instance == null)
        {
            currentDifficultyText.text = "Difficulty: None";
            return;
        }

        currentDifficultyText.text =
            "Difficulty: " + DifficultyManager.Instance.GetCurrentDifficultyName();
    }
}