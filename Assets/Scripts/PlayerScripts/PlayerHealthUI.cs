using UnityEngine;
using UnityEngine.UI;

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
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= Refresh;
    }

    private void Start()
    {
        ResolvePlayerHealth();
        TryCreateBossBarFromPlayerBar();
        Refresh();
    }

    private void Update()
    {
        if (playerHealth == null)
            ResolvePlayerHealth();
        if (bossHealthBarFillRect == null && bossHealthFillImage == null)
            TryCreateBossBarFromPlayerBar();
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

    private void TryCreateBossBarFromPlayerBar()
    {
        if (bossHealthBarFillRect != null || bossHealthFillImage != null) return;

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
        SetBossBarVisible(false);
    }
}
