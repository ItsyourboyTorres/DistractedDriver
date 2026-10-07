using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Shop Slots")]
    public ShopUpgradeSlot[] slots;

    [Header("Upgrade Pools")]
    public List<ShopUpgradeData> commonUpgrades = new List<ShopUpgradeData>();
    public List<ShopUpgradeData> uncommonUpgrades = new List<ShopUpgradeData>();
    public List<ShopUpgradeData> rareUpgrades = new List<ShopUpgradeData>();
    public List<ShopUpgradeData> epicUpgrades = new List<ShopUpgradeData>();
    public List<ShopUpgradeData> legendaryUpgrades = new List<ShopUpgradeData>();

    [Header("Rarity Chances")]
    [Range(0, 100)] public float commonChance = 40f;
    [Range(0, 100)] public float uncommonChance = 30f;
    [Range(0, 100)] public float rareChance = 15f;
    [Range(0, 100)] public float epicChance = 10f;
    [Range(0, 100)] public float legendaryChance = 5f;

    [Header("Safety")]
    public int maxAttemptsPerSlot = 50;

    [Header("Purchased Upgrades")]
    public List<ShopUpgradeData> purchasedUpgrades = new List<ShopUpgradeData>();

    void Awake()
    {
        Instance = this;
    }

    System.Collections.IEnumerator Start()
    {
        // Other managers establish their base stats in Start before restoration.
        yield return null;
        SaveJSONData.RestoreProgress(this);
        if (!RestoreShop()) GenerateShop();
        if (ShopHoverUI.Instance != null)
        {
            var tutorial = ShopHoverUI.Instance.GetComponent<FirstDriveTutorial>();
            if (tutorial == null) tutorial = ShopHoverUI.Instance.gameObject.AddComponent<FirstDriveTutorial>();
            tutorial.PrepareStarterPurchase();
        }
        SaveJSONData.SaveProgress();
    }

    public void GenerateShop()
    {
        if (slots == null || slots.Length == 0) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].ClearSlot();
        }

        List<ShopUpgradeData> usedUpgrades = new List<ShopUpgradeData>();

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            ShopUpgradeData rolledUpgrade = RollUniqueUpgrade(usedUpgrades);

            if (rolledUpgrade != null)
            {
                slots[i].SetUpgrade(rolledUpgrade);

                if (!rolledUpgrade.allowDuplicateInShop)
                    usedUpgrades.Add(rolledUpgrade);
            }
            else
            {
                slots[i].ClearSlot();
                Debug.LogWarning("Could not find a valid upgrade for slot " + i);
            }
        }

        if (ShopHoverUI.Instance != null)
            ShopHoverUI.Instance.Hide();
    }

    public bool PurchaseUpgrade(ShopUpgradeData upgrade)
    {
        if (!CanPurchase(upgrade, out string reason)) return false;
        ShopUpgradeData copied = null;
        if (upgrade.upgradeType == ShopUpgradeType.Joker)
        {
            var candidates = purchasedUpgrades.FindAll(CanCopy);
            if (candidates.Count == 0) return false;
            copied = candidates[Random.Range(0, candidates.Count)];
        }
        int price = MoneyManager.Instance.GetScaledUpgradePrice(upgrade.basePrice);
        if (!MoneyManager.Instance.SpendCash(price)) return false;
        purchasedUpgrades.Add(upgrade);
        if (copied != null)
        {
            // Copy the effect and record it for saving, without a second charge.
            purchasedUpgrades.Add(copied);
            ApplyUpgradeEffect(copied);
        }
        else ApplyUpgradeEffect(upgrade);
        SaveJSONData.Progress.tutorialPurchased = true;
        SaveJSONData.SaveProgress();
        return true;
    }

    public bool CanPurchase(ShopUpgradeData upgrade, out string reason)
    {
        reason = "";
        if (upgrade == null) { reason = "NO UPGRADE"; return false; }
        if (!upgrade.canBuyMultiple && purchasedUpgrades.Contains(upgrade)) { reason = "OWNED"; return false; }
        if (!EffectAvailable(upgrade))
        {
            reason = upgrade.upgradeType == ShopUpgradeType.RemoveMinigame ? "NEEDS TWO PLAYABLE GAMES" :
                upgrade.upgradeType == ShopUpgradeType.Joker ? "BUY A STACKABLE UPGRADE FIRST" : "UNAVAILABLE";
            return false;
        }
        if (MoneyManager.Instance == null || MoneyManager.Instance.currentCash < MoneyManager.Instance.GetScaledUpgradePrice(upgrade.basePrice))
        { reason = "NOT ENOUGH CASH"; return false; }
        return true;
    }

    bool CanCopy(ShopUpgradeData u)
    {
        return u != null && u.canBuyMultiple && u.upgradeType != ShopUpgradeType.Joker &&
            u.upgradeType != ShopUpgradeType.RemoveMinigame && EffectAvailable(u);
    }

    public bool EffectAvailable(ShopUpgradeData u)
    {
        if (u == null || u.basePrice < 0) return false;
        switch (u.upgradeType)
        {
            case ShopUpgradeType.CarSpeed: return u.value > 0 && CarController.Instance != null;
            case ShopUpgradeType.DopamineMax: return u.value > 0 && DopamineManager.Instance != null;
            case ShopUpgradeType.RideFareMultiplier: return u.value > 0 && TaxiRideManager.Instance != null;
            case ShopUpgradeType.SurgePricing: return u.value > 0 && u.durationDays > 0 && TaxiRideManager.Instance != null;
            case ShopUpgradeType.TimeStopUnlock: return PlayerUpgradeState.Instance != null && TimeStopManager.Instance != null;
            case ShopUpgradeType.RemoveMinigame:
                return PhoneAppManager.Instance != null && PhoneAppManager.Instance.minigameSelector != null &&
                    PhoneAppManager.Instance.minigameSelector.CanRemoveGame;
            case ShopUpgradeType.Joker: return purchasedUpgrades.Exists(CanCopy);
            default: return false;
        }
    }

    public void RestoreOwnedUpgrades(List<string> ids)
    {
        purchasedUpgrades.Clear();
        var all = GetAllUpgrades();
        foreach (string id in ids)
        {
            var u = all.Find(candidate => candidate != null && candidate.name == id);
            if (u == null) { Debug.LogWarning("Saved upgrade not found: " + id); continue; }
            purchasedUpgrades.Add(u);
            // These effects have their own saved state or were already recorded as copies.
            if (u.upgradeType != ShopUpgradeType.Joker && u.upgradeType != ShopUpgradeType.RemoveMinigame &&
                u.upgradeType != ShopUpgradeType.SurgePricing) ApplyUpgradeEffect(u);
        }
    }

    bool RestoreShop()
    {
        var data = SaveJSONData.Progress;
        if (!data.hasRun || slots == null || data.shopItems == null || data.shopSold == null ||
            data.shopItems.Count != slots.Length || data.shopSold.Count != slots.Length) return false;
        var all = GetAllUpgrades();
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            var upgrade = all.Find(u => u != null && u.name == data.shopItems[i]);
            slots[i].SetUpgrade(upgrade);
            slots[i].RestorePurchased(data.shopSold[i]);
        }
        return true;
    }

    ShopUpgradeData RollUniqueUpgrade(List<ShopUpgradeData> usedUpgrades)
    {
        for (int attempt = 0; attempt < maxAttemptsPerSlot; attempt++)
        {
            UpgradeRarity rarity = RollRarity();
            List<ShopUpgradeData> pool = GetPoolForRarity(rarity);

            if (pool == null || pool.Count == 0)
                continue;

            ShopUpgradeData candidate = pool[Random.Range(0, pool.Count)];

            if (candidate == null)
                continue;

            if (!candidate.allowDuplicateInShop && usedUpgrades.Contains(candidate))
                continue;

            if (!candidate.canBuyMultiple && purchasedUpgrades.Contains(candidate))
                continue;

            return candidate;
        }

        List<ShopUpgradeData> allUpgrades = GetAllUpgrades();

        foreach (ShopUpgradeData candidate in allUpgrades)
        {
            if (candidate == null) continue;
            if (!candidate.allowDuplicateInShop && usedUpgrades.Contains(candidate)) continue;
            if (!candidate.canBuyMultiple && purchasedUpgrades.Contains(candidate)) continue;

            return candidate;
        }

        return null;
    }

    void ApplyUpgradeEffect(ShopUpgradeData upgrade)
    {
        if (upgrade == null) return;

        switch (upgrade.upgradeType)
        {
            case ShopUpgradeType.None:
                break;

            case ShopUpgradeType.CarSpeed:
                if (CarController.Instance != null)
                    CarController.Instance.AddSpeedUpgrade(upgrade.value);
                break;

            case ShopUpgradeType.DopamineMax:
                if (DopamineManager.Instance != null)
                    DopamineManager.Instance.AddMaxDopamineUpgrade(upgrade.value);
                break;

            case ShopUpgradeType.RideFareMultiplier:
                if (TaxiRideManager.Instance != null)
                    TaxiRideManager.Instance.AddFareMultiplier(upgrade.value);
                break;

            case ShopUpgradeType.SurgePricing:
                if (TaxiRideManager.Instance != null)
                    TaxiRideManager.Instance.AddTemporaryFareMultiplier(upgrade.value, upgrade.durationDays);
                break;

            case ShopUpgradeType.TimeStopUnlock:
                if (PlayerUpgradeState.Instance != null)
                    PlayerUpgradeState.Instance.hasTimeStop = true;
                break;

            case ShopUpgradeType.RemoveMinigame:
                if (PhoneAppManager.Instance != null && PhoneAppManager.Instance.minigameSelector != null)
                    PhoneAppManager.Instance.minigameSelector.RemoveRandomPlayableGame();
                break;

            case ShopUpgradeType.Joker:
                if (PlayerUpgradeState.Instance != null)
                    Debug.Log("Joker copies are applied by the purchase transaction.");
                break;
        }
    }

    UpgradeRarity RollRarity()
    {
        float total = commonChance + uncommonChance + rareChance + epicChance + legendaryChance;
        if (total <= 0f)
            return UpgradeRarity.Common;

        float roll = Random.Range(0f, total);

        if (roll < legendaryChance)
            return UpgradeRarity.Legendary;

        roll -= legendaryChance;
        if (roll < epicChance)
            return UpgradeRarity.Epic;

        roll -= epicChance;
        if (roll < rareChance)
            return UpgradeRarity.Rare;

        roll -= rareChance;
        if (roll < uncommonChance)
            return UpgradeRarity.Uncommon;

        return UpgradeRarity.Common;
    }

    List<ShopUpgradeData> GetPoolForRarity(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Common: return commonUpgrades;
            case UpgradeRarity.Uncommon: return uncommonUpgrades;
            case UpgradeRarity.Rare: return rareUpgrades;
            case UpgradeRarity.Epic: return epicUpgrades;
            case UpgradeRarity.Legendary: return legendaryUpgrades;
        }

        return commonUpgrades;
    }

    public List<ShopUpgradeData> GetAllUpgrades()
    {
        List<ShopUpgradeData> all = new List<ShopUpgradeData>();

        all.AddRange(commonUpgrades);
        all.AddRange(uncommonUpgrades);
        all.AddRange(rareUpgrades);
        all.AddRange(epicUpgrades);
        all.AddRange(legendaryUpgrades);

        return all;
    }
}
