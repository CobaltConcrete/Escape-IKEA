using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EquipmentEnum;

public class PlayerInventoryInteraction : MonoBehaviour
{
    private Inventory inventory;

    [SerializeField] private UI_Inventory uiInventory;
    [SerializeField] private EquipmentUI equipmentUI;
    [SerializeField] private EquipmentData equipmentData;
    [SerializeField] private Dialogue playerDialogue;

    [Header("Item Definitions")]

    private SpriteRenderer playerSpriteRenderer;
    private PlayerMovement playerMovement;
    private ItemDefinition currentUtilityChainDefinition;

    private Coroutine flashCoroutine;
    private Color baseColor;

    private bool firstItemFound;

    [Header("Utility Priority")]
    [SerializeField] private List<ItemDefinition> utilityPriorityList;

    private void Awake()
    {
        inventory = new Inventory(UseItem);
        playerMovement = GetComponent<PlayerMovement>();

        playerSpriteRenderer = transform.Find("PlayerSprite").GetComponent<SpriteRenderer>();
        baseColor = playerSpriteRenderer.color;

        firstItemFound = false;
    }

    private void Start()
    {
        uiInventory.SetPlayer(this);
        uiInventory.SetInventory(inventory);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            UseEquippedUtility();
        }
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    private void TryAutoEquipPickedUpItem(Item pickedItem)
    {
        if (pickedItem == null || pickedItem.definition == null) return;
        if (!ItemEquipClassifier.IsEquipable(pickedItem)) return;
        if (equipmentData == null) return;

        EquipTag pickedTag = ItemEquipClassifier.GetEquipTag(pickedItem);
        Item equippedItem = equipmentData.GetEquippedItem(pickedTag);

        // Weapon/Armor
        if (pickedTag == EquipTag.Weapon || pickedTag == EquipTag.Armor)
        {
            if (equippedItem == null)
            {
                AutoEquipItem(pickedItem);
            }

            return;
        }

        // Utility
        if (pickedTag != EquipTag.Utility) return;

        // empty slot, equip immediately
        if (equippedItem == null)
        {
            AutoEquipItem(pickedItem);
            return;
        }

        // same utility: Stack on the utility slot
        if (equippedItem.definition == pickedItem.definition && equippedItem.IsStackable())
        {
            int totalAmount = inventory.GetTotalAmountByDefinition(pickedItem.definition);

            // clean all the selected utility in inventory
            inventory.RemoveAllByDefinition(pickedItem.definition);

            // stack them all to the utility slot
            equippedItem.amount += totalAmount;

            equipmentUI.RefreshSlot(EquipTag.Utility);
            return;
        }

        // compare priority on the list
        int equippedPriority = GetUtilityPriorityIndex(equippedItem.definition);
        int pickedPriority = GetUtilityPriorityIndex(pickedItem.definition);

        // list priority, smaller on the list, more priority
        if (pickedPriority >= 0 && (equippedPriority < 0 || pickedPriority < equippedPriority))
        {
            int originalIndex = inventory.GetItemIndex(pickedItem);

            Item itemToEquip = BuildUtilityItemToEquip(pickedItem.definition);
            if (itemToEquip == null) return;

            Item oldItem = equipmentUI.EquipItem(itemToEquip);
            SetCurrentUtilityChain(itemToEquip);

            if (oldItem != null)
            {
                inventory.InsertItemAt(oldItem, originalIndex);
            }
        }
    }

    private int GetUtilityPriorityIndex(ItemDefinition definition)
    {
        if (definition == null || utilityPriorityList == null)
            return -1;

        for (int i = 0; i < utilityPriorityList.Count; i++)
        {
            if (utilityPriorityList[i] == definition)
            {
                return i;
            }
        }

        return -1;
    }
    private void SetCurrentUtilityChain(Item item)
    {
        if (item == null || item.definition == null) return;

        if (ItemEquipClassifier.GetEquipTag(item) == EquipTag.Utility)
        {
            currentUtilityChainDefinition = item.definition;
        }
    }

    private void TryAutoEquipReplacementUtility()
    {
        // use the current utility chain
        if (TryEquipUtilityByDefinition(currentUtilityChainDefinition))
        {
            return;
        }

        // current chain is gone
        currentUtilityChainDefinition = null;

        // find the most prioritized one on the list again
        if (utilityPriorityList == null || utilityPriorityList.Count == 0)
        {
            return;
        }

        foreach (ItemDefinition def in utilityPriorityList)
        {
            if (TryEquipUtilityByDefinition(def))
            {
                return;
            }
        }
    }
    private Item BuildUtilityItemToEquip(ItemDefinition definition)
    {
        if (definition == null) return null;

        Item sampleItem = inventory.FindFirstItemByDefinition(definition);
        if (sampleItem == null) return null;

        if (!ItemEquipClassifier.IsEquipable(sampleItem)) return null;
        if (ItemEquipClassifier.GetEquipTag(sampleItem) != EquipTag.Utility) return null;

        if (sampleItem.IsStackable()) //stackable: equip all
        {
            int totalAmount = inventory.GetTotalAmountByDefinition(definition);
            if (totalAmount <= 0) return null;

            inventory.RemoveAllByDefinition(definition);

            return new Item
            {
                definition = definition,
                amount = totalAmount
            };
        }
        else //un-stackable: equip one
        {
            inventory.RemoveOneItem(sampleItem);

            return new Item
            {
                definition = definition,
                amount = 1
            };
        }
    }
    private bool TryEquipUtilityByDefinition(ItemDefinition definition)
    {
        if (definition == null) return false;

        Item itemToEquip = BuildUtilityItemToEquip(definition);
        if (itemToEquip == null) return false;

        equipmentUI.EquipItem(itemToEquip);
        SetCurrentUtilityChain(itemToEquip);
        return true;
    }

