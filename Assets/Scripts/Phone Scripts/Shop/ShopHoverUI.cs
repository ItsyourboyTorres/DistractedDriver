using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopHoverUI : MonoBehaviour
{
    public static ShopHoverUI Instance { get; private set; }
    public GameObject rootPanel;
    public TMP_Text descriptionText, costText, warningText;
    [TextArea] public string defaultWarning = "";
    private Object hoverOwner;
    private string hoverDescription, hoverCost, hoverWarning;
    private string tutorialDescription, tutorialStatus, tutorialFooter;
    private bool hasHover;

    void Awake()
    {
        Instance = this;
        // Keep the actual scene panel, font, size, colors and effective text scale.
        ConfigureText(descriptionText, 0.06f, 0.60f);
        ConfigureText(costText, 0.60f, 0.77f);
        ConfigureText(warningText, 0.80f, 0.97f);
        if (rootPanel != null)
            foreach (Graphic graphic in rootPanel.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
    }

    void ConfigureText(TMP_Text text, float top, float bottom)
    {
        if (text == null || rootPanel == null) return;
        RectTransform panel = rootPanel.GetComponent<RectTransform>();
        RectTransform rect = text.rectTransform;
        float sx = Mathf.Abs(rect.localScale.x), sy = Mathf.Abs(rect.localScale.y);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -panel.rect.height * top);
        rect.sizeDelta = new Vector2(panel.rect.width * 0.90f / Mathf.Max(sx, 0.001f),
            panel.rect.height * (bottom - top) / Mathf.Max(sy, 0.001f));
        text.margin = Vector4.zero;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
    }

    void LateUpdate() { Render(); }
    public void Show(string description, string costLine) { ShowFor(this, description, costLine, ""); }
    public void ShowFor(Object owner, string description, string costLine, string warning)
    {
        hoverOwner = owner; hasHover = true;
        hoverDescription = description; hoverCost = costLine; hoverWarning = warning;
        Render();
    }
    public void HideFor(Object owner) { if (hoverOwner == owner) Hide(); }
    public void Hide() { hoverOwner = null; hasHover = false; Render(); }
    public void SetTutorial(string description, string status, string footer)
    {
        tutorialDescription = description; tutorialStatus = status; tutorialFooter = footer;
        Render();
    }
    void Render()
    {
        bool paused = Time.timeScale == 0f || (PauseMenuUI.Instance != null &&
            PauseMenuUI.Instance.pauseMenuRoot != null && PauseMenuUI.Instance.pauseMenuRoot.activeInHierarchy);
        bool shopOpen = PhoneAppManager.Instance != null && PhoneAppManager.Instance.CurrentApp == PhoneAppManager.App.Shop;
        if (!shopOpen) { hasHover = false; hoverOwner = null; }
        bool hover = hasHover && hoverOwner != null && shopOpen;
        bool show = !paused && (hover || !string.IsNullOrEmpty(tutorialDescription));
        if (rootPanel != null && rootPanel.activeSelf != show) rootPanel.SetActive(show);
        if (!show) return;
        if (descriptionText != null) descriptionText.text = hover ? hoverDescription : tutorialDescription;
        if (costText != null) costText.text = hover ? hoverCost : tutorialStatus;
        if (warningText != null) warningText.text = hover ? hoverWarning : tutorialFooter;
    }
}
