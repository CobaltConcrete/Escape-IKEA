using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentSlotUI : MonoBehaviour
{
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI amountText;

    private UI_ItemSlotTooltipTrigger tooltipTrigger;
    private Item currentItem;

    private void Awake()
    {
        tooltipTrigger = GetComponent<UI_ItemSlotTooltipTrigger>();

        if (tooltipTrigger == null)
        {
            tooltipTrigger = gameObject.AddComponent<UI_ItemSlotTooltipTrigger>();
        }

        ClearSlot();
    }

    public void SetItem(Item item)
    {
        currentItem = item;

        if (item == null)
        {
            ClearSlot();
            return;
        }

        itemImage.enabled = true;
        itemImage.sprite = item.GetSprite();
        itemImage.preserveAspect = true;

        itemImage.rectTransform.localScale = Vector3.one;

        float scale = item.GetUIScale();
        itemImage.rectTransform.localScale = new Vector3(scale, scale, 1f);

        UpdateAmountText(item);
        RefreshDurabilityBar(item);

        if (tooltipTrigger != null)
        {
            tooltipTrigger.SetItem(currentItem);
        }
    }

    public void ClearSlot()
    {
        currentItem = null;

        itemImage.sprite = null;
        itemImage.enabled = false;
        itemImage.rectTransform.localScale = Vector3.one;

        if (amountText != null)
        {
            amountText.text = "";
        }

        Transform durabilityTransform = transform.Find("durability");
        if (durabilityTransform != null)
        {
            durabilityTransform.gameObject.SetActive(false);
        }

        if (tooltipTrigger != null)
        {
            tooltipTrigger.SetItem(null);
        }
    }

    private void UpdateAmountText(Item item)
    {
        if (amountText == null) return;

        if (item.IsStackable() && item.amount > 1)
        {
            amountText.text = item.amount.ToString();
        }
        else
        {
            amountText.text = "";
        }
    }

    private void RefreshDurabilityBar(Item item)
    {
        Transform durabilityTransform = transform.Find("durability");
        if (durabilityTransform == null)
        {
            return;
        }

        if (item == null || !item.IsArmor())
        {
            durabilityTransform.gameObject.SetActive(false);
            return;
        }

        item.InitializeRuntimeDataIfNeeded();

        durabilityTransform.gameObject.SetActive(true);

        Transform fillTransform = durabilityTransform.Find("Fill");
        if (fillTransform == null)
        {
            return;
        }

        RectTransform fillRect = fillTransform.GetComponent<RectTransform>();
        Image fillImage = fillTransform.GetComponent<Image>();

        float maxDurability = item.GetArmorMaxDurability();
        float currentDurability = Mathf.Max(0f, item.GetArmorCurrentDurability());
        float t = maxDurability > 0f ? Mathf.Clamp01(currentDurability / maxDurability) : 0f;

        if (fillRect != null)
        {
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(Mathf.Max(0.001f, t), 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero;
        }

        if (fillImage != null)
        {
            fillImage.color = new Color(0.2f, 0.55f, 1f, 1f);
        }
    }
}