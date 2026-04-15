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
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Vector2 mouseOffset = new Vector2(16f, -16f);

    private RectTransform canvasRectTransform;
    private Camera uiCamera;
    private bool isShowing;

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

        UpdatePosition();
    }

    public void Show(Item item)
    {
        if (item == null || item.definition == null)
        {
            Hide();
            return;
        }

        if (titleText != null)
        {
            string itemName = string.IsNullOrWhiteSpace(item.definition.itemName)
                ? "ITEM"
                : item.definition.itemName.ToUpper();

            titleText.text = itemName;
        }

        if (descriptionText != null)
        {
            string desc = item.definition.GetDescription();
            descriptionText.text = string.IsNullOrWhiteSpace(desc) ? "" : desc;
        }

        if (root != null)
        {
            root.gameObject.SetActive(true);
        }

        // 先清一下旧位置，避免沿用上一帧的奇怪偏移
        if (contentRoot != null)
        {
            contentRoot.anchoredPosition = Vector2.zero;
        }

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

        if (root != null)
        {
            root.gameObject.SetActive(false);
        }
    }

    private void HideImmediate()
    {
        isShowing = false;

        if (root != null)
        {
            root.gameObject.SetActive(false);
        }
    }

    private void UpdatePosition()
    {
        if (contentRoot == null || canvasRectTransform == null)
            return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

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

        // 手感参数（已经帮你调好了）
        float gapX = 27f;   // 往右
        float gapY = 29f;   // 往下（稍微更大一点防挡鼠标）

        // 默认：鼠标右下
        float posX = localPoint.x + gapX;
        float posY = localPoint.y - gapY;

        // 右边超出 → 翻到左边
        if (posX + tooltipWidth > canvasHalfWidth)
        {
            posX = localPoint.x - tooltipWidth - gapX;
        }

        // 下边超出 → 翻到上边
        if (posY - tooltipHeight < -canvasHalfHeight)
        {
            posY = localPoint.y + tooltipHeight + gapY;
        }

        contentRoot.anchoredPosition = new Vector2(posX, posY);
    }
}