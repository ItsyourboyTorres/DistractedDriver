using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class DebtFailedPanelUI : MonoBehaviour
{
    [Header("Main")]
    public GameObject root;
    public CanvasGroup canvasGroup;

    [Header("UI")]
    public TMP_Text failText;
    public Button tryAgainButton;
    public Button quitButton;

    [Header("Fade")]
    public float fadeDuration = 1f;
    public float textDelay = 0.35f;
    public float textFadeDuration = 0.4f;

    [Header("Audio")]
    public AudioSource uiAudioSource;
    public AudioClip swooshClip;

    [Header("Scene")]
    public string mainMenuSceneName = "MainMenu";

    private bool showing = false;

    private CanvasGroup failTextGroup;
    private CanvasGroup tryAgainGroup;
    private CanvasGroup quitGroup;

    private void Start()
    {
        if (root != null)
            root.SetActive(false);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        failTextGroup = GetOrAddCanvasGroup(failText != null ? failText.gameObject : null);
        tryAgainGroup = GetOrAddCanvasGroup(tryAgainButton != null ? tryAgainButton.gameObject : null);
        quitGroup = GetOrAddCanvasGroup(quitButton != null ? quitButton.gameObject : null);

        SetGroupAlpha(failTextGroup, 0f);
        SetGroupAlpha(tryAgainGroup, 0f);
        SetGroupAlpha(quitGroup, 0f);
    }

    public void Show()
    {
        if (showing)
            return;

        showing = true;

        if (WorldAmbienceManager.Instance != null)
            WorldAmbienceManager.Instance.FadeOutAndStopAllAmbience();

        if (root != null)
            root.SetActive(true);

        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float t = 0f;

        if (canvasGroup == null)
            yield break;

        SetGroupAlpha(failTextGroup, 0f);
        SetGroupAlpha(tryAgainGroup, 0f);
        SetGroupAlpha(quitGroup, 0f);

        if (uiAudioSource != null && swooshClip != null)
            uiAudioSource.PlayOneShot(swooshClip);

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;

        yield return new WaitForSecondsRealtime(textDelay);

        yield return StartCoroutine(FadeCanvasGroup(failTextGroup, 0f, 1f, textFadeDuration));
        yield return StartCoroutine(FadeCanvasGroup(tryAgainGroup, 0f, 1f, textFadeDuration));
        yield return StartCoroutine(FadeCanvasGroup(quitGroup, 0f, 1f, textFadeDuration));

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        Time.timeScale = 0f;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float start, float end, float duration)
    {
        if (group == null)
            yield break;

        float t = 0f;
        group.alpha = start;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, end, t / duration);
            yield return null;
        }

        group.alpha = end;
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        if (obj == null)
            return null;

        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = obj.AddComponent<CanvasGroup>();

        return cg;
    }

    private void SetGroupAlpha(CanvasGroup group, float alpha)
    {
        if (group != null)
            group.alpha = alpha;
    }

    public void OnTryAgainPressed()
    {
        Time.timeScale = 1f;
        SaveJSONData.NewGame();
    }

    public void OnQuitPressed()
    {
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Application.Quit();
            Debug.Log("Quit Game pressed.");
        }
    }
}