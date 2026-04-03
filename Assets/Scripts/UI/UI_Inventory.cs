using CodeMonkey.Utils;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_Inventory : MonoBehaviour
{
    private Inventory inventory;
    private Transform itemSlotContainer;
    private Transform itemSlotTemplate;
    private PlayerInventoryInteraction player;
    public PlayerMovement playerMovement;

    private void Awake()
    {
        itemSlotContainer = transform.Find("itemSlotContainer");

        if (itemSlotContainer == null)
        {
            itemSlotContainer = transform.Find("Scroll View/Viewport/itemSlotContainer");
        }

        if (itemSlotContainer == null)
        {
            Debug.LogError("UI_Inventory: itemSlotContainer not found!");
            return;
        }

        itemSlotTemplate = itemSlotContainer.Find("itemSlotTemplate");

        if (itemSlotTemplate == null)
        {
            Debug.LogError("UI_Inventory: itemSlotTemplate not found!");
            return;
        }

        itemSlotTemplate.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (inventory != null)
        {
            inventory.OnItemListChanged += Inventory_OnItemListChaned;
        }

        RefreshInventoryItems();
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnItemListChanged -= Inventory_OnItemListChaned;
        }
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnItemListChanged -= Inventory_OnItemListChaned;
        }
    }

    public void SetPlayer(PlayerInventoryInteraction player)
    {
        this.player = player;
    }

    public void SetInventory(Inventory inventory)
    {
        if (this.inventory != null)
        {
            this.inventory.OnItemListChanged -= Inventory_OnItemListChaned;
        }

        this.inventory = inventory;

        if (isActiveAndEnabled && this.inventory != null)
        {
            this.inventory.OnItemListChanged += Inventory_OnItemListChaned;
        }

        RefreshInventoryItems();
    }

    private void Inventory_OnItemListChaned(object sender, EventArgs e)
    {
        RefreshInventoryItems();
    }

    private void RefreshInventoryItems()
    {
        if (itemSlotContainer == null || itemSlotTemplate == null || inventory == null)
        {
            return;
        }

        foreach (Transform child in itemSlotContainer)
        {
            if (child == itemSlotTemplate) continue;
            Destroy(child.gameObject);
        }

        int x = 0;
        int y = 0;

        float itemSlotCellSize = 52f;
        int columnCount = 5;

        int visibleNonLootCount = 0;

        foreach (Item item in inventory.GetItemList())
        {
            if (item == null || item.definition == null) continue;
            if (item.IsLoot()) continue;

            visibleNonLootCount++;

            RectTransform itemSlotRectTransform =
                Instantiate(itemSlotTemplate, itemSlotContainer).GetComponent<RectTransform>();

            itemSlotRectTransform.gameObject.SetActive(true);

            Button_UI buttonUI = itemSlotRectTransform.GetComponent<Button_UI>();
            if (buttonUI == null)
            {
                continue;
            }

            buttonUI.ClickFunc = () =>
            {
                inventory.UseItem(item);
            };

            buttonUI.MouseRightClickFunc = () =>
            {
                if (player == null) return;

                Item duplicateItem = item.Clone();
                inventory.RemoveItem(item);
                ItemWorld.DropItem(player.GetPosition(), duplicateItem);
            };

            float topOffset = 8f;
            itemSlotRectTransform.anchoredPosition =
                new Vector2(x * itemSlotCellSize, topOffset - y * itemSlotCellSize);

            Transform imageTransform = itemSlotRectTransform.Find("image");
            if (imageTransform != null)
            {
                Image image = imageTransform.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = item.GetSprite();
                    image.preserveAspect = true;

                    RectTransform imageRectTransform = imageTransform.GetComponent<RectTransform>();
                    float iconSize = 50f * item.GetUIScale();
                    imageRectTransform.sizeDelta = new Vector2(iconSize, iconSize);
                }
            }

            Transform amountTransform = itemSlotRectTransform.Find("amountText");
            if (amountTransform != null)
            {
                TextMeshProUGUI uiText = amountTransform.GetComponent<TextMeshProUGUI>();
                if (uiText != null)
                {
                    uiText.text = item.amount > 1 ? item.amount.ToString() : "";
                }
            }

            x++;
            if (x >= columnCount)
            {
                x = 0;
                y++;
            }
        }

        int rowCount = Mathf.CeilToInt(visibleNonLootCount / (float)columnCount);
        rowCount = Mathf.Max(1, rowCount);

        RectTransform containerRectTransform = itemSlotContainer.GetComponent<RectTransform>();

        float viewportHeight = 166f;
        float bottomPadding = 10f;
        float contentHeight = rowCount * itemSlotCellSize + bottomPadding;
        contentHeight = Mathf.Max(viewportHeight, contentHeight);

        containerRectTransform.sizeDelta = new Vector2(
            containerRectTransform.sizeDelta.x,
            contentHeight
        );
    }
}