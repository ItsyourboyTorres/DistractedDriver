using UnityEngine;
using UnityEngine.UI;

public class FirstDriveTutorial : MonoBehaviour
{
    public static FirstDriveTutorial Instance { get; private set; }
    public bool IsRunning => !SaveJSONData.Progress.tutorialCompleted;
    private float nextOfferRetry;
    private ShopUpgradeSlot highlightedSlot;
    private Outline highlight;
    void Awake() { Instance = this; }

    void Update()
    {
        if (ShopHoverUI.Instance == null) return;
        if (!IsRunning)
        {
            ShopHoverUI.Instance.SetTutorial(null, null, null); ClearHighlight(); return;
        }
        if (Time.timeScale == 0f) return;
        if (Input.GetKeyDown(KeyCode.F8)) { SkipTutorial(); return; }
        var progress = SaveJSONData.Progress;
        if (progress.tutorialPurchased && progress.tutorialDelivered) { Complete(); return; }
        string message, status;
        if (!progress.tutorialPurchased)
        {
            bool shopOpen = PhoneAppManager.Instance != null && PhoneAppManager.Instance.CurrentApp == PhoneAppManager.App.Shop;
            message = shopOpen
                ? "Hover an upgrade to read its effect and price. Click the highlighted upgrade to buy it."
                : "Welcome! Open the SHOP app on your phone. Upgrades help your taxi and your earnings.";
            status = "1 / 4  -  YOUR FIRST UPGRADE";
            HighlightStarter(shopOpen);
        }
        else
        {
            ClearHighlight();
            var rides = TaxiRideManager.Instance;
            if (rides != null && rides.IsGoingToDropoff)
            {
                message = "Passenger aboard! Follow the GPS arrow to the drop-off zone and drive into it to finish the ride.";
                status = "4 / 4  -  COMPLETE THE RIDE";
            }
            else if (rides != null && rides.HasActiveRide)
            {
                message = "Follow the GPS arrow to your passenger. Drive into the pickup square to collect them.";
                status = "3 / 4  -  FOLLOW THE ARROW";
            }
            else
            {
                bool offers = rides != null && rides.HasValidOffers;
                message = offers
                    ? "Open the TAXI app. Choose a passenger and click ACCEPT to start your first ride."
                    : "Waiting for a ride request. You can explore the phone or press F8 to skip the tutorial.";
                status = "2 / 4  -  CHOOSE A PASSENGER";
                if (!offers && rides != null && Time.unscaledTime >= nextOfferRetry)
                {
                    nextOfferRetry = Time.unscaledTime + 5f; rides.RefreshAvailableOffers();
                }
            }
        }
        ShopHoverUI.Instance.SetTutorial(message, status, "F8 - SKIP TUTORIAL");
    }

    public void PrepareStarterPurchase()
    {
        var progress = SaveJSONData.Progress;
        if (!IsRunning || progress.tutorialPurchased || ShopManager.Instance == null || MoneyManager.Instance == null) return;
        var shop = ShopManager.Instance;
        if (shop.slots == null || shop.slots.Length == 0 || shop.slots[0] == null) return;
        var starter = shop.GetAllUpgrades().Find(u => u != null && u.upgradeType == ShopUpgradeType.CarSpeed && shop.EffectAvailable(u));
        if (starter == null) starter = shop.GetAllUpgrades().Find(u => u != null && shop.EffectAvailable(u));
        if (starter == null) return;
        // Existing upgrade at its normal price; one-time persisted cash assistance.
        if (!progress.tutorialGrantGiven)
        {
            progress.tutorialGrantGiven = true;
            int shortfall = Mathf.Max(0, MoneyManager.Instance.GetScaledUpgradePrice(starter.basePrice) - MoneyManager.Instance.currentCash);
            if (shortfall > 0) MoneyManager.Instance.AddCash(shortfall);
        }
        shop.slots[0].SetUpgrade(starter);
        SaveJSONData.SaveProgress();
    }

    void HighlightStarter(bool visible)
    {
        if (!visible || ShopManager.Instance == null) { ClearHighlight(); return; }
        var slots = ShopManager.Instance.slots;
        if (slots == null) return;
        ShopUpgradeSlot target = System.Array.Find(slots, s => s != null && s.currentUpgrade != null &&
            ShopManager.Instance.CanPurchase(s.currentUpgrade, out _));
        if (target == highlightedSlot) return;
        ClearHighlight();
        if (target == null || target.upgradeButton == null || target.upgradeButton.targetGraphic == null) return;
        highlightedSlot = target;
        highlight = target.upgradeButton.targetGraphic.gameObject.AddComponent<Outline>();
        highlight.effectColor = new Color(1f, 0.84f, 0.29f);
        highlight.effectDistance = new Vector2(0.05f, -0.05f);
    }
    void ClearHighlight()
    {
        if (highlight != null) Destroy(highlight);
        highlight = null; highlightedSlot = null;
    }
    [ContextMenu("Skip Tutorial")]
    public void SkipTutorial() { Complete(); }
    void Complete()
    {
        SaveJSONData.Progress.tutorialCompleted = true;
        SaveJSONData.SaveProgress(); ClearHighlight();
        ShopHoverUI.Instance?.SetTutorial(null, null, null);
    }
    [ContextMenu("Reset Tutorial For Testing")]
    public void ResetTutorial()
    {
        var progress = SaveJSONData.Progress;
        progress.tutorialCompleted = false; progress.tutorialPurchased = false; progress.tutorialDelivered = false;
        // Do not reset the cash grant when only resetting instructions.
        SaveJSONData.SaveProgress(); PrepareStarterPurchase();
    }
    void OnDisable() { ClearHighlight(); }
}
