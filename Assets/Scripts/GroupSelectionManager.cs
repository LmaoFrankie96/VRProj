using UnityEngine;
using UnityEngine.UI;

public class GroupSelectionManager : MonoBehaviour
{
    [Header("UI Groups")]
    public GameObject groupSelectionUI; // Parent object of group selection UI
    public GameObject mainUI; // Parent object of main UI

    [Header("Group Selection Buttons")]
    public Button group1Button;
    public Button group2Button;
    public Button group3Button;

    [Header("Main UI Buttons")]
    public Button[] group1Buttons; // Buttons only for Group 1
    public Button[] group2Buttons; // Buttons only for Group 2
    public Button[] group3Buttons; // Buttons only for Group 3
    public Button[] commonButtons; // Buttons for all groups

    private const string GROUP_PREF_KEY = "SelectedGroup";
    private int selectedGroup = 0;

    void Start()
    {
        // Check if group was already selected in a previous session
        if (PlayerPrefs.HasKey(GROUP_PREF_KEY))
        {
            selectedGroup = PlayerPrefs.GetInt(GROUP_PREF_KEY);
            ShowMainUI();
            UpdateButtonVisibility();
            Debug.Log($"Loaded previous group selection: {selectedGroup}");
        }
        else
        {
            // Initialize UI states if no group was selected before
            ShowGroupSelection();
        }

        // Set up button listeners
        group1Button.onClick.AddListener(() => SetGroup(1));
        group2Button.onClick.AddListener(() => SetGroup(2));
        group3Button.onClick.AddListener(() => SetGroup(3));
    }

    void ShowGroupSelection()
    {
        groupSelectionUI.SetActive(true);
        mainUI.SetActive(false);
    }

    void ShowMainUI()
    {
        groupSelectionUI.SetActive(false);
        mainUI.SetActive(true);
    }

    void SetGroup(int groupNumber)
    {
        selectedGroup = groupNumber;

        // Save the selection to PlayerPrefs
        PlayerPrefs.SetInt(GROUP_PREF_KEY, selectedGroup);
        PlayerPrefs.Save(); // Immediately save to disk

        ShowMainUI();
        UpdateButtonVisibility();
        Debug.Log($"Group {groupNumber} selected and saved");
    }

    void UpdateButtonVisibility()
    {
        // Deactivate all group-specific buttons first
        SetButtonsActive(group1Buttons, false);
        SetButtonsActive(group2Buttons, false);
        SetButtonsActive(group3Buttons, false);

        // Activate buttons for selected group
        switch (selectedGroup)
        {
            case 1: SetButtonsActive(group1Buttons, true); break;
            case 2: SetButtonsActive(group2Buttons, true); break;
            case 3: SetButtonsActive(group3Buttons, true); break;
        }

        // Always activate common buttons
        SetButtonsActive(commonButtons, true);
    }

    void SetButtonsActive(Button[] buttons, bool active)
    {
        foreach (Button button in buttons)
        {
            if (button != null)
            {
                button.gameObject.SetActive(active);
            }
        }
    }

    // Optional: Add method to reset and show group selection again
    public void ResetGroupSelection()
    {
        selectedGroup = 0;
        PlayerPrefs.DeleteKey(GROUP_PREF_KEY); // Remove saved group
        ShowGroupSelection();
    }
}