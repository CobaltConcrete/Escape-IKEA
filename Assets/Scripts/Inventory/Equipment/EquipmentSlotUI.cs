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
}