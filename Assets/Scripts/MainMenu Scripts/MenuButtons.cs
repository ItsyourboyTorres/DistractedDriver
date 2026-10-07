using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuButtons : MonoBehaviour
{
    public void PlayGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("GameScene");
    }

    public void HowToPlay()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("HowToPlay");
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
        Debug.Log("Game Quit");
    }
    private TMPro.TMP_Text newGameLabel;
    private float confirmUntil;
    void Start()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu") return;
        UnityEngine.UI.Button play = null;
        foreach (var button in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (button.name == "PlayButton") { play = button; break; }
        if (play == null) return;
        var parent = play.transform.parent;
        var newGame = Instantiate(play, parent);
        newGame.name = "NewGameButton";
        newGame.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        newGame.onClick.AddListener(ConfirmNewGame);
        newGameLabel = newGame.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (newGameLabel != null)
        {
            newGameLabel.fontSizeMax = newGameLabel.fontSize;
            newGameLabel.fontSizeMin = newGameLabel.fontSize * 0.6f;
            newGameLabel.enableAutoSizing = true;
            newGameLabel.text = "NEW GAME";
        }
        SetMenuPosition(play.transform, 22f);
        SetMenuPosition(newGame.transform, 7f);
        var how = parent.Find("HowToPlayButton");
        var quit = parent.Find("QuitButton");
        if (how != null) SetMenuPosition(how, -8f);
        if (quit != null) SetMenuPosition(quit, -23f);
    }
    static void SetMenuPosition(Transform target, float y)
    {
        var rect = target as RectTransform;
        if (rect != null) rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
    }
    void Update()
    {
        if (confirmUntil > 0 && Time.unscaledTime > confirmUntil)
        {
            confirmUntil = 0;
            if (newGameLabel != null) newGameLabel.text = "NEW GAME";
        }
    }
    public void ConfirmNewGame()
    {
        if (!SaveJSONData.Progress.hasRun || (confirmUntil > 0 && Time.unscaledTime <= confirmUntil))
        { SaveJSONData.NewGame(); return; }
        confirmUntil = Time.unscaledTime + 4f;
        if (newGameLabel != null) newGameLabel.text = "RESET SAVE?";
    }

}
