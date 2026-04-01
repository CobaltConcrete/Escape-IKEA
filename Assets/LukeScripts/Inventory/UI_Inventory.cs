using CodeMonkey.Utils;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Progress;

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
        itemSlotTemplate = itemSlotContainer.Find("itemSlotTemplate");

        itemSlotTemplate.gameObject.SetActive(false);
    }

    //private void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.K))
    //    {
    //        UseFirstItem();
    //    }
    //}

    public void SetPlayer(PlayerInventoryInteraction player)
    {
        this.player = player;
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

    //private void UseFirstItem()
    //{
    //    if (inventory == null) return;

    //    var itemList = inventory.GetItemList();

    //    if (itemList.Count > 0)
    //    {
    //        Item item = itemList[0];

    //        if (item.itemType == Item.ItemType.SpeedPill)
    //        {

    //           playerMovement.BoostSpeedFor10Seconds();
    //        }

    //        inventory.UseItem(item);
    //    }
    //}

    //private void RefreshInventoryItems()
    //{
    //    foreach (Transform child in itemSlotContainer)
    //    {
    //        if (child == itemSlotTemplate) continue;
    //        Destroy(child.gameObject);
    //    }

    //    int x = 0;
    //    int y = 0;
    //    float itemSlotCellSize = 50f;

    //    foreach (Item item in inventory.GetItemList())
    //    {
    //        RectTransform itemSlotRectTransform =
    //            Instantiate(itemSlotTemplate, itemSlotContainer).GetComponent<RectTransform>();

    //        itemSlotRectTransform.gameObject.SetActive(true);

    //        Button_UI buttonUI = itemSlotRectTransform.GetComponent<Button_UI>();
    //        if (buttonUI == null)
    //        {
    //            return;
    //        }

    //        buttonUI.ClickFunc = () =>
    //        {
    //            // Use item
    //            inventory.UseItem(item);
    //        };

    //        buttonUI.MouseRightClickFunc = () =>
    //        {
    //            // Drop item
    //            Item duplicateItem = new Item { itemType = item.itemType, amount = item.amount };
    //            inventory.RemoveItem(item);
    //            ItemWorld.DropItem(player.GetPosition(), duplicateItem);
    //        };

    //        itemSlotRectTransform.anchoredPosition = new Vector2(x * itemSlotCellSize, -y * itemSlotCellSize);
    //        Transform imageTransform = itemSlotRectTransform.Find("image");
    //        Image image = imageTransform.GetComponent<Image>();
    //        image.sprite = item.GetSprite();

    //        TextMeshProUGUI uiText = itemSlotRectTransform.Find("amountText").GetComponent<TextMeshProUGUI>();
    //        if (item.amount > 1)
    //        {
    //            uiText.SetText(item.amount.ToString());
    //        }
    //        else
    //        {
    //            uiText.SetText("");
    //        }
    //        image.preserveAspect = true;

    //        RectTransform imageRectTransform = imageTransform.GetComponent<RectTransform>();
    //        float iconSize = 50f * item.GetUIScale();
    //        imageRectTransform.sizeDelta = new Vector2(iconSize, iconSize);

    //        x++;
    //        if (x > 4)
    //        {
    //            x = 0;
    //            y++;
    //        }
    //    }
    //}

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

        foreach (Item item in inventory.GetItemList())
        {
            if (item.IsLoot()) continue;
            RectTransform itemSlotRectTransform =
                Instantiate(itemSlotTemplate, itemSlotContainer).GetComponent<RectTransform>();

            itemSlotRectTransform.gameObject.SetActive(true);

            Button_UI buttonUI = itemSlotRectTransform.GetComponent<Button_UI>();
            if (buttonUI == null)
            {
                return;
            }

            buttonUI.ClickFunc = () =>
            {
                inventory.UseItem(item);
            };

            buttonUI.MouseRightClickFunc = () =>
            {
                Item duplicateItem = item.Clone();

                inventory.RemoveItem(item);
                ItemWorld.DropItem(player.GetPosition(), duplicateItem);
            };

            float topOffset = 8f;
            itemSlotRectTransform.anchoredPosition = new Vector2(x * itemSlotCellSize, topOffset - y * itemSlotCellSize);

            Transform imageTransform = itemSlotRectTransform.Find("image");
            Image image = imageTransform.GetComponent<Image>();
            image.sprite = item.GetSprite();
            image.preserveAspect = true;

            TextMeshProUGUI uiText = itemSlotRectTransform.Find("amountText").GetComponent<TextMeshProUGUI>();
            if (item.amount > 1)
            {
                uiText.SetText(item.amount.ToString());
            }
            else
            {
                uiText.SetText("");
            }

            RectTransform imageRectTransform = imageTransform.GetComponent<RectTransform>();
            float iconSize = 50f * item.GetUIScale();
            imageRectTransform.sizeDelta = new Vector2(iconSize, iconSize);

            x++;
            if (x >= columnCount)
            {
                x = 0;
                y++;
            }
        }

        int itemCount = inventory.GetItemList().Count;
        int rowCount = Mathf.CeilToInt(itemCount / (float)columnCount);
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