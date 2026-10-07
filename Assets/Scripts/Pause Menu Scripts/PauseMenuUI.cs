using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuUI : MonoBehaviour
{
    public static PauseMenuUI Instance { get; private set; }

    [Header("Root")]
    public GameObject pauseMenuRoot;

    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject optionsPanel;
    public GameObject upgradesPanel;
    public GameObject mainMenuConfirmPanel;

    [Header("Scene")]
    public string mainMenuSceneName = "MainMenu";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        HidePauseMenuImmediate();
    }

    public void ShowPauseMenu()
    {
        SaveJSONData.SaveProgress();
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(true);

        ShowMainPanel();
    }

    public void HidePauseMenuImmediate()
    {
        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);
    }

    public void ShowMainPanel()
    {
        SetOnlyPanelActive(mainPanel);
    }

    public void ShowOptionsPanel()
    {
        SetOnlyPanelActive(optionsPanel);
    }

    public void ShowUpgradesPanel()
    {
        SetOnlyPanelActive(upgradesPanel);
    }

    public void ShowMainMenuConfirmPanel()
    {
        SetOnlyPanelActive(mainMenuConfirmPanel);
    }

    public void ResumeGame()
    {
        if (PauseAudioManager.Instance != null)
        {
            PauseAudioManager.Instance.SetPaused(false);
            return;
        }

        Time.timeScale = 1f;
        HidePauseMenuImmediate();
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SaveJSONData.SaveProgress();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void SetOnlyPanelActive(GameObject target)
    {
        if (mainPanel != null)
            mainPanel.SetActive(target == mainPanel);

        if (optionsPanel != null)
            optionsPanel.SetActive(target == optionsPanel);

        if (upgradesPanel != null)
            upgradesPanel.SetActive(target == upgradesPanel);

        if (mainMenuConfirmPanel != null)
            mainMenuConfirmPanel.SetActive(target == mainMenuConfirmPanel);
    }
}