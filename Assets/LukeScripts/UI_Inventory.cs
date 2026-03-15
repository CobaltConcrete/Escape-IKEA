using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_Inventory : MonoBehaviour
{
    private Inventory inventory;
    private Transform itemSlotContainer;
    private Transform itemSlotTemplate;

    private void Awake()
    {
        itemSlotContainer = transform.Find("itemSlotContainer");
        itemSlotTemplate = itemSlotContainer.Find("itemSlotTemplate");

        itemSlotTemplate.gameObject.SetActive(false);
    }

    public void SetInventory(Inventory inventory)
    {
        this.inventory = inventory;

        inventory.OnItemListChanged += Inventory_OnItemListChaned;

        RefreshInventoryItems();
    }

    private void Inventory_OnItemListChaned(object sender, EventArgs e)
    {
        RefreshInventoryItems();
    }

    private void RefreshInventoryItems()
    {
        foreach (Transform child in itemSlotContainer)
        {
            if (child == itemSlotTemplate) continue;
            Destroy(child.gameObject);
        }

        int x = 0;
        int y = 0;
        float itemSlotCellSize = 50f;

        foreach (Item item in inventory.GetItemList())
        {
            RectTransform itemSlotRectTransform =
                Instantiate(itemSlotTemplate, itemSlotContainer).GetComponent<RectTransform>();

            itemSlotRectTransform.gameObject.SetActive(true);
            itemSlotRectTransform.anchoredPosition = new Vector2(x * itemSlotCellSize, -y * itemSlotCellSize);

            Transform imageTransform = itemSlotRectTransform.Find("image");
            Image image = imageTransform.GetComponent<Image>();
            image.sprite = item.GetSprite();

            TextMeshProUGUI uiText = itemSlotRectTransform.Find("amountText").GetComponent<TextMeshProUGUI>();
            if(item.amount > 1)
            {
                uiText.SetText(item.amount.ToString());
            }
            else
            {
                uiText.SetText("");
            }
                image.preserveAspect = true;

            RectTransform imageRectTransform = imageTransform.GetComponent<RectTransform>();
            float iconSize = 50f * item.GetUIScale();
            imageRectTransform.sizeDelta = new Vector2(iconSize, iconSize);

            x++;
            if (x > 4)
            {
                x = 0;
                y++;
            }
        }
    }
}