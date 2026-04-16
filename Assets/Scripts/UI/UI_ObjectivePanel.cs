using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_ObjectivePanel : MonoBehaviour
{
    [Header("Loot List")]
    [SerializeField] private Transform lootContainer;
    [SerializeField] private GameObject lootRowTemplate;

    [Header("Other Text")]
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI bossText;

    [Header("Panel chrome (optional — resolved by name if unset)")]
    [Tooltip("Gray panel behind the list (ShoppingListArt). Height is adjusted to fit all rows + footer.")]
    [SerializeField] private RectTransform shoppingListBackdrop;

    [Header("Dynamic List Text")]
    [SerializeField] private float listFontMax = 26f;
    [SerializeField] private float listFontMin = 11f;
    [SerializeField] private float rowLineHeightMultiplier = 1.2f;
    [SerializeField] private float footerTopGap = 14f;
    [SerializeField] private float bossBelowValueGap = 8f;
    [SerializeField] private float footerVerticalNudge = 30f;

    [Header("Backdrop sizing")]
    [SerializeField] private float titleAreaReserve = 78f;
    [SerializeField] private float backdropVerticalPadding = 32f;
    [SerializeField] private float backdropMinHeight = 260f;
    [SerializeField] private float backdropMaxHeight = 520f;

    private Vector2 valueInitialAnchoredPos;
    private Vector2 bossInitialAnchoredPos;
    private bool footerAnchorsInitialized;

    private void Awake()
    {
        if (shoppingListBackdrop == null)
        {
            Transform t = transform.Find("ShoppingListArt");
            if (t != null)
                shoppingListBackdrop = t as RectTransform;
        }
    }

    private void Start()
    {
        CacheFooterAnchors();
        RefreshUI();
        StartCoroutine(CoRefreshAfterObjectiveInit());
    }

    private IEnumerator CoRefreshAfterObjectiveInit()
    {
        yield return null;
        RefreshUI();
    }

    private void OnEnable()
    {
        if (RunObjectiveManager.Instance != null)
        {
            RunObjectiveManager.Instance.OnObjectiveProgressChanged += RefreshUI;
        }

        RefreshUI();
    }

    private void OnDisable()
    {
        if (RunObjectiveManager.Instance != null)
        {
            RunObjectiveManager.Instance.OnObjectiveProgressChanged -= RefreshUI;
        }
    }

    public void RefreshUI()
    {
        if (RunObjectiveManager.Instance == null)
        {
            ClearLootRows();

            if (valueText != null) valueText.text = "";
            if (bossText != null) bossText.text = "";

            return;
        }

        RefreshLootRows();
        RefreshValueText();
        RefreshBossText();
        ResizeShoppingListBackdrop();
        RepositionFooterBelowList();
    }

    private void RefreshLootRows()
    {
        if (lootContainer == null || lootRowTemplate == null) return;

        ClearLootRows();

        foreach (ShoppingListEntry entry in RunObjectiveManager.Instance.CurrentShoppingList)
        {
            if (entry == null) continue;

            GameObject row = Instantiate(lootRowTemplate, lootContainer);
            row.SetActive(true);

            Transform lootTextTransform = row.transform.Find("LootText");
            if (lootTextTransform != null)
            {
                TextMeshProUGUI text = lootTextTransform.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = $"{entry.GetDisplayName()} {entry.collectedAmount}/{entry.requiredAmount}";
                    ApplyDynamicListTextStyle(text, RunObjectiveManager.Instance.CurrentShoppingList.Count, GetFooterFontSize());
                }
            }

            Transform checkTransform = row.transform.Find("Checkmark");
            if (checkTransform != null)
            {
                checkTransform.gameObject.SetActive(entry.IsComplete());
            }
        }
    }

    private void ApplyDynamicListTextStyle(TextMeshProUGUI text, int rowCount, float footerFontSize)
    {
        if (text == null)
            return;

        float t = Mathf.InverseLerp(8f, 20f, rowCount);
        float dynamicSize = Mathf.Lerp(listFontMax, listFontMin, t);
        float targetSize = Mathf.Min(dynamicSize, Mathf.Max(10f, footerFontSize));

        text.enableAutoSizing = false;
        text.fontSize = targetSize;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private void CacheFooterAnchors()
    {
        if (footerAnchorsInitialized)
            return;

        if (valueText != null)
            valueInitialAnchoredPos = valueText.rectTransform.anchoredPosition;
        if (bossText != null)
            bossInitialAnchoredPos = bossText.rectTransform.anchoredPosition;
        footerAnchorsInitialized = true;
    }

    private float MeasureLootListHeight()
    {
        if (lootContainer == null)
            return 0f;

        RectTransform containerRect = lootContainer as RectTransform;
        if (containerRect == null)
            return 0f;

        VerticalLayoutGroup vlg = lootContainer.GetComponent<VerticalLayoutGroup>();
        float h = vlg != null ? vlg.padding.top + vlg.padding.bottom : 0f;
        int activeRows = 0;
        float rowFont = GetFooterFontSize();
        float rowHeight = Mathf.Max(20f, rowFont * rowLineHeightMultiplier);

        foreach (Transform child in lootContainer)
        {
            if (lootRowTemplate != null && child.gameObject == lootRowTemplate)
                continue;
            if (!child.gameObject.activeSelf)
                continue;

            h += rowHeight;
            activeRows++;
        }

        if (vlg != null && activeRows > 1)
            h += vlg.spacing * (activeRows - 1);

        return h;
    }

    private float GetFooterFontSize()
    {
        float valueSize = valueText != null ? valueText.fontSize : listFontMin;
        float bossSize = bossText != null ? bossText.fontSize : valueSize;
        return Mathf.Max(10f, Mathf.Min(valueSize, bossSize));
    }

    private void RepositionFooterBelowList()
    {
        if (lootContainer == null)
            return;

        CacheFooterAnchors();

        if (valueText != null)
        {
            valueText.gameObject.SetActive(true);
            RectTransform vr = valueText.rectTransform;
            float valueY = valueInitialAnchoredPos.y - Mathf.Max(0f, footerVerticalNudge);
            vr.anchoredPosition = new Vector2(
                valueInitialAnchoredPos.x,
                valueY);
        }

        if (bossText != null && valueText != null)
        {
            bossText.gameObject.SetActive(true);
            RectTransform br = bossText.rectTransform;
            float valueH = Mathf.Max(20f, valueText.preferredHeight);
            float bossY = valueText.rectTransform.anchoredPosition.y - valueH - Mathf.Max(0f, bossBelowValueGap);
            br.anchoredPosition = new Vector2(
                bossInitialAnchoredPos.x,
                bossY);
        }
    }

    private void ResizeShoppingListBackdrop()
    {
        if (shoppingListBackdrop == null)
            return;

        float listHeight = MeasureLootListHeight();

        float valueH = valueText != null ? Mathf.Max(24f, valueText.preferredHeight) : 0f;
        float bossH = bossText != null ? Mathf.Max(24f, bossText.preferredHeight) : 0f;

        float footerBlock = footerTopGap;
        if (valueText != null)
            footerBlock += valueH + bossBelowValueGap;
        if (bossText != null)
            footerBlock += bossH;

        float needed =
            titleAreaReserve
            + listHeight
            + footerBlock
            + backdropVerticalPadding;

        float newH = Mathf.Clamp(needed, backdropMinHeight, Mathf.Max(backdropMinHeight, backdropMaxHeight));

        Vector2 sd = shoppingListBackdrop.sizeDelta;
        float oldH = sd.y;
        if (Mathf.Abs(newH - oldH) < 1f)
            return;

        shoppingListBackdrop.sizeDelta = new Vector2(sd.x, newH);

        float dh = newH - oldH;
        // Pivot is centered: extend downward by shifting the rect center down half the delta.
        shoppingListBackdrop.anchoredPosition += new Vector2(0f, -dh * 0.5f);
    }

    private void ClearLootRows()
    {
        if (lootContainer == null || lootRowTemplate == null) return;

        foreach (Transform child in lootContainer)
        {
            if (child.gameObject == lootRowTemplate) continue;
            Destroy(child.gameObject);
        }
    }

    private void RefreshValueText()
    {
        if (valueText == null) return;
        valueText.gameObject.SetActive(true);

        RunObjectiveManager rom = RunObjectiveManager.Instance;
        if (rom == null)
            return;

        int cur = rom.CurrentCollectedValue;
        int req = rom.RequiredGoalValue;
        valueText.text = $"Value: {cur} / {req}";
    }

    private void RefreshBossText()
    {
        if (bossText == null) return;
        bossText.gameObject.SetActive(true);

        if (RunObjectiveManager.Instance.IsObjectiveComplete())
        {
            bossText.text = "Boss: <color=#4CAF50><b>Unlocked</b></color>";
        }
        else
        {
            bossText.text = "Boss: <color=#F44336><b>Locked</b></color>";
        }
    }
}