    private void AutoEquipItem(Item item)
    {
        Item itemToEquip = item.Clone();
        Item oldItem = equipmentUI.EquipItem(itemToEquip);

        if (ItemEquipClassifier.GetEquipTag(itemToEquip) == EquipTag.Utility)
        {
            SetCurrentUtilityChain(itemToEquip);
        }

        if (item.IsStackable())
        {
            inventory.RemoveItem(item);
        }
        else
        {
            inventory.RemoveOneItem(item);
        }

        if (oldItem != null)
        {
            inventory.AddItem(oldItem);
        }
    }

    private void UseEquippedUtility()
    {
        Item utilityItem = equipmentData.GetEquippedItem(EquipTag.Utility);

        if (utilityItem == null || utilityItem.definition == null) return;

        ItemDefinition def = utilityItem.definition;

        switch (def.useEffect)
        {
            case ItemUseEffect.Heal:
                FlashGreen();
                break;

            case ItemUseEffect.SpeedBoost:
                playerMovement.BoostSpeedFor10Seconds();
                FlashBlue();
                break;

            case ItemUseEffect.FlashPink:
                FlashPink();
                break;

            default:
                return;
        }

        ConsumeUtilityItem(utilityItem);
    }

    private void ConsumeUtilityItem(Item utilityItem)
    {
        utilityItem.amount--;

        if (utilityItem.amount <= 0)
        {
            equipmentUI.ClearSlot(EquipTag.Utility);
            TryAutoEquipReplacementUtility();
        }
        else
        {
            equipmentUI.RefreshSlot(EquipTag.Utility);
        }
    }

    internal void UseItem(Item item)
    {
        if (item == null || item.definition == null) return;

        // equipments
        if (ItemEquipClassifier.IsEquipable(item))
        {
            int originalIndex = inventory.GetItemIndex(item);

            Item itemToEquip = item.Clone();
            Item oldItem = equipmentUI.EquipItem(itemToEquip);
            if (ItemEquipClassifier.GetEquipTag(itemToEquip) == EquipTag.Utility)
            {
                SetCurrentUtilityChain(itemToEquip);
            }

            if (item.IsStackable())
            {
                inventory.RemoveItem(item);
            }
            else
            {
                inventory.RemoveOneItem(item);
            }

            if (oldItem != null)
            {
                inventory.InsertItemAt(oldItem, originalIndex);
            }

            return;
        }

        // not equipment -> see effect
        switch (item.definition.useEffect)
        {
            case ItemUseEffect.Heal:
                FlashGreen();
                inventory.RemoveOneItem(item);
                break;

            case ItemUseEffect.FlashPink:
                FlashPink();
                inventory.RemoveOneItem(item);
                break;
        }
    }

    public void UnequipItem(EquipTag equipTag)
    {
        Item removedItem = equipmentUI.UnequipItem(equipTag);

        if (removedItem != null)
        {
            inventory.AddItem(removedItem);
        }

        if (equipTag == EquipTag.Utility)
        {
            currentUtilityChainDefinition = null;
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        ItemWorld itemWorld = collider.GetComponent<ItemWorld>();

        if (itemWorld != null && itemWorld.CanBePickedUp())
        {
            if (!firstItemFound)
            {
                firstItemFound = true;
                playerDialogue.ShowDialogue();
            }

            Item pickedUpItem = itemWorld.GetItem();

            inventory.AddItem(pickedUpItem);
            TryAutoEquipPickedUpItem(pickedUpItem);

            itemWorld.DestroySelf();
        }
    }

    private void StartFlash(Color targetColor)
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            playerSpriteRenderer.color = baseColor;
        }

        flashCoroutine = StartCoroutine(FlashSmooth(targetColor));
    }

    private IEnumerator FlashSmooth(Color flashColor)
    {
        float flashInTime = 0.10f;
        float flashOutTime = 0.45f;

        float timer = 0f;

        while (timer < flashInTime)
        {
            timer += Time.deltaTime;
            float t = timer / flashInTime;
            t = 1f - Mathf.Pow(1f - t, 3f);
            playerSpriteRenderer.color = Color.Lerp(baseColor, flashColor, t);
            yield return null;
        }

        playerSpriteRenderer.color = flashColor;

        timer = 0f;

        while (timer < flashOutTime)
        {
            timer += Time.deltaTime;
            float t = timer / flashOutTime;
            t = t * t;
            playerSpriteRenderer.color = Color.Lerp(flashColor, baseColor, t);
            yield return null;
        }

        playerSpriteRenderer.color = baseColor;
        flashCoroutine = null;
    }

    public void FlashGreen()
    {
        StartFlash(new Color(0.4f, 1f, 0.4f));
    }

    public void FlashBlue()
    {
        StartFlash(new Color(0.4f, 0.4f, 1f));
    }

    public void FlashPink()
    {
        StartFlash(new Color(1f, 0.4f, 0.8f));
    }
}