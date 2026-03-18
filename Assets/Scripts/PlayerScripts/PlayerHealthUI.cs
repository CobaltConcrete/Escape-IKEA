using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;

    [SerializeField] private RectTransform healthBarFillRect;

    [SerializeField] private float barWidthFallback = 200f;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Text healthText;

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
        Refresh();
    }

    private void Update()
    {
        if (playerHealth == null)
            ResolvePlayerHealth();
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
    }
}
