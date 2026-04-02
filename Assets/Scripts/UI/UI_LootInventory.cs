using CodeMonkey.Utils;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_LootInventory : MonoBehaviour
{
    private Inventory inventory;

    private Transform lootSlotContainer;
    private Transform lootSlotTemplate;

    private PlayerInventoryInteraction player;

    private void Awake()
    {
        lootSlotContainer = transform.Find("lootSlotContainer");

        if (lootSlotContainer == null)
        {
            lootSlotContainer = transform.Find("Scroll View_Loot/Viewport/lootSlotContainer");
        }

        if (lootSlotContainer == null)
        {
            Debug.LogError("UI_LootInventory: lootSlotContainer not found!");
            return;
        }

        lootSlotTemplate = lootSlotContainer.Find("lootSlotTemplate");

        if (lootSlotTemplate == null)
        {
            Debug.LogError("UI_LootInventory: lootSlotTemplate not found!");
            return;
        }

        lootSlotTemplate.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (inventory != null)
        {
            inventory.OnLootListChanged += Inventory_OnLootListChanged;
        }

        RefreshLootItems();
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.OnLootListChanged -= Inventory_OnLootListChanged;
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
            this.inventory.OnLootListChanged -= Inventory_OnLootListChanged;
        }

        this.inventory = inventory;

        if (isActiveAndEnabled && this.inventory != null)
        {
            this.inventory.OnLootListChanged += Inventory_OnLootListChanged;
        }

        if (RunObjectiveManager.Instance != null)
        {
            RunObjectiveManager.Instance.SetInventory(inventory);
        }

        RefreshLootItems();
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnLootListChanged -= Inventory_OnLootListChanged;
        }
    }

    private void Inventory_OnLootListChanged(object sender, EventArgs e)
    {
        RefreshLootItems();
    }

    private void RefreshLootItems()
    {
        if (lootSlotContainer == null || lootSlotTemplate == null || inventory == null)
        {
            return;
        }

        foreach (Transform child in lootSlotContainer)
        {
            if (child == lootSlotTemplate) continue;
            Destroy(child.gameObject);
        }

        int x = 0;
        int y = 0;

        float slotSize = 52f;
        int columnCount = 5;

        foreach (Item item in inventory.GetLootList())
        {
            if (item == null || item.definition == null) continue;
            if (!item.definition.IsLoot()) continue;

            RectTransform slot =
                Instantiate(lootSlotTemplate, lootSlotContainer).GetComponent<RectTransform>();

            slot.gameObject.SetActive(true);

            Button_UI buttonUI = slot.GetComponent<Button_UI>();

            if (buttonUI != null)
            {
                buttonUI.ClickFunc = () =>
                {
                    // TODO: loot detail
                };

                buttonUI.MouseRightClickFunc = () =>
                {
                    if (player == null) return;

                    Item dropItem = new Item
                    {
                        definition = item.definition,
                        amount = 1,
                        worldScale = item.worldScale
                    };

                    inventory.RemoveLoot(item);
                    ItemWorld.DropItem(player.GetPosition(), dropItem);
                };
            }

            float topOffset = 8f;
            slot.anchoredPosition = new Vector2(x * slotSize, topOffset - y * slotSize);

            Transform imageTransform = slot.Find("image");
            if (imageTransform != null)
            {
                Image image = imageTransform.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = item.GetSprite();
                    image.preserveAspect = true;

                    RectTransform imageRect = imageTransform.GetComponent<RectTransform>();
                    float iconSize = 50f * item.GetUIScale();
                    imageRect.sizeDelta = new Vector2(iconSize, iconSize);
                }
            }

            Transform amountTransform = slot.Find("amountText");
            if (amountTransform != null)
            {
                TextMeshProUGUI text = amountTransform.GetComponent<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = item.amount > 1 ? item.amount.ToString() : "";
                }
            }

            x++;
            if (x >= columnCount)
            {
                x = 0;
                y++;
            }
        }

        int count = inventory.GetLootList().Count;
        int rowCount = Mathf.CeilToInt(count / (float)columnCount);
        rowCount = Mathf.Max(1, rowCount);

        RectTransform containerRect = lootSlotContainer.GetComponent<RectTransform>();

        float viewportHeight = 166f;
        float padding = 10f;
        float height = rowCount * slotSize + padding;
        height = Mathf.Max(viewportHeight, height);

        containerRect.sizeDelta = new Vector2(containerRect.sizeDelta.x, height);
    }
}