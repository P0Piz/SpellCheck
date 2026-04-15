using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject tipPanel;
    public GameObject mainPanel;
    public GameObject difficultyPanel;
    public GameObject scoresPanel;
    public GameObject settingsPanel;

    [Header("Default Selected Buttons")]
    public Button tipDefaultButton;
    public Button mainDefaultButton;
    public Button difficultyDefaultButton;
    public Button scoresDefaultButton;
    public Button settingsDefaultButton;

    [Header("Scene")]
    public string gameplaySceneName = "GameScene";

    [Header("Input Lock")]
    [SerializeField] private float inputBlockTime = 0.15f;
    private float inputBlockedUntil = 0f;

    void Start()
    {
        ShowTip();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (Time.unscaledTime < inputBlockedUntil)
            return;

        HandleSubmitInput();
        HandleBackInput();
        EnsureSomethingIsSelected();
    }

    void BlockInputTemporarily()
    {
        inputBlockedUntil = Time.unscaledTime + inputBlockTime;
    }

    void HandleSubmitInput()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;

            if (selected == null)
                return;

            Button button = selected.GetComponent<Button>();
            if (button != null)
                button.onClick.Invoke();
        }
    }

    void HandleBackInput()
    {
        if (!Input.GetKeyDown(KeyCode.L))
            return;

        if (difficultyPanel.activeSelf || scoresPanel.activeSelf || settingsPanel.activeSelf)
        {
            ShowMain();
        }
    }

    void EnsureSomethingIsSelected()
    {
        if (EventSystem.current.currentSelectedGameObject != null)
            return;

        if (tipPanel.activeSelf && tipDefaultButton != null)
            EventSystem.current.SetSelectedGameObject(tipDefaultButton.gameObject);
        else if (mainPanel.activeSelf && mainDefaultButton != null)
            EventSystem.current.SetSelectedGameObject(mainDefaultButton.gameObject);
        else if (difficultyPanel.activeSelf && difficultyDefaultButton != null)
            EventSystem.current.SetSelectedGameObject(difficultyDefaultButton.gameObject);
        else if (scoresPanel.activeSelf && scoresDefaultButton != null)
            EventSystem.current.SetSelectedGameObject(scoresDefaultButton.gameObject);
        else if (settingsPanel.activeSelf && settingsDefaultButton != null)
            EventSystem.current.SetSelectedGameObject(settingsDefaultButton.gameObject);
    }

    public void PlayPressed()
    {
        ShowDifficulty();
    }

    public void ScoresPressed()
    {
        ShowScores();
    }

    public void SettingsPressed()
    {
        ShowSettings();
    }

    public void ExitPressed()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }

    public void SelectEasy()
    {
        SetDifficultyAndStart(GameDifficulty.Easy);
    }

    public void SelectNormal()
    {
        SetDifficultyAndStart(GameDifficulty.Normal);
    }

    public void SelectHard()
    {
        SetDifficultyAndStart(GameDifficulty.Hard);
    }

    void SetDifficultyAndStart(GameDifficulty difficulty)
    {
        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.SetDifficulty(difficulty);

        SceneManager.LoadScene(gameplaySceneName);
    }

    public void ShowTip()
    {
        tipPanel.SetActive(true);
        mainPanel.SetActive(false);
        difficultyPanel.SetActive(false);
        scoresPanel.SetActive(false);
        settingsPanel.SetActive(false);

        SelectButton(tipDefaultButton);
        BlockInputTemporarily();
    }

    public void ShowMain()
    {
        tipPanel.SetActive(false);
        mainPanel.SetActive(true);
        difficultyPanel.SetActive(false);
        scoresPanel.SetActive(false);
        settingsPanel.SetActive(false);

        SelectButton(mainDefaultButton);
        BlockInputTemporarily();
    }

    public void ShowDifficulty()
    {
        mainPanel.SetActive(false);
        difficultyPanel.SetActive(true);
        scoresPanel.SetActive(false);
        settingsPanel.SetActive(false);

        SelectButton(difficultyDefaultButton);
        BlockInputTemporarily();
    }

    public void ShowScores()
    {
        mainPanel.SetActive(false);
        difficultyPanel.SetActive(false);
        scoresPanel.SetActive(true);
        settingsPanel.SetActive(false);

        SelectButton(scoresDefaultButton);
        BlockInputTemporarily();
    }

    public void ShowSettings()
    {
        mainPanel.SetActive(false);
        difficultyPanel.SetActive(false);
        scoresPanel.SetActive(false);
        settingsPanel.SetActive(true);

        SelectButton(settingsDefaultButton);
        BlockInputTemporarily();
    }

    void SelectButton(Button button)
    {
        if (button == null)
            return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
}