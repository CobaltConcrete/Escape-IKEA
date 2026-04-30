using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class PlayerHealthUI : MonoBehaviour
{
    [Header("Player References")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerInventoryInteraction playerInventoryInteraction;

    [Header("Layout Roots")]
    [SerializeField] private RectTransform barListRoot;

    [Header("Health")]
    [SerializeField] private RectTransform healthRowRoot;
    [SerializeField] private RectTransform healthBarFillRect;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Text healthText;
    [SerializeField] private float barWidthFallback = 200f;
    [SerializeField] private Color healthBarColor = new Color(0.2f, 0.9f, 0.3f, 1f);

    [Header("Armor")]
    [SerializeField] private RectTransform armorRowRoot;
    [SerializeField] private RectTransform armorBarFillRect;
    [SerializeField] private Image armorFillImage;
    [SerializeField] private Text armorText;
    [SerializeField] private Color armorBarColor = new Color(0.2f, 0.55f, 1f, 1f);

    [Header("Boss")]
    [SerializeField] private RectTransform bossRowRoot;
    [SerializeField] private RectTransform bossHealthBarFillRect;
    [SerializeField] private Image bossHealthFillImage;
    [SerializeField] private Text bossHealthText;
    [SerializeField] private Color bossBarColor = Color.red;

    [Header("Run Timer")]
    [SerializeField] private Text runTimerText;
    [SerializeField] private GameRunTimer runTimer;

    private EnemyCombat bossCombat;

    private void Awake()
    {
        ResolvePlayerHealth();
        ResolvePlayerInventoryInteraction();
        ResolveUiReferences();
    }

    private void OnEnable()
    {
        ResolvePlayerHealth();
        ResolvePlayerInventoryInteraction();
        ResolveUiReferences();

        if (playerHealth != null)
            playerHealth.OnHealthChanged += Refresh;

        EnsureRunTimerHud();

        if (!Application.isPlaying)
        {
            RefreshEditorPreview();
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= Refresh;
    }

    private void Start()
    {
        ResolvePlayerHealth();
        ResolvePlayerInventoryInteraction();
        ResolveUiReferences();
        EnsureRunTimerHud();
        BindGameRunTimer();
        Refresh();
    }

    private void Update()
    {
        if (!Application.isPlaying)
            return;

        if (playerHealth == null)
            ResolvePlayerHealth();

        if (playerInventoryInteraction == null)
            ResolvePlayerInventoryInteraction();

        ResolveUiReferences();
        ResolveBossCombat();
        Refresh();
    }

    private void ResolvePlayerHealth()
    {
        if (playerHealth == null)
            playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
    }

    private void ResolvePlayerInventoryInteraction()
    {
        if (playerInventoryInteraction == null)
            playerInventoryInteraction = Object.FindFirstObjectByType<PlayerInventoryInteraction>();
    }

    private void ResolveUiReferences()
    {
        if (barListRoot == null)
        {
            Transform t = transform.Find("BarList");
            if (t != null)
                barListRoot = t as RectTransform;
        }

        if (healthRowRoot == null)
        {
            Transform t = transform.Find("BarList/Healthbar");
            if (t != null)
                healthRowRoot = t as RectTransform;
        }

        if (healthBarFillRect == null)
        {
            Transform t = transform.Find("BarList/Healthbar/Fill");
            if (t != null)
                healthBarFillRect = t as RectTransform;
        }

        if (healthFillImage == null && healthBarFillRect != null)
            healthFillImage = healthBarFillRect.GetComponent<Image>();

        if (healthText == null)
        {
            Transform t = transform.Find("BarList/Healthbar/Image");
            if (t != null)
                healthText = t.GetComponent<Text>();
        }

        if (armorRowRoot == null)
        {
            Transform t = transform.Find("BarList/ArmorLine");
            if (t != null)
                armorRowRoot = t as RectTransform;
        }

        if (armorBarFillRect == null)
        {
            Transform t = transform.Find("BarList/ArmorLine/Fill");
            if (t != null)
                armorBarFillRect = t as RectTransform;
        }

        if (armorFillImage == null && armorBarFillRect != null)
            armorFillImage = armorBarFillRect.GetComponent<Image>();

        if (armorText == null)
        {
            Transform t = transform.Find("BarList/ArmorLine/ArmorLabel");
            if (t != null)
                armorText = t.GetComponent<Text>();
        }

        if (bossRowRoot == null)
        {
            Transform t = transform.Find("BarList/BossHealthLine");
            if (t != null)
                bossRowRoot = t as RectTransform;
        }

        if (bossHealthBarFillRect == null)
        {
            Transform t = transform.Find("BarList/BossHealthLine/Fill");
            if (t != null)
                bossHealthBarFillRect = t as RectTransform;
        }

        if (bossHealthFillImage == null && bossHealthBarFillRect != null)
            bossHealthFillImage = bossHealthBarFillRect.GetComponent<Image>();

        if (bossHealthText == null)
        {
            Transform t = transform.Find("BarList/BossHealthLine/BossHealthLabel");
            if (t != null)
                bossHealthText = t.GetComponent<Text>();
        }
    }

    private void Refresh()
    {
        RefreshPlayerHealth();
        RefreshArmor();
        RefreshBoss();
    }

    private void RefreshPlayerHealth()
    {
        if (!Application.isPlaying)
            return;

        if (playerHealth == null) return;

        SetRowVisible(healthRowRoot, true);

        float t = playerHealth.MaxHealth > 0f
            ? Mathf.Clamp01(playerHealth.CurrentHealth / playerHealth.MaxHealth)
            : 0f;

        SetBarFill(healthBarFillRect, healthFillImage, t);

        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(playerHealth.CurrentHealth):0} / {Mathf.Ceil(playerHealth.MaxHealth):0}";
        }

        if (healthFillImage != null)
        {
            healthFillImage.color = healthBarColor;
        }
    }

    private void RefreshArmor()
    {
        if (!Application.isPlaying)
        {
            if (armorFillImage != null)
                armorFillImage.color = armorBarColor;

            if (armorText != null)
                armorText.text = "Armor";

            SetRowVisible(armorRowRoot, true);
            SetBarFill(armorBarFillRect, armorFillImage, 1f);
            return;
        }

        if (playerInventoryInteraction == null)
        {
            SetRowVisible(armorRowRoot, false);
            return;
        }

        Item armorItem = playerInventoryInteraction.GetEquippedArmorItem();
        if (armorItem == null || !armorItem.IsArmor())
        {
            SetRowVisible(armorRowRoot, false);
            return;
        }

        armorItem.InitializeRuntimeDataIfNeeded();

        float max = armorItem.GetArmorMaxDurability();
        float current = Mathf.Max(0f, armorItem.GetArmorCurrentDurability());
        float t = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        SetRowVisible(armorRowRoot, true);
        SetBarFill(armorBarFillRect, armorFillImage, t);

        if (armorFillImage != null)
            armorFillImage.color = armorBarColor;

        if (armorText != null)
            armorText.text = $"{Mathf.Ceil(current):0} / {Mathf.Ceil(max):0}";
    }

    private void RefreshBoss()
    {
        if (!Application.isPlaying)
        {
            if (bossHealthFillImage != null)
                bossHealthFillImage.color = bossBarColor;

            if (bossHealthText != null)
                bossHealthText.text = "Boss HP";

            SetRowVisible(bossRowRoot, true);
            SetBarFill(bossHealthBarFillRect, bossHealthFillImage, 1f);
            return;
        }

        bool shouldShowBoss = false;

        if (BossRoomController.IsPlayerInsideBossRoom())
        {
            if (bossCombat != null && bossCombat.GetCurrentHealth() > 0f)
            {
                shouldShowBoss = true;
            }
        }

        if (!shouldShowBoss)
        {
            SetRowVisible(bossRowRoot, false);

            if (bossHealthText != null)
                bossHealthText.text = "";

            return;
        }

        float max = bossCombat.GetMaxHealth();
        float current = bossCombat.GetCurrentHealth();
        float t = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        SetRowVisible(bossRowRoot, true);
        SetBarFill(bossHealthBarFillRect, bossHealthFillImage, t);

        if (bossHealthFillImage != null)
            bossHealthFillImage.color = bossBarColor;

        if (bossHealthText != null)
            bossHealthText.text = $"{Mathf.Ceil(current):0} / {Mathf.Ceil(max):0}";
    }

    private void ResolveBossCombat()
    {
        if (bossCombat != null && bossCombat.GetCurrentHealth() > 0f)
            return;

        CafeteriaBossPattern boss = Object.FindFirstObjectByType<CafeteriaBossPattern>();
        if (boss == null)
        {
            bossCombat = null;
            return;
        }

        bossCombat = boss.GetComponent<EnemyCombat>();
    }

    private void SetRowVisible(RectTransform rowRoot, bool visible)
    {
        if (rowRoot != null && rowRoot.gameObject.activeSelf != visible)
        {
            rowRoot.gameObject.SetActive(visible);

            if (Application.isPlaying)
                LayoutRebuilder.ForceRebuildLayoutImmediate(barListRoot);
        }
    }

    private void SetBarFill(RectTransform fillRect, Image fillImage, float t)
    {
        t = Mathf.Clamp01(t);

        if (fillRect != null)
        {
            var parent = fillRect.parent as RectTransform;
            if (parent != null)
            {
                fillRect.anchorMin = new Vector2(0f, 0f);
                fillRect.anchorMax = new Vector2(Mathf.Max(0.001f, t), 1f);
                fillRect.pivot = new Vector2(0.5f, 0.5f);
                fillRect.anchoredPosition = Vector2.zero;
                fillRect.sizeDelta = Vector2.zero;
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
                return;
            }
        }

        if (fillImage != null && fillImage.sprite != null)
        {
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = t;
            return;
        }

        if (fillImage != null)
        {
            float full = barWidthFallback;
            var rt = fillImage.rectTransform;
            var p = rt.parent as RectTransform;
            if (p != null && p.rect.width > 2f)
                full = p.rect.width;

            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, full * t);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (!gameObject.scene.IsValid()) return;

        ResolveUiReferences();
        RefreshEditorPreview();
    }
#endif

    private void RefreshEditorPreview()
    {
        SetRowVisible(healthRowRoot, true);
        SetRowVisible(armorRowRoot, true);
        SetRowVisible(bossRowRoot, true);

        SetBarFill(healthBarFillRect, healthFillImage, 1f);
        SetBarFill(armorBarFillRect, armorFillImage, 1f);
        SetBarFill(bossHealthBarFillRect, bossHealthFillImage, 1f);

        if (healthFillImage != null)
            healthFillImage.color = healthBarColor;

        if (armorFillImage != null)
            armorFillImage.color = armorBarColor;

        if (bossHealthFillImage != null)
            bossHealthFillImage.color = bossBarColor;

        if (healthText != null) healthText.text = "HP";
        if (armorText != null) armorText.text = "Armor";
        if (bossHealthText != null) bossHealthText.text = "Boss HP";

        //if (Application.isPlaying && barListRoot != null)
        //    LayoutRebuilder.ForceRebuildLayoutImmediate(barListRoot);
    }

    private void EnsureRunTimerHud()
    {
        if (!gameObject.scene.IsValid()) return;

        RectTransform canvasRt = transform as RectTransform;
        if (canvasRt == null) return;

        if (runTimerText == null)
        {
            Transform found = transform.Find("RunTimerPanel/RunTimerText");
            if (found != null)
                runTimerText = found.GetComponent<Text>();
        }

        if (runTimerText == null)
            CreateRunTimerHud(canvasRt);
        else
            ApplyRunTimerBottomRightLayout(runTimerText.rectTransform);
    }

    private void BindGameRunTimer()
    {
        if (!Application.isPlaying || runTimerText == null) return;

        if (runTimer == null)
            runTimer = GetComponent<GameRunTimer>();

        if (runTimer == null)
        {
            Debug.LogWarning("PlayerHealthUI: No GameRunTimer found on HealthUI.");
            return;
        }

        runTimer.BindTimerText(runTimerText);
    }

    private void CreateRunTimerHud(RectTransform canvasRt)
    {
        const float margin = 18f;
        const float panelW = 152f;
        const float panelH = 48f;

        GameObject panelObj = new GameObject("RunTimerPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform panelRt = panelObj.GetComponent<RectTransform>();
        panelRt.SetParent(canvasRt, false);
        panelRt.anchorMin = new Vector2(1f, 0f);
        panelRt.anchorMax = new Vector2(1f, 0f);
        panelRt.pivot = new Vector2(1f, 0f);
        panelRt.anchoredPosition = new Vector2(-18f, 18f);
        panelRt.sizeDelta = new Vector2(panelW, panelH);

        Image panelBg = panelObj.GetComponent<Image>();
        panelBg.color = new Color(0.08f, 0.08f, 0.1f, 0.88f);
        panelBg.raycastTarget = false;

        GameObject textObj = new GameObject("RunTimerText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.SetParent(panelRt, false);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10f, 6f);
        textRt.offsetMax = new Vector2(-10f, -6f);

        Text txt = textObj.GetComponent<Text>();
        txt.text = "0:00";
        txt.fontSize = 22;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.raycastTarget = false;

        Outline outline = textObj.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(1.25f, -1.25f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
            txt.font = font;

        panelRt.SetAsLastSibling();
        runTimerText = txt;
    }

    private void ApplyRunTimerBottomRightLayout(RectTransform textRt)
    {
        if (textRt == null) return;

        RectTransform canvasRt = transform as RectTransform;
        if (canvasRt == null) return;

        RectTransform parent = textRt.parent as RectTransform;

        if (parent != null && parent.name == "RunTimerPanel")
        {
            parent.SetParent(canvasRt, false);
            parent.anchorMin = new Vector2(1f, 0f);
            parent.anchorMax = new Vector2(1f, 0f);
            parent.pivot = new Vector2(1f, 0f);
            parent.anchoredPosition = new Vector2(-18f, 18f);
            parent.sizeDelta = new Vector2(152f, 48f);
            parent.SetAsLastSibling();
            return;
        }

        textRt.SetParent(canvasRt, false);
        textRt.anchorMin = new Vector2(1f, 0f);
        textRt.anchorMax = new Vector2(1f, 0f);
        textRt.pivot = new Vector2(1f, 0f);
        textRt.anchoredPosition = new Vector2(-18f, 18f);
        textRt.sizeDelta = new Vector2(152f, 48f);
        textRt.SetAsLastSibling();
    }
}
