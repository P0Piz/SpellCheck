using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [System.Serializable]
    public class PanelEntry
    {
        public GameObject panel;
        public Selectable defaultSelectable;
    }

    public PanelEntry tipPanel;
    public PanelEntry mainPanel;
    public PanelEntry difficultyPanel;
    public PanelEntry scoresPanel;
    public PanelEntry settingsPanel;

    public string gameplaySceneName = "GameScene";

    [SerializeField] private KeyCode submitKey = KeyCode.K;
    [SerializeField] private KeyCode backKey = KeyCode.L;

    [SerializeField] private float inputBlockTime = 0.15f;

    [SerializeField] private string buttonPressTrigger = "Press";
    [SerializeField] private float buttonPressDelay = 0.15f;

    [Header("UI Audio")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip navigateClip;
    [SerializeField] private AudioClip selectClip;
    [SerializeField] private float navigateVolume = 1f;
    [SerializeField] private float selectVolume = 1f;

    private float inputBlockedUntil = 0f;
    private PanelEntry currentPanel;
    private bool isPressingButton = false;

    private GameObject lastSelectedObject;

    public TitleTypingFX title;

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

        if (EventSystem.current == null)
            return;

        EnsureValidSelection();
        CheckSelectionChanged();
        HandleSubmitInput();
        HandleBackInput();
    }

    void HandleSubmitInput()
    {
        if (!Input.GetKeyDown(submitKey))
            return;

        if (isPressingButton)
            return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null)
            return;

        TMP_InputField input = selected.GetComponent<TMP_InputField>();
        if (input != null)
            return;

        Button button = selected.GetComponent<Button>();
        if (button != null && button.IsInteractable())
            StartCoroutine(PressButton(button));
    }

    IEnumerator PressButton(Button button)
    {
        isPressingButton = true;

        PlaySelectSound();

        Animator anim = button.GetComponent<Animator>();

        if (anim != null)
        {
            anim.ResetTrigger(buttonPressTrigger);
            anim.SetTrigger(buttonPressTrigger);
            yield return new WaitForSecondsRealtime(buttonPressDelay);
        }
        else
        {
            yield return null;
        }

        if (button != null && button.IsInteractable())
            button.onClick.Invoke();

        isPressingButton = false;
    }

    void HandleBackInput()
    {
        if (!Input.GetKeyDown(backKey))
            return;

        if (currentPanel == difficultyPanel || currentPanel == scoresPanel || currentPanel == settingsPanel)
            ShowMain();
    }

    void EnsureValidSelection()
    {
        if (currentPanel == null || currentPanel.panel == null)
            return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (selected == null)
        {
            Select(currentPanel.defaultSelectable, false);
            return;
        }

        if (!selected.transform.IsChildOf(currentPanel.panel.transform))
        {
            Select(currentPanel.defaultSelectable, false);
            return;
        }

        Selectable s = selected.GetComponent<Selectable>();
        if (s != null && (!s.IsInteractable() || !s.gameObject.activeInHierarchy))
            Select(currentPanel.defaultSelectable, false);
    }

    void CheckSelectionChanged()
    {
        if (EventSystem.current == null)
            return;

        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (currentSelected == null)
            return;

        if (currentSelected != lastSelectedObject)
        {
            if (lastSelectedObject != null)
                PlayNavigateSound();

            lastSelectedObject = currentSelected;
        }
    }

    void Select(Selectable selectable, bool playSound = false)
    {
        if (selectable == null || EventSystem.current == null)
            return;

        GameObject previousSelected = EventSystem.current.currentSelectedGameObject;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(selectable.gameObject);

        TMP_InputField input = selectable.GetComponent<TMP_InputField>();
        if (input != null)
        {
            input.ActivateInputField();
            input.MoveTextEnd(false);
        }

        if (playSound && previousSelected != selectable.gameObject)
            PlayNavigateSound();

        lastSelectedObject = selectable.gameObject;
    }

    void SwitchPanel(PanelEntry panel)
    {
        tipPanel.panel.SetActive(false);
        mainPanel.panel.SetActive(false);
        difficultyPanel.panel.SetActive(false);
        scoresPanel.panel.SetActive(false);
        settingsPanel.panel.SetActive(false);

        panel.panel.SetActive(true);
        currentPanel = panel;

        StartCoroutine(SelectNextFrame(panel));
        BlockInput();
    }

    IEnumerator SelectNextFrame(PanelEntry panel)
    {
        yield return null;
        Select(panel.defaultSelectable, false);
    }

    void BlockInput()
    {
        inputBlockedUntil = Time.unscaledTime + inputBlockTime;
    }

    void PlayNavigateSound()
    {
        if (uiAudioSource != null && navigateClip != null)
        {
            uiAudioSource.pitch = Random.Range(0.9f, 1.1f);
            uiAudioSource.PlayOneShot(navigateClip, navigateVolume);
        }
    }

    void PlaySelectSound()
    {
        if (uiAudioSource != null && selectClip != null)
        {
            uiAudioSource.pitch = Random.Range(0.95f, 1.15f);
            uiAudioSource.PlayOneShot(selectClip, selectVolume);
        }
    }

    public void PlayPressed() => ShowDifficulty();
    public void ScoresPressed() => ShowScores();
    public void SettingsPressed() => ShowSettings();

    public void ExitPressed()
    {
        Application.Quit();
        Debug.Log("Quit Game");
    }

    public void SelectEasy() => SetDifficultyAndStart(GameDifficulty.Easy);
    public void SelectNormal() => SetDifficultyAndStart(GameDifficulty.Normal);
    public void SelectHard() => SetDifficultyAndStart(GameDifficulty.Hard);

    void SetDifficultyAndStart(GameDifficulty difficulty)
    {
        if (DifficultyManager.Instance != null)
            DifficultyManager.Instance.SetDifficulty(difficulty);

        SceneManager.LoadScene(gameplaySceneName);
    }

    public void EnableTitle()
    {
        title.enabled = true;
    }

    public void ShowTip() => SwitchPanel(tipPanel);
    public void ShowMain() => SwitchPanel(mainPanel);
    public void ShowDifficulty() => SwitchPanel(difficultyPanel);
    public void ShowScores() => SwitchPanel(scoresPanel);
    public void ShowSettings() => SwitchPanel(settingsPanel);
}