using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class TimeStopManager : MonoBehaviour
{
    public static TimeStopManager Instance { get; private set; }

    [Header("Unlock / Uses")]
    public PlayerUpgradeState playerUpgradeState;
    public bool requireUpgrade = true;
    public bool oneUsePerDay = true;
    public bool usedThisDay = false;

    [Header("Input")]
    public KeyCode activateKey = KeyCode.E;

    [Header("Duration")]
    public float stopDuration = 10f;

    [Header("References")]
    public DayNightCycle dayNightCycle;
    public Transform playerCar;
    public TimeStopShockwave shockwavePrefab;
    public TimeStopVisualController visualController;

    [Header("Audio")]
    public AudioSource oneShotSource;
    public AudioSource loopSource;
    public AudioClip activationClip;
    public AudioClip stoppedLoopClip;

    [Header("Optional Snapshot")]
    public AudioMixerSnapshot normalSnapshot;
    public AudioMixerSnapshot timeStopSnapshot;
    public float snapshotTransition = 0.08f;

    [Header("State")]
    public bool isTimeStopped = false;

    private Coroutine stopRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (Input.GetKeyDown(activateKey))
            TryActivate();
    }

    public void TryActivate()
    {
        if (isTimeStopped)
            return;

        if (oneUsePerDay && usedThisDay)
            return;

        if (Time.timeScale == 0f) return;
        if (playerUpgradeState == null) playerUpgradeState = PlayerUpgradeState.Instance;
        if (requireUpgrade && (playerUpgradeState == null || !playerUpgradeState.hasTimeStop))
            return;

        stopRoutine = StartCoroutine(TimeStopRoutine());
    }

    private IEnumerator TimeStopRoutine()
    {
        isTimeStopped = true;
        usedThisDay = true;
        SaveJSONData.SaveProgress();

        Debug.Log("[TimeStop] ACTIVATED");

        if (dayNightCycle != null)
            dayNightCycle.SetTimeStopPaused(true);

        if (timeStopSnapshot != null)
            timeStopSnapshot.TransitionTo(snapshotTransition);

        if (oneShotSource != null && activationClip != null)
            oneShotSource.PlayOneShot(activationClip);

        if (visualController != null)
            visualController.BeginTimeStop();

        if (shockwavePrefab != null && playerCar != null)
        {
            TimeStopShockwave wave = Instantiate(
                shockwavePrefab,
                playerCar.position,
                Quaternion.identity
            );

            wave.Play();
        }

        if (loopSource != null && stoppedLoopClip != null)
        {
            loopSource.clip = stoppedLoopClip;
            loopSource.loop = true;
            loopSource.Play();
        }

        yield return new WaitForSeconds(stopDuration);

        if (loopSource != null)
            loopSource.Stop();

        if (visualController != null)
            visualController.EndTimeStop();

        if (normalSnapshot != null)
            normalSnapshot.TransitionTo(snapshotTransition);

        if (dayNightCycle != null)
            dayNightCycle.SetTimeStopPaused(false);

        isTimeStopped = false;
        stopRoutine = null;

        Debug.Log("[TimeStop] ENDED");
    }

    public void ResetForNewDay()
    {
        usedThisDay = false;
    }
}