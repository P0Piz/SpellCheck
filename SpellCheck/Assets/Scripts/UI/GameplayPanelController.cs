using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

public class GameplayUIKeyboardControl : MonoBehaviour
{
    [System.Serializable]
    public class PanelEntry
    {
        public GameObject panel;
        public Selectable defaultSelectable;
    }

    [Header("Panels To Watch")]
    public PanelEntry[] panels;

    [Header("Pause")]
    [SerializeField] private KeyCode pauseKey = KeyCode.Escape;
    [SerializeField] private GameObject pausePanel;

    [Header("Input")]
    [SerializeField] private KeyCode submitKey = KeyCode.K;
    [SerializeField] private float inputBlockTime = 0.15f;

    [Header("Button Press Animation")]
    [SerializeField] private string buttonPressTrigger = "Press";
    [SerializeField] private float buttonPressDelay = 0.15f;

    [Header("UI Audio")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip navigateClip;
    [SerializeField] private AudioClip selectClip;
    [SerializeField] private float navigateVolume = 1f;
    [SerializeField] private float selectVolume = 1f;

    private float inputBlockedUntil = 0f;
    private PanelEntry lastActivePanel;
    private bool isPressingButton = false;
    private GameObject lastSelectedObject;

    private GameObject panelHiddenByPause;
    private bool isPaused = false;

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandlePauseInput();

        if (Time.unscaledTime < inputBlockedUntil)
            return;

        if (EventSystem.current == null)
            return;

        PanelEntry activePanel = GetActivePanel();

        if (activePanel == null)
        {
            lastActivePanel = null;
            lastSelectedObject = null;
            return;
        }

        if (activePanel != lastActivePanel)
        {
            ForceSelectActivePanel(activePanel);
            lastActivePanel = activePanel;
            return;
        }

        EnsureValidSelection(activePanel);
        CheckSelectionChanged(activePanel);
        HandleSubmitInput();
    }

    void HandlePauseInput()
    {
        if (!Input.GetKeyDown(pauseKey))
            return;

        if (pausePanel == null)
            return;

        if (!isPaused)
            PauseGame();
        else
            UnpauseGame();
    }

    void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        panelHiddenByPause = null;

        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i].panel != null &&
                panels[i].panel.activeSelf &&
                panels[i].panel != pausePanel)
            {
                panelHiddenByPause = panels[i].panel;
                panels[i].panel.SetActive(false);
                break;
            }
        }

        pausePanel.SetActive(true);
        RefreshSelection();
    }

    void UnpauseGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        pausePanel.SetActive(false);

        if (panelHiddenByPause != null)
        {
            panelHiddenByPause.SetActive(true);
            panelHiddenByPause = null;
        }

        RefreshSelection();
    }

    PanelEntry GetActivePanel()
    {
        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i].panel != null && panels[i].panel.activeSelf)
                return panels[i];
        }

        return null;
    }

    void EnsureValidSelection(PanelEntry activePanel)
    {
        if (activePanel == null || activePanel.panel == null || EventSystem.current == null)
            return;

        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

        if (currentSelected == null)
        {
            SelectSelectable(activePanel.defaultSelectable, false);
            return;
        }

        if (!currentSelected.transform.IsChildOf(activePanel.panel.transform) &&
            currentSelected != activePanel.panel)
        {
            SelectSelectable(activePanel.defaultSelectable, false);
            return;
        }

        Selectable selectable = currentSelected.GetComponent<Selectable>();
        if (selectable != null)
        {
            if (!selectable.gameObject.activeInHierarchy || !selectable.IsInteractable())
                SelectSelectable(activePanel.defaultSelectable, false);
        }
    }

    void CheckSelectionChanged(PanelEntry activePanel)
    {
        if (activePanel == null || EventSystem.current == null)
            return;

        GameObject currentSelected = EventSystem.current.currentSelectedGameObject;
        if (currentSelected == null)
            return;

        if (!currentSelected.transform.IsChildOf(activePanel.panel.transform) &&
            currentSelected != activePanel.panel)
            return;

        if (currentSelected != lastSelectedObject)
        {
            if (lastSelectedObject != null)
                PlayNavigateSound();

            lastSelectedObject = currentSelected;
        }
    }

    void HandleSubmitInput()
    {
        if (!Input.GetKeyDown(submitKey))
            return;

        if (isPressingButton)
            return;

        if (EventSystem.current == null)
            return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null)
            return;

        TMP_InputField tmpInput = selected.GetComponent<TMP_InputField>();
        if (tmpInput != null)
            return;

        Button button = selected.GetComponent<Button>();
        if (button != null && button.IsInteractable())
            StartCoroutine(PressButtonWithAnimation(button));
    }

    IEnumerator PressButtonWithAnimation(Button button)
    {
        if (button == null)
            yield break;

        isPressingButton = true;

        PlaySelectSound();

        Animator animator = button.GetComponent<Animator>();

        if (animator != null)
        {
            animator.ResetTrigger(buttonPressTrigger);
            animator.SetTrigger(buttonPressTrigger);
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

    public void RefreshSelection()
    {
        StartCoroutine(RefreshSelectionNextFrame());
    }

    IEnumerator RefreshSelectionNextFrame()
    {
        yield return null;

        PanelEntry activePanel = GetActivePanel();
        if (activePanel == null)
            yield break;

        ForceSelectActivePanel(activePanel);
        lastActivePanel = activePanel;
    }

    void ForceSelectActivePanel(PanelEntry activePanel)
    {
        if (activePanel == null)
            return;

        SelectSelectable(activePanel.defaultSelectable, false);
        BlockInputTemporarily();
    }

    void SelectSelectable(Selectable selectable, bool playSound = false)
    {
        if (selectable == null || EventSystem.current == null)
            return;

        GameObject previousSelected = EventSystem.current.currentSelectedGameObject;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(selectable.gameObject);

        TMP_InputField tmpInput = selectable.GetComponent<TMP_InputField>();
        if (tmpInput != null)
        {
            tmpInput.ActivateInputField();
            tmpInput.MoveTextEnd(false);
        }

        if (playSound && previousSelected != selectable.gameObject)
            PlayNavigateSound();

        lastSelectedObject = selectable.gameObject;
    }

    void BlockInputTemporarily()
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
}