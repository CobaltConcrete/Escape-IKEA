using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EquipmentSlotUI : MonoBehaviour
{
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI amountText;

    private void Awake()
    {
        ClearSlot();
    }

    public void SetItem(Item item)
    {
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
    }

    public void ClearSlot()
    {
        itemImage.sprite = null;
        itemImage.enabled = false;
        itemImage.rectTransform.localScale = Vector3.one;

        if (amountText != null)
        {
            amountText.text = "";
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