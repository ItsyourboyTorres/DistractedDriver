using UnityEngine;

public class PlayerUpgradeState : MonoBehaviour
{
    public static PlayerUpgradeState Instance { get; private set; }

    [Header("Unlocks / Special Effects")]
    public bool hasTimeStop = false;
    public int removeMinigameCharges = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void TriggerJokerCopy()
    {
        Debug.LogWarning("Use ShopManager.PurchaseUpgrade for Joker so its effect is applied exactly once.");
    }
}
