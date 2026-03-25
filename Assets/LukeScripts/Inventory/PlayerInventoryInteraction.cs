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

    [Header("Auto Equip Settings")]
    [SerializeField] private bool autoEquipWhenSlotEmpty = false;
    [SerializeField] private bool autoRefillUtilityWhenConsumed = false;

    private SpriteRenderer playerSpriteRenderer;
    private PlayerMovement playerMovement;
    private ItemDefinition currentUtilityChainDefinition;

    private Coroutine flashCoroutine;
    private Color baseColor;

    private bool firstItemFound;

    [Header("Weapon Priority")]
    [SerializeField] private List<ItemDefinition> weaponPriorityList;

    [Header("Armor Priority")]
    [SerializeField] private List<ItemDefinition> armorPriorityList;

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

        // Weapon / Armor£ºauto equip if empty
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

        // empty: equip immediately
        if (equippedItem == null)
        {
            AutoEquipItem(pickedItem);
            return;
        }

        // same utility: stack on slot
        if (equippedItem.definition == pickedItem.definition && equippedItem.IsStackable())
        {
            int totalAmount = inventory.GetTotalAmountByDefinition(pickedItem.definition);

            inventory.RemoveAllByDefinition(pickedItem.definition);
            equippedItem.amount += totalAmount;

            equipmentUI.RefreshSlot(EquipTag.Utility);
            return;
        }

        // when bool true, replace with top priority
        int equippedPriority = GetPriorityIndex(equippedItem.definition, utilityPriorityList);
        int pickedPriority = GetPriorityIndex(pickedItem.definition, utilityPriorityList);

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

    private int GetPriorityIndex(ItemDefinition definition, List<ItemDefinition> priorityList)
    {
        if (definition == null || priorityList == null)
            return -1;

        for (int i = 0; i < priorityList.Count; i++)
        {
            if (priorityList[i] == definition)
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

    private void TryAutoEquipAfterEmpty(EquipTag equipTag)
    {
        if (!autoEquipWhenSlotEmpty) return;

        if (equipmentData.GetEquippedItem(equipTag) != null)
            return;

        switch (equipTag)
        {
            case EquipTag.Weapon:
                TryAutoEquipBestSimple(EquipTag.Weapon, weaponPriorityList);
                return;

            case EquipTag.Armor:
                TryAutoEquipBestSimple(EquipTag.Armor, armorPriorityList);
                return;

            case EquipTag.Utility:
                TryAutoEquipReplacementUtility();
                return;
        }
    }

    private void TryAutoEquipBestSimple(EquipTag equipTag, List<ItemDefinition> priorityList)
    {
        if (priorityList == null || priorityList.Count == 0)
            return;

        if (equipmentData.GetEquippedItem(equipTag) != null)
            return;

        foreach (ItemDefinition def in priorityList)
        {
            Item item = inventory.FindFirstItemByDefinition(def);
            if (item == null) continue;
            if (!ItemEquipClassifier.IsEquipable(item)) continue;
            if (ItemEquipClassifier.GetEquipTag(item) != equipTag) continue;

            AutoEquipItem(item);
            return;
        }
    }

    private void TryAutoEquipReplacementUtility()
    {
        // try to preserve current utility chain
        if (TryEquipUtilityByDefinition(currentUtilityChainDefinition))
        {
            return;
        }

        currentUtilityChainDefinition = null;

        // find best based on list
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
    //private void TryAutoEquipNextInPriority(EquipTag equipTag, List<ItemDefinition> priorityList, ItemDefinition currentDefinition)
    //{
    //    if (!autoEquipWhenSlotEmpty) return;
    //    if (priorityList == null || priorityList.Count == 0) return;
    //    if (equipmentData.GetEquippedItem(equipTag) != null) return;

    //    int startIndex = -1;

    //    if (currentDefinition != null)
    //    {
    //        startIndex = GetPriorityIndex(currentDefinition, priorityList);
    //    }

    //    int count = priorityList.Count;

    //    for (int step = 1; step <= count; step++)
    //    {
    //        int index = (startIndex + step) % count;
    //        ItemDefinition def = priorityList[index];

    //        Item item = inventory.FindFirstItemByDefinition(def);
    //        if (item == null) continue;
    //        if (!ItemEquipClassifier.IsEquipable(item)) continue;
    //        if (ItemEquipClassifier.GetEquipTag(item) != equipTag) continue;

    //        if (equipTag == EquipTag.Utility)
    //        {
    //            Item itemToEquip = BuildUtilityItemToEquip(def);
    //            if (itemToEquip == null) return;

    //            equipmentUI.EquipItem(itemToEquip);
    //            SetCurrentUtilityChain(itemToEquip);
    //        }
    //        else
    //        {
    //            AutoEquipItem(item);
    //        }

    //        return;
    //    }
    //}

    private Item BuildUtilityItemToEquip(ItemDefinition definition)
    {
        if (definition == null) return null;

        Item sampleItem = inventory.FindFirstItemByDefinition(definition);
        if (sampleItem == null) return null;

        if (!ItemEquipClassifier.IsEquipable(sampleItem)) return null;
        if (ItemEquipClassifier.GetEquipTag(sampleItem) != EquipTag.Utility) return null;

        if (sampleItem.IsStackable())
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
        else
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
    private bool TryEquipByDefinition(EquipTag equipTag, ItemDefinition definition)
    {
        if (definition == null) return false;

        if (equipTag == EquipTag.Utility)
        {
            return TryEquipUtilityByDefinition(definition);
        }

        Item item = inventory.FindFirstItemByDefinition(definition);
        if (item == null) return false;
        if (!ItemEquipClassifier.IsEquipable(item)) return false;
        if (ItemEquipClassifier.GetEquipTag(item) != equipTag) return false;

        AutoEquipItem(item);
        return true;
    }

    private void TryAutoEquipWorseAfterDrop(
    EquipTag equipTag,
    List<ItemDefinition> priorityList,
    ItemDefinition droppedDefinition)
    {
        if (!autoEquipWhenSlotEmpty) return;
        if (priorityList == null || priorityList.Count == 0) return;
        if (equipmentData.GetEquippedItem(equipTag) != null) return;

        int startIndex = GetPriorityIndex(droppedDefinition, priorityList);

        // not on list, find best
        if (startIndex < 0)
        {
            for (int i = 0; i < priorityList.Count; i++)
            {
                if (TryEquipByDefinition(equipTag, priorityList[i]))
                {
                    return;
                }
            }
            return;
        }

        // look for worse
        for (int i = startIndex + 1; i < priorityList.Count; i++)
        {
            if (TryEquipByDefinition(equipTag, priorityList[i]))
            {
                return;
            }
        }

        // if already the worst, go back to find best
        for (int i = 0; i < startIndex; i++)
        {
            if (TryEquipByDefinition(equipTag, priorityList[i]))
            {
                return;
            }
        }

        // if there really are nothing left, leave empty
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

            if (autoRefillUtilityWhenConsumed)
            {
                TryAutoEquipReplacementUtility();
            }
        }
        else
        {
            equipmentUI.RefreshSlot(EquipTag.Utility);
        }
    }

    internal void UseItem(Item item)
    {
        if (item == null || item.definition == null) return;

        // Equipments
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

        // Non-equip items
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
        if (removedItem == null) return;

        inventory.AddItem(removedItem);

        ItemDefinition removedDefinition = removedItem.definition;

        if (equipTag == EquipTag.Utility)
        {
            currentUtilityChainDefinition = null;
        }

        List<ItemDefinition> priorityList = GetPriorityListByTag(equipTag);
        TryAutoEquipWorseAfterDrop(equipTag, priorityList, removedDefinition);
    }

    //public void DropEquippedItem(EquipTag equipTag)
    //{
    //    if (equipmentData == null || equipmentUI == null)
    //        return;

    //    Item equippedItem = equipmentData.GetEquippedItem(equipTag);
    //    if (equippedItem == null || equippedItem.definition == null)
    //        return;

    //    Item droppedItem = new Item
    //    {
    //        definition = equippedItem.definition,
    //        amount = equippedItem.amount
    //    };

    //    equipmentData.ClearSlot(equipTag);

    //    if (equipTag == EquipTag.Utility)
    //    {
    //        currentUtilityChainDefinition = null;
    //    }

    //    equipmentUI.RefreshAllSlots();
    //    ItemWorld.DropItem(GetPosition(), droppedItem);

    //    TryAutoEquipAfterEmpty(equipTag);
    //}
    public void DropEquippedItem(EquipTag equipTag)
    {
        if (equipmentData == null || equipmentUI == null)
            return;

        Item equippedItem = equipmentData.GetEquippedItem(equipTag);
        if (equippedItem == null || equippedItem.definition == null)
            return;

        Item droppedItem = new Item
        {
            definition = equippedItem.definition,
            amount = equippedItem.amount
        };

        equipmentData.ClearSlot(equipTag);

        ItemDefinition droppedDefinition = equippedItem.definition;

        if (equipTag == EquipTag.Utility)
        {
            currentUtilityChainDefinition = null;
        }

        equipmentUI.RefreshAllSlots();
        ItemWorld.DropItem(GetPosition(), droppedItem);

        List<ItemDefinition> priorityList = GetPriorityListByTag(equipTag);
        TryAutoEquipWorseAfterDrop(equipTag, priorityList, droppedDefinition);
    }
    private List<ItemDefinition> GetPriorityListByTag(EquipTag equipTag)
    {
        switch (equipTag)
        {
            case EquipTag.Weapon:
                return weaponPriorityList;
            case EquipTag.Armor:
                return armorPriorityList;
            case EquipTag.Utility:
                return utilityPriorityList;
            default:
                return null;
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