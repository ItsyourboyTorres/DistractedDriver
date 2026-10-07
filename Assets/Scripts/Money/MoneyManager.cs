using System;
using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [Header("Starting Values")]
    public int startingDay = 1;
    public int startingCash = 0;

    [Header("Debt Scaling")]
    public int day1Debt = 18;
    public float debtGrowthMultiplier = 1.22f;

    [Header("Fare Scaling")]
    public int baseFareDay1 = 2;
    public int fareRandomBonusMax = 1;
    public float fareIncreasePerDay = 0.35f;

    [Header("Money Multipliers")]
    public float fareMultiplier = 1f;

    [Header("Runtime")]
    public int currentDay;
    public int currentCash;
    public int currentDebtGoal;

    public event Action<int> OnCashAdded;
    public event Action<int> OnCashChanged;

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
        currentDay = startingDay;
        currentCash = startingCash;
        currentDebtGoal = GetDebtForDay(currentDay);

        OnCashChanged?.Invoke(currentCash);
    }

    public void StartNextDay()
    {
        currentDay++;
        currentDebtGoal = GetDebtForDay(currentDay);
    }

    public void StartSpecificDay(int day)
    {
        currentDay = Mathf.Max(1, day);
        currentDebtGoal = GetDebtForDay(currentDay);
    }

    public int GetDebtForDay(int day)
    {
        return Mathf.RoundToInt(day1Debt * Mathf.Pow(debtGrowthMultiplier, day - 1));
    }

    public int GetBaseFareForDay(int day)
    {
        float fare = baseFareDay1 + ((day - 1) * fareIncreasePerDay);
        return Mathf.RoundToInt(fare);
    }

    public int GetRandomRideFare()
    {
        float baseFare = GetBaseFareForDay(currentDay);

        // Random.Range int version is min inclusive, max exclusive,
        // so +1 makes the max actually reachable.
        int randomBonus = UnityEngine.Random.Range(0, fareRandomBonusMax + 1);

        float fare = (baseFare + randomBonus) * fareMultiplier;
        return Mathf.RoundToInt(fare);
    }

    public int AddRideFare()
    {
        int fare = GetRandomRideFare();
        AddCash(fare);
        Debug.Log("[Money] Ride earned: $" + fare + " | Cash now: $" + currentCash);
        return fare;
    }

    public void AddCash(int amount)
    {
        currentCash += amount;

        if (amount > 0)
            OnCashAdded?.Invoke(amount);

        OnCashChanged?.Invoke(currentCash);
    }

    public bool SpendCash(int amount)
    {
        if (amount < 0 || currentCash < amount)
            return false;

        currentCash -= amount;
        OnCashChanged?.Invoke(currentCash);
        return true;
    }

    public bool HasMetDebtGoal()
    {
        return currentCash >= currentDebtGoal;
    }

    public bool TryPayDebtForDay()
    {
        if (currentCash < currentDebtGoal)
        {
            Debug.Log("[Money] Not enough cash to pay debt. Need $" + currentDebtGoal + ", have $" + currentCash);
            return false;
        }

        currentCash -= currentDebtGoal;
        OnCashChanged?.Invoke(currentCash);

        Debug.Log("[Money] Paid debt: $" + currentDebtGoal + " | Remaining cash: $" + currentCash);
        return true;
    }

    public int GetScaledUpgradePrice(int basePrice)
    {
        return Mathf.RoundToInt(basePrice * Mathf.Pow(1.18f, currentDay - 1));
    }

    public int GetScaledUpgradePriceForDay(int basePrice, int day)
    {
        return Mathf.RoundToInt(basePrice * Mathf.Pow(1.18f, day - 1));
    }

    public void AddFareMultiplier(float amount)
    {
        fareMultiplier += amount;
        fareMultiplier = Mathf.Max(0.1f, fareMultiplier);
        Debug.Log("[Money] Fare multiplier now: " + fareMultiplier.ToString("0.00") + "x");
    }

    public void SetFareMultiplier(float amount)
    {
        fareMultiplier = Mathf.Max(0.1f, amount);
        Debug.Log("[Money] Fare multiplier set to: " + fareMultiplier.ToString("0.00") + "x");
    }
    public void RestoreProgress(int cash, int day)
    {
        currentCash = Mathf.Max(0, cash);
        StartSpecificDay(day);
        OnCashChanged?.Invoke(currentCash);
    }

}