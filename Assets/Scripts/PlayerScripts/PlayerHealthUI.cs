using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

    [SerializeField] private RectTransform healthBarFillRect;

    [SerializeField] private float barWidthFallback = 200f;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Text healthText;
    [SerializeField] private RectTransform bossHealthBarFillRect;
    [SerializeField] private Image bossHealthFillImage;
    [SerializeField] private Text bossHealthText;
    [SerializeField] private Color bossBarColor = Color.red;

    [SerializeField] private Text runTimerText;
    [SerializeField] private GameRunTimer runTimer;

    private EnemyCombat bossCombat;
    private RectTransform autoBossBarRoot;

    private void Awake()
    {
        ResolvePlayerHealth();
    }

    private void OnEnable()
    {
        ResolvePlayerHealth();
        if (playerHealth != null)
            playerHealth.OnHealthChanged += Refresh;
        EnsureRunTimerHud();
        EnsureBossBarHud();
        if (!Application.isPlaying)
            RefreshBossEditorPreview();
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= Refresh;
    }

    private void Start()
    {
        ResolvePlayerHealth();
        EnsureBossBarHud();
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
        if (bossHealthBarFillRect == null && bossHealthFillImage == null)
            EnsureBossBarHud();
        ResolveBossCombat();
        Refresh();
    }

    private void ResolvePlayerHealth()
    {
        if (playerHealth == null)
            playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
    }

    private void Refresh()
    {
        if (playerHealth == null) return;

        float t = playerHealth.MaxHealth > 0f
            ? Mathf.Clamp01(playerHealth.CurrentHealth / playerHealth.MaxHealth)
            : 0f;

        if (healthBarFillRect != null)
        {
            var parent = healthBarFillRect.parent as RectTransform;
            if (parent != null)
            {
                healthBarFillRect.anchorMin = new Vector2(0f, 0f);
                healthBarFillRect.anchorMax = new Vector2(Mathf.Max(0.001f, t), 1f);
                healthBarFillRect.pivot = new Vector2(0.5f, 0.5f);
                healthBarFillRect.anchoredPosition = Vector2.zero;
                healthBarFillRect.sizeDelta = Vector2.zero;
                healthBarFillRect.offsetMin = Vector2.zero;
                healthBarFillRect.offsetMax = Vector2.zero;
            }
        }
        else if (healthFillImage != null && healthFillImage.sprite != null)
        {
            healthFillImage.type = Image.Type.Filled;
            healthFillImage.fillMethod = Image.FillMethod.Horizontal;
            healthFillImage.fillAmount = t;
        }
        else if (healthFillImage != null)
        {
            float full = barWidthFallback;
            var rt = healthFillImage.rectTransform;
            var p = rt.parent as RectTransform;
            if (p != null && p.rect.width > 2f)
                full = p.rect.width;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, full * t);
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.Ceil(playerHealth.CurrentHealth):0} / {Mathf.Ceil(playerHealth.MaxHealth):0}";
        }

        RefreshBoss();
    }

    private void ResolveBossCombat()
    {
        if (bossCombat != null) return;
        CafeteriaBossPattern boss = Object.FindFirstObjectByType<CafeteriaBossPattern>();
        if (boss == null) return;
        bossCombat = boss.GetComponent<EnemyCombat>();
    }

    private void RefreshBoss()
    {
        if (!Application.isPlaying)
        {
            RefreshBossEditorPreview();
            return;
        }

        if (!BossRoomController.IsPlayerInsideBossRoom())
        {
            SetBossBarVisible(false);
            if (bossHealthText != null) bossHealthText.text = "";
            return;
        }

        if (bossCombat == null)
        {
            SetBossBarVisible(false);
            if (bossHealthText != null) bossHealthText.text = "";
            return;
        }

        if (bossCombat.GetCurrentHealth() <= 0f)
        {
            bossCombat = null;
            SetBossBarVisible(false);
            if (bossHealthText != null) bossHealthText.text = "";
            return;
        }

        SetBossBarVisible(true);
        float max = bossCombat.GetMaxHealth();
        float current = bossCombat.GetCurrentHealth();
        float t = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        if (bossHealthBarFillRect != null)
        {
            bossHealthBarFillRect.anchorMin = new Vector2(0f, 0f);
            bossHealthBarFillRect.anchorMax = new Vector2(Mathf.Max(0.001f, t), 1f);
            bossHealthBarFillRect.anchoredPosition = Vector2.zero;
            bossHealthBarFillRect.sizeDelta = Vector2.zero;
            bossHealthBarFillRect.offsetMin = Vector2.zero;
            bossHealthBarFillRect.offsetMax = Vector2.zero;
        }
        else if (bossHealthFillImage != null)
        {
            bossHealthFillImage.type = Image.Type.Filled;
            bossHealthFillImage.fillMethod = Image.FillMethod.Horizontal;
            bossHealthFillImage.fillAmount = t;
        }

        if (bossHealthText != null)
        {
            bossHealthText.text = $"{Mathf.Ceil(current):0} / {Mathf.Ceil(max):0}";
        }
    }

    private void SetBossBarVisible(bool visible)
    {
        if (autoBossBarRoot != null)
            autoBossBarRoot.gameObject.SetActive(visible);

        if (bossHealthBarFillRect != null)
            bossHealthBarFillRect.gameObject.SetActive(visible);

        if (bossHealthFillImage != null)
            bossHealthFillImage.gameObject.SetActive(visible);

        if (bossHealthText != null)
            bossHealthText.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Editor: show full red boss bar under the player bar so you can see and edit it in the Scene view.
    /// </summary>
    private void RefreshBossEditorPreview()
    {
        EnsureBossBarHud();
        if (bossHealthBarFillRect == null && bossHealthFillImage == null) return;

        SetBossBarVisible(true);

        if (bossHealthBarFillRect != null)
        {
            var parent = bossHealthBarFillRect.parent as RectTransform;
            if (parent != null)
            {
                bossHealthBarFillRect.anchorMin = new Vector2(0f, 0f);
                bossHealthBarFillRect.anchorMax = new Vector2(1f, 1f);
                bossHealthBarFillRect.pivot = new Vector2(0.5f, 0.5f);
                bossHealthBarFillRect.anchoredPosition = Vector2.zero;
                bossHealthBarFillRect.sizeDelta = Vector2.zero;
                bossHealthBarFillRect.offsetMin = Vector2.zero;
                bossHealthBarFillRect.offsetMax = Vector2.zero;
            }
        }
        else if (bossHealthFillImage != null && bossHealthFillImage.sprite != null)
        {
            bossHealthFillImage.type = Image.Type.Filled;
            bossHealthFillImage.fillMethod = Image.FillMethod.Horizontal;
            bossHealthFillImage.fillAmount = 1f;
        }

        if (bossHealthFillImage != null)
            bossHealthFillImage.color = bossBarColor;

        if (bossHealthText != null)
            bossHealthText.text = "Boss HP";
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        EnsureBossBarHud();
        RefreshBossEditorPreview();
    }
#endif

    /// <summary>
    /// Creates or finds the boss HP row (red bar under the player health bar). Exists in edit mode for Scene view authoring.
    /// </summary>
    private void EnsureBossBarHud()
    {
        if (bossHealthBarFillRect != null && bossHealthFillImage != null)
        {
            if (autoBossBarRoot == null && bossHealthBarFillRect != null)
                autoBossBarRoot = bossHealthBarFillRect.parent as RectTransform;
            return;
        }

        if (bossHealthBarFillRect == null || bossHealthFillImage == null)
        {
            Transform fillTf = transform.Find("BossHealthLine/Fill");
            if (fillTf != null)
            {
                bossHealthBarFillRect = fillTf.GetComponent<RectTransform>();
                bossHealthFillImage = fillTf.GetComponent<Image>();
                autoBossBarRoot = fillTf.parent as RectTransform;

                if (bossHealthText == null)
                {
                    Transform labelTf = transform.Find("BossHealthLine/BossHealthLabel");
                    if (labelTf != null)
                        bossHealthText = labelTf.GetComponent<Text>();
                }
            }
        }

        if (bossHealthBarFillRect != null && bossHealthFillImage != null) return;

        RectTransform playerBarRoot = null;
        if (healthBarFillRect != null)
            playerBarRoot = healthBarFillRect.parent as RectTransform;
        if (playerBarRoot == null && healthFillImage != null)
            playerBarRoot = healthFillImage.rectTransform.parent as RectTransform;
        if (playerBarRoot == null && healthFillImage != null)
            playerBarRoot = healthFillImage.rectTransform;
        if (playerBarRoot == null) return;

        RectTransform parent = playerBarRoot.parent as RectTransform;
        if (parent == null) return;

        GameObject bossRootObj = new GameObject("BossHealthLine", typeof(RectTransform), typeof(Image));
        RectTransform bossRoot = bossRootObj.GetComponent<RectTransform>();
        bossRoot.SetParent(parent, false);
        bossRoot.anchorMin = playerBarRoot.anchorMin;
        bossRoot.anchorMax = playerBarRoot.anchorMax;
        bossRoot.pivot = playerBarRoot.pivot;
        bossRoot.sizeDelta = playerBarRoot.sizeDelta;
        float y = playerBarRoot.anchoredPosition.y - Mathf.Max(12f, playerBarRoot.rect.height + 6f);
        bossRoot.anchoredPosition = new Vector2(playerBarRoot.anchoredPosition.x, y);
        int playerIndex = playerBarRoot.GetSiblingIndex();
        bossRoot.SetSiblingIndex(Mathf.Min(playerIndex + 1, parent.childCount - 1));

        Image rootImage = bossRootObj.GetComponent<Image>();
        rootImage.color = new Color(0f, 0f, 0f, 0.45f);

        GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.SetParent(bossRoot, false);
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fillObj.GetComponent<Image>();
        fillImage.color = bossBarColor;

        bossHealthBarFillRect = fillRect;
        bossHealthFillImage = fillImage;
        autoBossBarRoot = bossRoot;

        GameObject labelObj = new GameObject("BossHealthLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.SetParent(bossRoot, false);
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        Text labelTxt = labelObj.GetComponent<Text>();
        labelTxt.text = "Boss HP";
        labelTxt.fontSize = 14;
        labelTxt.alignment = TextAnchor.MiddleCenter;
        labelTxt.color = Color.white;
        labelTxt.raycastTarget = false;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font != null)
            labelTxt.font = font;
        bossHealthText = labelTxt;

        if (Application.isPlaying)
            SetBossBarVisible(false);
    }

    /// <summary>
    /// Builds or finds the timer panel so it exists in the Scene view (edit mode) and at runtime.
    /// </summary>
    private void EnsureRunTimerHud()
    {
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
            runTimer = gameObject.AddComponent<GameRunTimer>();
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
        panelRt.anchoredPosition = new Vector2(-margin, margin);
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

        // Legacy or misplaced timer: anchor directly on the canvas bottom-right so it stays visible.
        textRt.SetParent(canvasRt, false);
        textRt.anchorMin = new Vector2(1f, 0f);
        textRt.anchorMax = new Vector2(1f, 0f);
        textRt.pivot = new Vector2(1f, 0f);
        textRt.anchoredPosition = new Vector2(-18f, 18f);
        textRt.sizeDelta = new Vector2(152f, 48f);
        textRt.SetAsLastSibling();
    }
}
