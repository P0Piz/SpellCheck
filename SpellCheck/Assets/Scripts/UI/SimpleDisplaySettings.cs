using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SimpleDisplaySettings : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown screenModeDropdown;

    private Resolution[] resolutions;
    private List<Resolution> filteredResolutions = new List<Resolution>();

    void Start()
    {
        SetupResolutionDropdown();
        SetupScreenModeDropdown();
    }

    void SetupResolutionDropdown()
    {
        resolutions = Screen.resolutions;
        filteredResolutions.Clear();
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        HashSet<string> added = new HashSet<string>();

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;

            if (added.Contains(option))
                continue;

            added.Add(option);
            filteredResolutions.Add(resolutions[i]);
            options.Add(option);

            if (Screen.currentResolution.width == resolutions[i].width &&
                Screen.currentResolution.height == resolutions[i].height)
            {
                currentResolutionIndex = filteredResolutions.Count - 1;
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    void SetupScreenModeDropdown()
    {
        screenModeDropdown.ClearOptions();

        List<string> options = new List<string>()
        {
            "Fullscreen",
            "Windowed"
        };

        screenModeDropdown.AddOptions(options);

        if (Screen.fullScreenMode == FullScreenMode.Windowed)
            screenModeDropdown.value = 1;
        else
            screenModeDropdown.value = 0;

        screenModeDropdown.RefreshShownValue();
        screenModeDropdown.onValueChanged.AddListener(SetScreenMode);
    }

    public void SetResolution(int resolutionIndex)
    {
        if (resolutionIndex < 0 || resolutionIndex >= filteredResolutions.Count)
            return;

        Resolution resolution = filteredResolutions[resolutionIndex];

        bool isFullscreen = Screen.fullScreenMode != FullScreenMode.Windowed;

        if (isFullscreen)
        {
            Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.Windowed);
        }
    }

    public void SetScreenMode(int modeIndex)
    {
        FullScreenMode mode = FullScreenMode.FullScreenWindow;

        if (modeIndex == 1)
            mode = FullScreenMode.Windowed;

        Screen.fullScreenMode = mode;

        int currentResolutionIndex = resolutionDropdown.value;
        if (currentResolutionIndex >= 0 && currentResolutionIndex < filteredResolutions.Count)
        {
            Resolution resolution = filteredResolutions[currentResolutionIndex];
            Screen.SetResolution(resolution.width, resolution.height, mode);
        }
    }
}