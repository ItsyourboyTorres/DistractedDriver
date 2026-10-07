using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ShopUpgradeSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public RawImage backgroundImage;
    public Image iconImage;
    public TMP_Text nameText;
    public Button upgradeButton;
    public GameObject boughtOverlay;
    public TMP_Text overlayText;
    public Texture commonBackground, uncommonBackground, rareBackground, epicBackground, legendaryBackground;
    public ShopUpgradeData currentUpgrade;
    private bool purchased, hovering;
    public bool IsPurchased => purchased;

    void Awake()
    {
        if (nameText != null)
        {
            nameText.enableAutoSizing = true;
            nameText.fontSizeMax = nameText.fontSize;
            nameText.fontSizeMin = nameText.fontSize * 0.65f;
        }
        if (upgradeButton != null)
        {
            // Some scenes already wire this method in the Inspector.
            bool wired = false;
            for (int i = 0; i < upgradeButton.onClick.GetPersistentEventCount(); i++)
                if (upgradeButton.onClick.GetPersistentTarget(i) == this &&
                    upgradeButton.onClick.GetPersistentMethodName(i) == nameof(BuyUpgrade)) wired = true;
            if (!wired) upgradeButton.onClick.AddListener(BuyUpgrade);
        }
    }
    void Update()
    {
        RefreshSlotState();
        if (hovering) ShowHover();
    }
    void OnDisable()
    {
        hovering = false;
        ShopHoverUI.Instance?.HideFor(this);
    }
    public void SetUpgrade(ShopUpgradeData upgrade)
    {
        currentUpgrade = upgrade; purchased = false;
        if (upgrade == null) { ClearSlot(); return; }
        if (iconImage != null) { iconImage.sprite = upgrade.icon; iconImage.enabled = upgrade.icon != null; }
        if (nameText != null) nameText.text = upgrade.upgradeName;
        if (backgroundImage != null) backgroundImage.texture = GetBackgroundForRarity(upgrade.rarity);
        RefreshSlotState();
    }
    public void RestorePurchased(bool value) { purchased = value; RefreshSlotState(); }
    public void ClearSlot()
    {
        currentUpgrade = null; purchased = false;
        if (iconImage != null) { iconImage.sprite = null; iconImage.enabled = false; }
        if (nameText != null) nameText.text = "SOLD OUT";
        if (backgroundImage != null) backgroundImage.texture = null;
        if (upgradeButton != null) upgradeButton.interactable = false;
        if (boughtOverlay != null) boughtOverlay.SetActive(false);
        if (overlayText != null) overlayText.text = "";
    }
    Texture GetBackgroundForRarity(UpgradeRarity rarity)
    {
        switch (rarity)
        {
            case UpgradeRarity.Uncommon: return uncommonBackground;
            case UpgradeRarity.Rare: return rareBackground;
            case UpgradeRarity.Epic: return epicBackground;
            case UpgradeRarity.Legendary: return legendaryBackground;
            default: return commonBackground;
        }
    }
    bool Owned => currentUpgrade != null && !currentUpgrade.canBuyMultiple && ShopManager.Instance != null &&
        ShopManager.Instance.purchasedUpgrades.Contains(currentUpgrade);
    void RefreshSlotState()
    {
        if (currentUpgrade == null) return;
        bool sold = purchased || Owned;
        bool available = ShopManager.Instance != null && ShopManager.Instance.CanPurchase(currentUpgrade, out _);
        if (upgradeButton != null) upgradeButton.interactable = !sold && available && SaveJSONData.Ready;
        if (boughtOverlay != null) boughtOverlay.SetActive(sold);
        if (overlayText != null) overlayText.text = sold ? "OWNED" : "";
    }
    void ShowHover()
    {
        if (currentUpgrade == null || ShopHoverUI.Instance == null) return;
        int price = MoneyManager.Instance != null ? MoneyManager.Instance.GetScaledUpgradePrice(currentUpgrade.basePrice) : currentUpgrade.basePrice;
        string reason = "UNAVAILABLE";
        bool allowed = ShopManager.Instance != null && ShopManager.Instance.CanPurchase(currentUpgrade, out reason);
        string status = purchased || Owned ? "OWNED" : "$" + price + "  /  " + currentUpgrade.rarity.ToString().ToUpperInvariant();
        string warning = purchased || Owned ? "RESTOCKS NEXT DAY" :
            !allowed ? reason : currentUpgrade.canBuyMultiple ? "STACKABLE - RESTOCKS DAILY" : "ONE-TIME UNLOCK";
        if ((purchased || Owned) && !currentUpgrade.canBuyMultiple) warning = "ONE-TIME UNLOCK";
        ShopHoverUI.Instance.ShowFor(this, currentUpgrade.upgradeName + "\n" + (currentUpgrade.description ?? "").Trim(), status, warning);
    }
    public void OnPointerEnter(PointerEventData eventData) { HoverEnter(); }
    public void OnPointerExit(PointerEventData eventData) { HoverExit(); }
    public void HoverEnter() { hovering = true; ShowHover(); }
    public void HoverExit() { hovering = false; ShopHoverUI.Instance?.HideFor(this); }
    public void BuyUpgrade()
    {
        if (currentUpgrade == null || purchased || Owned || !SaveJSONData.Ready || ShopManager.Instance == null) return;
        if (!ShopManager.Instance.PurchaseUpgrade(currentUpgrade)) { ShowHover(); return; }
        purchased = true;
        RefreshSlotState(); ShowHover(); SaveJSONData.SaveProgress();
    }
}
