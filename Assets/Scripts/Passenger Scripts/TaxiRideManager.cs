using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaxiRideManager : MonoBehaviour
{
    public static TaxiRideManager Instance { get; private set; }

    [Header("UI References")]
    public PhoneHomeUI phoneHomeUI;
    public PhoneTaxiUI phoneTaxiUI;

    [Header("Taxi App Offers UI")]
    public TaxiAppOffersUI offersUI;

    [Header("Taxi App Views (prevents GPS jumping)")]
    public GameObject offersView;
    public GameObject activeRideView;

    [Header("Dialogue Popup UI (sprite pops up)")]
    public PassengerDialogueUI dialogueUI;

    [Header("Dialogue Timing")]
    public float pickupDialogueDelay = 0.25f;
    public float dropoffDialogueDelay = 0.5f;
    public float dialogueVisibleAfterComplete = 5f;

    [Header("World References")]
    public PickupZone[] pickupZones;
    public DropoffZone[] dropoffs;
    public PassengerPickup[] passengers;

    [Header("Offers Settings")]
    public int offersCount = 3;

    [Header("Flow Tuning")]
    public float nextRideDelay = 0.5f;

    [Header("Fare Upgrades")]
    public float permanentFareBonus = 0f;
    public float temporaryFareBonus = 0f;
    public int surgeDaysRemaining = 0;

    private enum RideState { Idle, ChoosingOffer, GoingToPickup, GoingToDropoff }
    private RideState state = RideState.Idle;

    private int currentPassengerIndex = -1;
    private int currentDropoffIndex = -1;

    private Coroutine nextRideRoutine;

    [System.Serializable]
    public struct RideOffer
    {
        public bool IsValid;
        public int passengerIndex;
        public int dropoffIndex;
        public float stars;
        public string passengerName;
        public string dropoffName;
        public Sprite portrait;
    }

    private RideOffer[] offers;

    private string currentRideMsg = "";
    private Transform currentGpsTarget = null;

    public bool CanAcceptOffers => state == RideState.ChoosingOffer;
    public bool IsGoingToDropoff => state == RideState.GoingToDropoff;
    public bool HasValidOffers => offers != null && System.Array.Exists(offers, offer => offer.IsValid);
    private List<FareBoostSave> fareBoosts = new List<FareBoostSave>();
    public void RefreshAvailableOffers() { if (!HasActiveRide) BuildNewOffers(); }

    public bool HasActiveRide
    {
        get
        {
            return state == RideState.GoingToPickup || state == RideState.GoingToDropoff;
        }
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        offers = new RideOffer[Mathf.Max(1, offersCount)];
        BuildNewOffers();
    }

    private void SetTaxiViews(bool showOffers)
    {
        if (offersView != null)
            offersView.SetActive(showOffers);

        if (activeRideView != null)
            activeRideView.SetActive(!showOffers);
    }

    private void BuildNewOffers()
    {
        if (nextRideRoutine != null)
        {
            StopCoroutine(nextRideRoutine);
            nextRideRoutine = null;
        }

        if (passengers == null || passengers.Length == 0 ||
            dropoffs == null || dropoffs.Length == 0 ||
            pickupZones == null || pickupZones.Length == 0)
        {
            Debug.LogWarning("TaxiRideManager: Need passengers, pickupZones, and dropoffs assigned.");
            return;
        }

        var validDropoffs = new List<int>();
        for (int i = 0; i < dropoffs.Length; i++) if (dropoffs[i] != null) validDropoffs.Add(i);
        if (validDropoffs.Count == 0) return;
        if (offers == null) offers = new RideOffer[Mathf.Max(1, offersCount)];
        state = RideState.ChoosingOffer;

        SetTaxiViews(true);

        for (int i = 0; i < pickupZones.Length; i++)
        {
            if (pickupZones[i] != null)
                pickupZones[i].SetActive(false);
        }

        for (int i = 0; i < dropoffs.Length; i++)
        {
            if (dropoffs[i] != null)
                dropoffs[i].SetActive(false);
        }

        List<int> availablePassengerIndexes = new List<int>();

        for (int i = 0; i < passengers.Length; i++)
        {
            if (passengers[i] != null && i < pickupZones.Length && pickupZones[i] != null)
                availablePassengerIndexes.Add(i);
        }

        for (int i = 0; i < availablePassengerIndexes.Count; i++)
        {
            int randomIndex = Random.Range(i, availablePassengerIndexes.Count);

            int temp = availablePassengerIndexes[i];
            availablePassengerIndexes[i] = availablePassengerIndexes[randomIndex];
            availablePassengerIndexes[randomIndex] = temp;
        }

        for (int i = 0; i < offers.Length; i++)
        {
            if (i >= availablePassengerIndexes.Count)
            {
                offers[i] = new RideOffer { IsValid = false };
                continue;
            }

            int pIndex = availablePassengerIndexes[i];
            int dIndex = validDropoffs[Random.Range(0, validDropoffs.Count)];

            PassengerPickup p = passengers[pIndex];
            DropoffZone d = dropoffs[dIndex];

            offers[i] = new RideOffer
            {
                IsValid = (p != null && d != null),
                passengerIndex = pIndex,
                dropoffIndex = dIndex,
                stars = p != null ? p.difficultyStars : 1f,
                passengerName = p != null ? p.passengerName : "Passenger",
                dropoffName = d != null ? d.dropoffName : "Dropoff",
                portrait = p != null ? p.portraitSprite : null
            };
        }

        if (phoneHomeUI != null)
            phoneHomeUI.ShowTaxiNotification($"{offers.Length} ride requests waiting. Open Taxi app to choose.");

        currentRideMsg = "Choose a ride:";
        currentGpsTarget = null;
        PushTaxiUI();

        if (offersUI != null)
            offersUI.Refresh();

        Debug.Log("[TaxiRideManager] Built new offers.");
    }

    public RideOffer GetOffer(int index)
    {
        if (offers == null || index < 0 || index >= offers.Length)
            return new RideOffer { IsValid = false };

        return offers[index];
    }

    public void AcceptOffer(int offerIndex)
    {
        if (state != RideState.ChoosingOffer) return;
        if (offers == null || offerIndex < 0 || offerIndex >= offers.Length) return;
        if (!offers[offerIndex].IsValid) return;

        var chosen = offers[offerIndex];
        currentPassengerIndex = chosen.passengerIndex;
        currentDropoffIndex = chosen.dropoffIndex;

        if (currentPassengerIndex < 0 || currentPassengerIndex >= passengers.Length) return;
        if (currentDropoffIndex < 0 || currentDropoffIndex >= dropoffs.Length) return;
        if (currentPassengerIndex < 0 || currentPassengerIndex >= pickupZones.Length) return;

        PassengerPickup currentPassenger = passengers[currentPassengerIndex];
        DropoffZone currentDropoff = dropoffs[currentDropoffIndex];
        PickupZone currentPickupZone = pickupZones[currentPassengerIndex];

        if (currentPassenger == null || currentDropoff == null || currentPickupZone == null) return;

        state = RideState.GoingToPickup;

        SetTaxiViews(false);

        for (int i = 0; i < pickupZones.Length; i++)
        {
            if (pickupZones[i] != null)
                pickupZones[i].SetActive(false);
        }

        for (int i = 0; i < dropoffs.Length; i++)
        {
            if (dropoffs[i] != null)
                dropoffs[i].SetActive(false);
        }

        currentPickupZone.rideManager = this;
        currentPickupZone.SetActive(true);

        string msg = $"{currentPassenger.passengerName} accepted. Go to the pickup square.";
        currentRideMsg = msg;

        currentGpsTarget = currentPickupZone.transform;
        PushTaxiUI();

        if (offersUI != null)
            offersUI.Refresh();

        if (PhoneAppManager.Instance != null)
        {
            PhoneAppManager.Instance.OpenHomeWithRideAccepted();
        }
        else if (phoneHomeUI != null)
        {
            phoneHomeUI.ShowRideAccepted();
        }

        SaveJSONData.SaveProgress();
        Debug.Log($"[TaxiRideManager] Accepted offer {offerIndex}: {currentPassenger.passengerName} -> {currentDropoff.dropoffName}");
    }

    public void OnPickupZoneEntered(PickupZone zone)
    {
        if (state != RideState.GoingToPickup)
            return;

        if (currentPassengerIndex < 0 || currentPassengerIndex >= pickupZones.Length)
            return;

        if (zone != pickupZones[currentPassengerIndex])
            return;

        state = RideState.GoingToDropoff;

        zone.SetActive(false);

        PassengerPickup currentPassenger = passengers[currentPassengerIndex];

        if (dialogueUI != null && currentPassenger != null)
        {
            dialogueUI.ShowLine(
                currentPassenger.passengerName,
                currentPassenger.portraitSprite,
                currentPassenger.GetRandomPickupDialogue(),
                pickupDialogueDelay,
                dialogueVisibleAfterComplete
            );
        }

        if (currentDropoffIndex < 0 || currentDropoffIndex >= dropoffs.Length)
            return;

        DropoffZone currentDropoff = dropoffs[currentDropoffIndex];

        if (currentDropoff == null)
            return;

        currentDropoff.rideManager = this;
        currentDropoff.SetActive(true);

        string msg = $"Taking {currentPassenger.passengerName} to {currentDropoff.dropoffName}";
        currentRideMsg = msg;
        currentGpsTarget = currentDropoff.transform;
        PushTaxiUI();

        SaveJSONData.SaveProgress();
        Debug.Log($"Picked up {currentPassenger.passengerName}, heading to {currentDropoff.dropoffName}");
    }

    public void OnDropoffTrigger(DropoffZone zone)
    {
        if (state != RideState.GoingToDropoff)
            return;

        if (currentDropoffIndex < 0 || currentDropoffIndex >= dropoffs.Length)
            return;

        DropoffZone currentDropoff = dropoffs[currentDropoffIndex];

        if (currentDropoff == null)
            return;

        if (zone != currentDropoff)
            return;

        zone.SetActive(false);
        state = RideState.Idle;

        PassengerPickup currentPassenger =
            (currentPassengerIndex >= 0 && currentPassengerIndex < passengers.Length)
            ? passengers[currentPassengerIndex]
            : null;

        string passengerName = currentPassenger != null ? currentPassenger.passengerName : "Passenger";

        if (dialogueUI != null && currentPassenger != null)
        {
            dialogueUI.ShowLine(
                currentPassenger.passengerName,
                currentPassenger.portraitSprite,
                currentPassenger.GetRandomDropoffDialogue(),
                dropoffDialogueDelay,
                dialogueVisibleAfterComplete
            );
        }

        float earnedFare = 0f;

        if (MoneyManager.Instance != null)
        {
            float originalMultiplier = MoneyManager.Instance.fareMultiplier;
            MoneyManager.Instance.SetFareMultiplier(GetCurrentFareMultiplier());
            earnedFare = MoneyManager.Instance.AddRideFare();
            MoneyManager.Instance.SetFareMultiplier(originalMultiplier);
        }

        string msg = $"Ride complete! {passengerName} is at {currentDropoff.dropoffName}. +${earnedFare:0}";
        currentRideMsg = msg;
        currentGpsTarget = null;
        PushTaxiUI();

        if (GameManager.Instance != null)
            GameManager.Instance.AddRideScore();

        Debug.Log("[TaxiRideManager] Current fare multiplier = " + GetCurrentFareMultiplier());
        Debug.Log("[TaxiRideManager] Earned fare = $" + earnedFare.ToString("0"));

        if (nextRideRoutine != null)
            StopCoroutine(nextRideRoutine);

        SaveJSONData.Progress.tutorialDelivered = true;
        SaveJSONData.SaveProgress();
        nextRideRoutine = StartCoroutine(BuildOffersAfterDelay(nextRideDelay));
    }

    private IEnumerator BuildOffersAfterDelay(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        BuildNewOffers();
        nextRideRoutine = null;
    }

    private void PushTaxiUI()
    {
        if (phoneTaxiUI != null)
        {
            phoneTaxiUI.SetRideInfo(currentRideMsg);

            if (currentGpsTarget != null)
                phoneTaxiUI.SetGpsTarget(currentGpsTarget);
            else
                phoneTaxiUI.ClearGpsTarget();
        }

        if (CarGPSArrow3D.Instance != null)
        {
            if (currentGpsTarget != null)
                CarGPSArrow3D.Instance.SetTarget(currentGpsTarget);
            else
                CarGPSArrow3D.Instance.ClearTarget();
        }
    }

    public void AddFareMultiplier(float amount)
    {
        permanentFareBonus += amount;
        Debug.Log("[TaxiRideManager] Permanent fare bonus now = " + permanentFareBonus);
    }

    public void AddTemporaryFareMultiplier(float amount, int days)
    {
        if (amount <= 0 || days <= 0) return;
        fareBoosts.Add(new FareBoostSave { amount = amount, days = days });
        RefreshFareBoosts();
    }
    void RefreshFareBoosts()
    {
        temporaryFareBonus = 0; surgeDaysRemaining = 0;
        foreach (var boost in fareBoosts)
        {
            temporaryFareBonus += boost.amount;
            surgeDaysRemaining = Mathf.Max(surgeDaysRemaining, boost.days);
        }
    }
    public float GetCurrentFareMultiplier() { return 1f + permanentFareBonus + temporaryFareBonus; }
    public void AdvanceUpgradeDay()
    {
        foreach (var boost in fareBoosts) boost.days--;
        fareBoosts.RemoveAll(boost => boost.days <= 0);
        RefreshFareBoosts();
    }
    public List<FareBoostSave> CaptureFareBoosts()
    {
        return fareBoosts.ConvertAll(b => new FareBoostSave { amount = b.amount, days = b.days });
    }
    public void RestoreFareBoosts(List<FareBoostSave> values)
    {
        fareBoosts = values.FindAll(b => b != null && b.days > 0 && b.amount > 0)
            .ConvertAll(b => new FareBoostSave { amount = b.amount, days = b.days });
        RefreshFareBoosts();
    }
    public void CaptureRide(out int passenger, out int dropoff, out int phase)
    {
        passenger = currentPassengerIndex; dropoff = currentDropoffIndex;
        phase = IsGoingToDropoff ? 2 : HasActiveRide ? 1 : 0;
    }
    public void RestoreRide(int passenger, int dropoff, int phase)
    {
        if (phase == 0 || passengers == null || pickupZones == null || dropoffs == null ||
            passenger < 0 || passenger >= passengers.Length || passenger >= pickupZones.Length ||
            dropoff < 0 || dropoff >= dropoffs.Length || passengers[passenger] == null ||
            pickupZones[passenger] == null || dropoffs[dropoff] == null) return;
        offers = new RideOffer[Mathf.Max(1, offersCount)];
        offers[0] = new RideOffer { IsValid = true, passengerIndex = passenger, dropoffIndex = dropoff };
        state = RideState.ChoosingOffer;
        AcceptOffer(0);
        if (phase == 2) OnPickupZoneEntered(pickupZones[passenger]);
    }
}
