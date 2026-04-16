using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_ItemTooltip : MonoBehaviour
{
    public static UI_ItemTooltip Instance { get; private set; }

    [SerializeField] private RectTransform root;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private Canvas parentCanvas;

    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI durabilityText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private GameObject spacerAfterTitle;
    [SerializeField] private GameObject spacerAfterDurability;

    [SerializeField] private Vector2 mouseOffset = new Vector2(16f, -16f);

    private RectTransform canvasRectTransform;
    private Camera uiCamera;
    private bool isShowing;

    private Item currentItem;
    public Item CurrentItem => currentItem;
    public bool IsShowing => isShowing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (root == null)
        {
            root = GetComponent<RectTransform>();
        }

        if (parentCanvas == null)
        {
            parentCanvas = GetComponentInParent<Canvas>();
        }

        if (parentCanvas != null)
        {
            canvasRectTransform = parentCanvas.GetComponent<RectTransform>();

            if (parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = null;
            }
            else
            {
                uiCamera = parentCanvas.worldCamera;
            }
        }

        HideImmediate();
    }

    private void Update()
    {
        if (!isShowing) return;

        RefreshCurrentItemVisuals();
        UpdatePosition();
    }

    public void Show(Item item)
    {
        if (item == null || item.definition == null)
        {
            Hide();
            return;
        }

        currentItem = item;

        if (root != null)
        {
            root.gameObject.SetActive(true);
        }

        if (contentRoot != null)
        {
            contentRoot.anchoredPosition = Vector2.zero;
        }

        RefreshCurrentItemVisuals();

        Canvas.ForceUpdateCanvases();

        if (contentRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        }

        isShowing = true;
        UpdatePosition();
    }

    public void Hide()
    {
        isShowing = false;
        currentItem = null;

        if (root != null)
        {
            root.gameObject.SetActive(false);
        }
    }

    private void HideImmediate()
    {
        isShowing = false;
        currentItem = null;

        if (root != null)
        {
            root.gameObject.SetActive(false);
        }
    }

    private void RefreshCurrentItemVisuals()
    {
        if (currentItem == null || currentItem.definition == null)
        {
            Hide();
            return;
        }

        bool hasDurability = false;
        bool hasDescription = false;

        // ===== Title =====
        if (titleText != null)
        {
            string itemName = string.IsNullOrWhiteSpace(currentItem.definition.itemName)
                ? "ITEM"
                : currentItem.definition.itemName.ToUpper();

            titleText.text = itemName;
        }

        // ===== Durability =====
        if (durabilityText != null)
        {
            if (currentItem.IsArmor())
            {
                currentItem.InitializeRuntimeDataIfNeeded();

                float current = Mathf.Max(0f, currentItem.GetArmorCurrentDurability());
                float max = currentItem.GetArmorMaxDurability();

                durabilityText.gameObject.SetActive(true);
                durabilityText.text = $"Durability: {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";

                hasDurability = true;
            }
            else
            {
                durabilityText.gameObject.SetActive(false);
                durabilityText.text = "";
            }
        }

        // ===== Description =====
        if (descriptionText != null)
        {
            string desc = currentItem.definition.GetDescription();

            if (string.IsNullOrWhiteSpace(desc))
            {
                descriptionText.gameObject.SetActive(false);
                descriptionText.text = "";
            }
            else
            {
                descriptionText.gameObject.SetActive(true);
                descriptionText.text = desc;

                hasDescription = true;
            }
        }

        // ===== Spacer 控制（关键就在这里）=====
        if (spacerAfterTitle != null)
        {
            // 只有 durability 存在才需要 title -> durability 间距
            spacerAfterTitle.SetActive(hasDurability);
        }

        if (spacerAfterDurability != null)
        {
            // 只有 durability + description 都存在才需要间距
            spacerAfterDurability.SetActive(hasDurability && hasDescription);
        }

        // ===== 刷新布局 =====
        if (contentRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
        }
    }

    private void UpdatePosition()
    {
        if (contentRoot == null || canvasRectTransform == null)
            return;

        Vector2 localPoint;
        bool success = RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            Input.mousePosition,
            uiCamera,
            out localPoint
        );

        if (!success)
            return;

        float tooltipWidth = contentRoot.rect.width;
        float tooltipHeight = contentRoot.rect.height;

        float canvasHalfWidth = canvasRectTransform.rect.width * 0.5f;
        float canvasHalfHeight = canvasRectTransform.rect.height * 0.5f;

        float gapX = 27f;
        float gapY = 29f;

        float posX = localPoint.x + gapX;
        float posY = localPoint.y - gapY;

        if (posX + tooltipWidth > canvasHalfWidth)
        {
            posX = localPoint.x - tooltipWidth - gapX;
        }

        if (posY - tooltipHeight < -canvasHalfHeight)
        {
            posY = localPoint.y + tooltipHeight + gapY;
        }

        contentRoot.anchoredPosition = new Vector2(posX, posY);
    }
}