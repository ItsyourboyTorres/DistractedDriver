using UnityEngine;
using UnityEngine.VFX;
using TMPro;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Day Timer Display")]
    public TMP_Text timerText;

    private bool timeStopPaused = false;

    [Header("Score")]
    public TMP_Text scoreText;
    public int scorePerRide = 100;
    private int score = 0;

    [Header("Ride Stats")]
    public int ridesCompleted = 0;

    [Header("Saving")]
    public SaveJSONData saveSystem;

    [Header("Driving Stats")]
    public Transform player;
    public float totalDistanceDriven = 0f;
    private Vector3 lastPosition;
    public float averageSpeed = 0f;

    [Header("Optional End Screen (leave assigned only if you still want to use it later)")]
    public GameObject endScreenPanel;
    public TMP_Text finalScoreText;

    [Header("Optional Win VFX")]
    public VisualEffect confettiVFX;
    public float confettiPlaySeconds = 2f;

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
        UpdateTimerUI();
        UpdateScoreUI();

        if (endScreenPanel != null)
            endScreenPanel.SetActive(false);

        Time.timeScale = 1f;

        if (player != null)
            lastPosition = player.position;
    }

    private void Update()
    {
        UpdateTimerUI();

        // keep tracking car movement even during time stop
        if (player != null)
        {
            float frameDistance = Vector3.Distance(player.position, lastPosition);
            totalDistanceDriven += frameDistance;
            lastPosition = player.position;
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;
        if (DayNightCycle.Instance == null) return;

        float timeRemaining = GetDayTimeRemaining();

        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);

        timerText.text = $"{minutes:0}:{seconds:00}";
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + score;
    }

    public void AddRideScore()
    {
        score += scorePerRide;
        ridesCompleted++;

        if (ScorePopupUI.Instance != null)
            ScorePopupUI.Instance.ShowPopup(scorePerRide);

        Debug.Log("Total Score: " + score + " | Rides: " + ridesCompleted);
        UpdateScoreUI();
    }

    public void SetTimeStopPaused(bool paused)
    {
        timeStopPaused = paused;
    }

    public float GetDayTimeRemaining()
    {
        if (DayNightCycle.Instance == null)
            return 0f;

        float totalDayLength = DayNightCycle.Instance.dayLengthSeconds;
        float progress01 = DayNightCycle.Instance.DayProgress01;
        float remaining = totalDayLength * (1f - progress01);

        return Mathf.Max(0f, remaining);
    }

    public float GetTimePlayed()
    {
        if (DayNightCycle.Instance == null)
            return 0f;

        float totalDayLength = DayNightCycle.Instance.dayLengthSeconds;
        return totalDayLength - GetDayTimeRemaining();
    }

    public void RefreshDrivingStats()
    {
        float timePlayed = GetTimePlayed();
        averageSpeed = (timePlayed > 0f) ? (totalDistanceDriven / timePlayed) : 0f;

        Debug.Log($"Driving stats: distance={totalDistanceDriven:F1}m, avgSpeed={averageSpeed:F2} m/s");
    }

    public void SaveRunDataNow()
    {
        RefreshDrivingStats();

        if (saveSystem != null)
            saveSystem.SaveDataNow();
        else
            Debug.LogWarning("GameManager: saveSystem is null, not saving.");
    }

    public void ShowLegacyEndScreenIfNeeded()
    {
        RefreshDrivingStats();

        if (endScreenPanel != null)
            endScreenPanel.SetActive(true);

        if (finalScoreText != null)
            finalScoreText.text = "Score: " + score;

        if (confettiVFX != null)
            confettiVFX.Play();
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SaveJSONData.NewGame();
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SaveJSONData.SaveProgress();
        SceneManager.LoadScene("MainMenu");
    }

    public TaxiGameData CreateSaveData()
    {
        RefreshDrivingStats();

        return new TaxiGameData(
            score,
            ridesCompleted,
            GetTimePlayed(),
            averageSpeed
        );
    }
    public void RestoreRideCount(int count)
    {
        ridesCompleted = Mathf.Max(0, count);
        score = ridesCompleted * scorePerRide;
        UpdateScoreUI();
    }

}