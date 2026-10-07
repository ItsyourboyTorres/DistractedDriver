using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.VFX;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    [Header("Day Length")]
    public float dayLengthSeconds = 300f; // 5 minutes

    [Header("Sun Orbit")]
    public Transform sunPivot;
    public float sunriseAngle = 15f;
    public float sunsetAngle = 165f;
    public float compassYaw = 0f;

    [Header("Sun Light")]
    public Light sunLight;
    public Gradient sunColorOverDay;
    public AnimationCurve sunIntensityOverDay = AnimationCurve.Linear(0f, 0.3f, 1f, 1f);

    [Header("Pre-Fade Day End FX")]
    public VisualEffect inCarWinVFX;
    public AudioSource inCarWinAudioSource;
    public float preFadeDelay = 2f;

    [Header("Day Over UI")]
    public GameObject dayOverPanel;
    public Image dayOverBackground;
    public TMP_Text dayOverText;
    public float fadeDuration = 3f;

    [Header("Day Count")]
    public int currentDay = 1;

    [Header("Music Fade Before End")]
    public float musicFadeBeforeEndSeconds = 3f;

    private float dayTimer = 0f;
    private bool dayEnded = false;
    private bool isFading = false;
    private float fadeTimer = 0f;
    private bool endingSequenceStarted = false;
    private bool timeStopPaused = false;
    private bool resolvedAfterFade = false;
    private bool endMusicFadeStarted = false;

    public float DayProgress01 => Mathf.Clamp01(dayTimer / dayLengthSeconds);
    public bool DayEnded => dayEnded;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        if (dayOverPanel != null)
            dayOverPanel.SetActive(false);

        SetDayOverAlpha(0f);
        ApplySun(0f);

        if (inCarWinVFX != null)
            inCarWinVFX.gameObject.SetActive(false);

        if (inCarWinAudioSource != null)
            inCarWinAudioSource.Stop();
    }

    void Update()
    {
        if (!dayEnded)
        {
            if (!timeStopPaused && !(FirstDriveTutorial.Instance != null && FirstDriveTutorial.Instance.IsRunning))
            {
                dayTimer += Time.deltaTime;

                float timeRemaining = dayLengthSeconds - dayTimer;

                if (!endMusicFadeStarted && timeRemaining <= musicFadeBeforeEndSeconds)
                {
                    endMusicFadeStarted = true;

                    if (WorldMusicManager.Instance != null)
                        WorldMusicManager.Instance.FadeOutAndStopMusic();
                }

                if (dayTimer >= dayLengthSeconds && !endingSequenceStarted)
                {
                    dayTimer = dayLengthSeconds;
                    StartCoroutine(EndDaySequence());
                }

                ApplySun(DayProgress01);
            }
        }
        else if (isFading)
        {
            if (timeStopPaused)
                return;

            fadeTimer += Time.deltaTime;
            float t = Mathf.Clamp01(fadeTimer / fadeDuration);

            SetDayOverAlpha(t);

            if (t >= 1f)
            {
                isFading = false;
                Debug.Log("[DayNight] Fade complete.");

                if (!resolvedAfterFade)
                {
                    resolvedAfterFade = true;

                    if (DayEndManager.Instance != null)
                        DayEndManager.Instance.ResolveDayEnd();
                }
            }
        }
    }

    void ApplySun(float t)
    {
        if (sunPivot != null)
        {
            float angle = Mathf.Lerp(sunriseAngle, sunsetAngle, t);
            sunPivot.rotation = Quaternion.Euler(angle, compassYaw, 0f);
        }

        if (sunLight != null)
        {
            if (sunColorOverDay != null)
                sunLight.color = sunColorOverDay.Evaluate(t);

            if (sunIntensityOverDay != null)
                sunLight.intensity = sunIntensityOverDay.Evaluate(t);
        }
    }

    IEnumerator EndDaySequence()
    {
        if (endingSequenceStarted)
            yield break;

        endingSequenceStarted = true;
        dayEnded = true;

        Debug.Log("[DayNight] Day ended. Playing in-car FX.");

        if (inCarWinVFX != null)
        {
            inCarWinVFX.gameObject.SetActive(true);
            inCarWinVFX.Reinit();
            inCarWinVFX.Play();
        }

        if (inCarWinAudioSource != null)
        {
            inCarWinAudioSource.Stop();
            inCarWinAudioSource.Play();
        }

        yield return new WaitForSeconds(preFadeDelay);

        if (dayOverPanel != null)
            dayOverPanel.SetActive(true);

        if (dayOverText != null)
            dayOverText.text = "Day " + currentDay + " Over";

        fadeTimer = 0f;
        isFading = true;
        SetDayOverAlpha(0f);

        Debug.Log("[DayNight] Starting fade to black.");
    }

    void SetDayOverAlpha(float alpha)
    {
        if (dayOverBackground != null)
        {
            Color bg = dayOverBackground.color;
            bg.a = alpha;
            dayOverBackground.color = bg;
        }

        if (dayOverText != null)
        {
            Color txt = dayOverText.color;
            txt.a = alpha;
            dayOverText.color = txt;
        }
    }

    public void ResetDay()
    {
        dayTimer = 0f;
        dayEnded = false;
        isFading = false;
        fadeTimer = 0f;
        endingSequenceStarted = false;
        timeStopPaused = false;
        resolvedAfterFade = false;
        endMusicFadeStarted = false;

        currentDay++;

        if (dayOverPanel != null)
            dayOverPanel.SetActive(false);

        SetDayOverAlpha(0f);
        ApplySun(0f);

        if (inCarWinVFX != null)
        {
            inCarWinVFX.Stop();
            inCarWinVFX.gameObject.SetActive(false);
        }

        if (inCarWinAudioSource != null)
            inCarWinAudioSource.Stop();

        Debug.Log("[DayNight] Day reset. Current day is now " + currentDay);
    }

    public void SetTimeStopPaused(bool paused)
    {
        timeStopPaused = paused;
    }
    public void RestoreProgress(int day, float progress)
    {
        currentDay = Mathf.Max(1, day);
        dayTimer = Mathf.Clamp01(progress) * dayLengthSeconds;
        ApplySun(DayProgress01);
    }

}