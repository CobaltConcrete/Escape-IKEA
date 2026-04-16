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

    public void RefreshNow()
    {
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
        Item tooltipItemToRestore = null;

        if (UI_ItemTooltip.Instance != null && UI_ItemTooltip.Instance.IsShowing)
        {
            tooltipItemToRestore = UI_ItemTooltip.Instance.CurrentItem;
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

            UI_ItemSlotTooltipTrigger tooltipTrigger =
                itemSlotRectTransform.GetComponent<UI_ItemSlotTooltipTrigger>();

            if (tooltipTrigger == null)
            {
                tooltipTrigger = itemSlotRectTransform.gameObject.AddComponent<UI_ItemSlotTooltipTrigger>();
            }

            tooltipTrigger.SetItem(item);

            Button_UI buttonUI = itemSlotRectTransform.GetComponent<Button_UI>();
            if (buttonUI == null)
            {
                continue;
            }

            buttonUI.ClickFunc = () =>
            {
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayButtonClick();
                }

                inventory.UseItem(item);
            };

            buttonUI.MouseRightClickFunc = () =>
            {
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayButtonClick();
                }

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

            RefreshDurabilityBar(itemSlotRectTransform, item);

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
        if (tooltipItemToRestore != null && tooltipItemToRestore.definition != null)
        {
            bool stillExists = false;

            foreach (Item item in inventory.GetItemList())
            {
                if (item == tooltipItemToRestore)
                {
                    stillExists = true;
                    break;
                }
            }

            if (stillExists && UI_ItemTooltip.Instance != null)
            {
                UI_ItemTooltip.Instance.Show(tooltipItemToRestore);
            }
        }
    }

    private void RefreshDurabilityBar(RectTransform slotRectTransform, Item item)
    {
        Transform durabilityTransform = slotRectTransform.Find("durability");
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