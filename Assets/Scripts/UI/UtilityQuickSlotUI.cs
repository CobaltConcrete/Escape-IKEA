using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static EquipmentEnum;

public class UtilityQuickSlotUI : MonoBehaviour
{
    [SerializeField] private EquipmentData equipmentData;
    [SerializeField] private Image itemImage;
    [SerializeField] private TextMeshProUGUI amountText;

    private void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (equipmentData == null)
        {
            Debug.LogWarning("UtilityQuickSlotUI: equipmentData is null");
            return;
        }

        Item utilityItem = equipmentData.GetEquippedItem(EquipTag.Utility);

        if (utilityItem == null)
        {
            itemImage.enabled = false;
            itemImage.sprite = null;
            itemImage.rectTransform.localScale = Vector3.one;

            if (amountText != null)
            {
                amountText.text = "";
            }

            return;
        }

        itemImage.enabled = true;
        itemImage.sprite = utilityItem.GetSprite();
        itemImage.preserveAspect = true;
        itemImage.rectTransform.localScale = Vector3.one * utilityItem.GetUIScale();

        if (amountText != null)
        {
            if (utilityItem.IsStackable() && utilityItem.amount > 1)
            {
                amountText.text = utilityItem.amount.ToString();
            }
            else
            {
                amountText.text = "";
            }
        }
    }
}