using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

[Serializable]
public class TaxiProgress
{
    public int version = 1;
    public bool hasRun;
    public int cash, day = 1, ridesCompleted;
    public float dayProgress;
    public Vector3 carPosition;
    public Quaternion carRotation = Quaternion.identity;
    public bool hasCarPosition;
    public bool tutorialCompleted, tutorialPurchased, tutorialDelivered, tutorialGrantGiven;
    public bool timeStopUsed;
    public List<string> upgrades = new List<string>();
    public List<int> removedGames = new List<int>();
    public List<FareBoostSave> fareBoosts = new List<FareBoostSave>();
    public int ridePassenger = -1, rideDropoff = -1, ridePhase;
    public List<string> shopItems = new List<string>();
    public List<bool> shopSold = new List<bool>();
}

[Serializable]
public class FareBoostSave
{
    public float amount;
    public int days;
}

public class SaveJSONData : MonoBehaviour
{
    private static TaxiProgress progress;
    private static bool ready;
    private static bool suppressSave;
    public static string ProgressPath => Path.Combine(Application.persistentDataPath, "TaxiProgress.json");
    public static bool Ready => ready;
    public static TaxiProgress Progress
    {
        get
        {
            if (progress != null) return progress;
            progress = ReadProgress(ProgressPath);
            return progress;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { progress = null; ready = false; suppressSave = false; }

    public static TaxiProgress ReadProgress(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var data = JsonUtility.FromJson<TaxiProgress>(File.ReadAllText(path));
                if (data != null && data.version == 1 && data.upgrades != null && data.removedGames != null && data.fareBoosts != null)
                    return data;
                throw new InvalidDataException("Unsupported or incomplete progress file.");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Could not load progress: " + e.Message);
            // Keep the damaged file for recovery rather than silently replacing it.
            try { File.Copy(path, path + ".unreadable-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss"), false); }
            catch (Exception copyError) { Debug.LogWarning(copyError.Message); }
        }
        return new TaxiProgress();
    }

    public static void WriteProgress(string path, TaxiProgress data)
    {
        string temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonUtility.ToJson(data, true));
        if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
        else File.Move(temporary, path);
    }

    public static void RestoreProgress(ShopManager shop)
    {
        ready = false;
        suppressSave = false;
        var data = Progress;
        if (data.hasRun)
        {
            MoneyManager.Instance?.RestoreProgress(data.cash, data.day);
            DayNightCycle.Instance?.RestoreProgress(data.day, data.dayProgress);
            if (CarController.Instance != null && data.hasCarPosition)
            {
                var car = CarController.Instance;
                car.transform.SetPositionAndRotation(data.carPosition, data.carRotation);
                var rb = car.GetComponent<Rigidbody>();
                if (rb != null) { rb.position = data.carPosition; rb.rotation = data.carRotation; rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            }
            var selector = PhoneAppManager.Instance != null ? PhoneAppManager.Instance.minigameSelector : null;
            if (selector != null) selector.RestoreRemovedGames(data.removedGames);
            shop.RestoreOwnedUpgrades(data.upgrades);
            TaxiRideManager.Instance?.RestoreFareBoosts(data.fareBoosts);
            TaxiRideManager.Instance?.RestoreRide(data.ridePassenger, data.rideDropoff, data.ridePhase);
            if (GameManager.Instance != null) GameManager.Instance.RestoreRideCount(data.ridesCompleted);
            if (TimeStopManager.Instance != null) TimeStopManager.Instance.usedThisDay = data.timeStopUsed;
        }
        ready = true;
    }

    public static void SaveProgress()
    {
        if (!ready || suppressSave || MoneyManager.Instance == null || ShopManager.Instance == null) return;
        var data = Progress;
        data.hasRun = true;
        data.cash = MoneyManager.Instance.currentCash; data.day = MoneyManager.Instance.currentDay;
        if (DayNightCycle.Instance != null) data.dayProgress = DayNightCycle.Instance.DayProgress01;
        if (GameManager.Instance != null) data.ridesCompleted = GameManager.Instance.ridesCompleted;
        if (CarController.Instance != null)
        {
            data.hasCarPosition = true;
            data.carPosition = CarController.Instance.transform.position;
            data.carRotation = CarController.Instance.transform.rotation;
        }
        data.upgrades.Clear();
        foreach (var upgrade in ShopManager.Instance.purchasedUpgrades)
            if (upgrade != null) data.upgrades.Add(upgrade.name);
        if (TaxiRideManager.Instance != null)
        {
            data.fareBoosts = TaxiRideManager.Instance.CaptureFareBoosts();
            TaxiRideManager.Instance.CaptureRide(out data.ridePassenger, out data.rideDropoff, out data.ridePhase);
        }
        var selector = PhoneAppManager.Instance != null ? PhoneAppManager.Instance.minigameSelector : null;
        if (selector != null) data.removedGames = selector.CaptureRemovedGames();
        if (TimeStopManager.Instance != null) data.timeStopUsed = TimeStopManager.Instance.usedThisDay;
        data.shopItems = new List<string>(); data.shopSold = new List<bool>();
        if (ShopManager.Instance.slots != null)
            foreach (var slot in ShopManager.Instance.slots)
            {
                data.shopItems.Add(slot != null && slot.currentUpgrade != null ? slot.currentUpgrade.name : "");
                data.shopSold.Add(slot != null && slot.IsPurchased);
            }
        try { WriteProgress(ProgressPath, data); }
        catch (Exception e) { Debug.LogError("Could not save progress: " + e.Message); }
    }

    public static void NewGame()
    {
        // Keep first-time tutorial completion; reset the economy and all upgrades.
        bool completed = Progress.tutorialCompleted;
        progress = new TaxiProgress { tutorialCompleted = completed };
        ready = false; suppressSave = true;
        try { WriteProgress(ProgressPath, progress); }
        catch (Exception e) { Debug.LogError("Could not save new game: " + e.Message); }
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    public void SaveDataNow()
    {
        SaveProgress();
        if (GameManager.Instance == null) return;
        string path = Path.Combine(Application.persistentDataPath, "TaxiRun_" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".json");
        try { File.WriteAllText(path, JsonUtility.ToJson(GameManager.Instance.CreateSaveData(), true)); }
        catch (Exception e) { Debug.LogError("Could not export run: " + e.Message); }
    }
    void OnApplicationQuit() { SaveProgress(); }
    void OnApplicationPause(bool paused) { if (paused) SaveProgress(); }
}
