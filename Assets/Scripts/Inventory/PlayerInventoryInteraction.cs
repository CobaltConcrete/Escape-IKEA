using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static EquipmentEnum;

public class PlayerInventoryInteraction : MonoBehaviour
{
    private Inventory inventory;

    [SerializeField] private UI_Inventory uiInventory;
    [SerializeField] private UI_LootInventory uiLootInventory;
    [SerializeField] private EquipmentUI equipmentUI;
    [SerializeField] private EquipmentData equipmentData;
    [SerializeField] private Dialogue playerDialogue;

    private ItemWorld nearbyLoot;
    [SerializeField] private float interactRadius = 0.45f;
    [SerializeField] private LayerMask interactableLayerMask;
    [SerializeField] private UI_InteractionPrompt interactionPromptUI;

    private IInteractable currentInteractable;

    [Header("Auto Equip Settings")]
    [SerializeField] private bool autoEquipWhenSlotEmpty = false;
    [SerializeField] private bool autoRefillUtilityWhenConsumed = false;
    [SerializeField] private bool autoReplaceWithHigherPriority = false;

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
        if (uiInventory != null)
        {
            uiInventory.SetPlayer(this);
            uiInventory.SetInventory(inventory);
        }

        if (uiLootInventory != null)
        {
            uiLootInventory.SetPlayer(this);
            uiLootInventory.SetInventory(inventory);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            UseEquippedUtility();
        }

        FindBestInteractable();

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (currentInteractable != null)
            {
                currentInteractable.Interact(this);
            }
        }
    }
    public Vector3 GetPosition()
    {
        return transform.position;
    }
    public void PickupLoot(ItemWorld itemWorld)
    {
        if (itemWorld == null) return;
        Item pickedUpItem = itemWorld.GetItem();
        if (pickedUpItem == null || pickedUpItem.definition == null) return;
        if (!pickedUpItem.IsLoot()) return;

        inventory.AddLoot(pickedUpItem);

        itemWorld.DestroySelf();
    }
    private void FindBestInteractable()
    {
        currentInteractable = null;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            interactRadius,
            interactableLayerMask
        );

        float bestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            IInteractable interactable = hit.GetComponent<IInteractable>();
            if (interactable == null) continue;

            float distance = Vector2.Distance(transform.position, hit.transform.position);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                currentInteractable = interactable;
            }
        }

        UpdateInteractionPrompt();
    }

    private void UpdateInteractionPrompt()
    {
        if (interactionPromptUI == null) return;

        if (currentInteractable == null)
        {
            interactionPromptUI.Hide();
            return;
        }

        string text = currentInteractable.GetInteractionText();

        if (string.IsNullOrEmpty(text))
        {
            interactionPromptUI.Hide();
        }
        else
        {
            interactionPromptUI.Show(text);
        }
    }
    private void TryAutoEquipPickedUpItem(Item pickedItem)
    {
        if (pickedItem == null || pickedItem.definition == null) return;
        if (!ItemEquipClassifier.IsEquipable(pickedItem)) return;
        if (equipmentData == null) return;

        EquipTag pickedTag = ItemEquipClassifier.GetEquipTag(pickedItem);
        Item equippedItem = equipmentData.GetEquippedItem(pickedTag);

        List<ItemDefinition> priorityList = GetPriorityListByTag(pickedTag);

        // =========================
        // Case 1: slot empty
        // only controlled by autoEquipWhenSlotEmpty
        // =========================
        if (equippedItem == null)
        {
            if (autoEquipWhenSlotEmpty)
            {
                AutoEquipItem(pickedItem);
            }
            return;
        }

        // =========================
        // Case 2: slot not empty
        // Utility special case: same item stack together
        // =========================
        if (pickedTag == EquipTag.Utility)
        {
            if (equippedItem.definition == pickedItem.definition && equippedItem.IsStackable())
            {
                int totalAmount = inventory.GetTotalAmountByDefinition(pickedItem.definition);

                inventory.RemoveAllByDefinition(pickedItem.definition);
                equippedItem.amount += totalAmount;

                equipmentUI.RefreshSlot(EquipTag.Utility);
                return;
            }
        }

        // =========================
        // Case 3: slot not empty, check higher priority replacement
        // only controlled by autoReplaceWithHigherPriority
        // =========================
        if (!autoReplaceWithHigherPriority)
        {
            return;
        }

        if (!IsHigherPriority(pickedItem.definition, equippedItem.definition, priorityList))
        {
            return;
        }

        int originalIndex = inventory.GetItemIndex(pickedItem);

        // Utility needs special build logic
        if (pickedTag == EquipTag.Utility)
        {
            Item itemToEquip = BuildUtilityItemToEquip(pickedItem.definition);
            if (itemToEquip == null) return;

            Item oldItem = equipmentUI.EquipItem(itemToEquip);
            SetCurrentUtilityChain(itemToEquip);

            if (oldItem != null)
            {
                inventory.InsertItemAt(oldItem, originalIndex);
            }

            return;
        }

        // Weapon / Armor replacement
        Item itemClone = pickedItem.Clone();
        Item oldEquippedItem = equipmentUI.EquipItem(itemClone);

        if (pickedItem.IsStackable())
        {
            inventory.RemoveItem(pickedItem);
        }
        else
        {
            inventory.RemoveOneItem(pickedItem);
        }

        if (oldEquippedItem != null)
        {
            inventory.InsertItemAt(oldEquippedItem, originalIndex);
        }
    }

    //below is the AutoEquip Version where autoReplaceWithHigherPriority only influences WEAPON/ARMOR, not UTILITY

    //private void TryAutoEquipPickedUpItem(Item pickedItem)
    //{
    //    if (pickedItem == null || pickedItem.definition == null) return;
    //    if (!ItemEquipClassifier.IsEquipable(pickedItem)) return;
    //    if (equipmentData == null) return;

    //    EquipTag pickedTag = ItemEquipClassifier.GetEquipTag(pickedItem);
    //    Item equippedItem = equipmentData.GetEquippedItem(pickedTag);
    //    List<ItemDefinition> priorityList = GetPriorityListByTag(pickedTag);

    //    // =========================
    //    // Weapon / Armor
    //    // =========================
    //    if (pickedTag == EquipTag.Weapon || pickedTag == EquipTag.Armor)
    //    {
    //        // slot empty -> controlled by autoEquipWhenSlotEmpty
    //        if (equippedItem == null)
    //        {
    //            if (autoEquipWhenSlotEmpty)
    //            {
    //                AutoEquipItem(pickedItem);
    //            }
    //            return;
    //        }

    //        // slot not empty -> controlled by autoReplaceWithHigherPriority
    //        if (!autoReplaceWithHigherPriority)
    //        {
    //            return;
    //        }

    //        if (!IsHigherPriority(pickedItem.definition, equippedItem.definition, priorityList))
    //        {
    //            return;
    //        }

    //        int originalIndex = inventory.GetItemIndex(pickedItem);

    //        Item itemToEquip = pickedItem.Clone();
    //        Item oldItem = equipmentUI.EquipItem(itemToEquip);

    //        if (pickedItem.IsStackable())
    //        {
    //            inventory.RemoveItem(pickedItem);
    //        }
    //        else
    //        {
    //            inventory.RemoveOneItem(pickedItem);
    //        }

    //        if (oldItem != null)
    //        {
    //            inventory.InsertItemAt(oldItem, originalIndex);
    //        }

    //        return;
    //    }

    //    // =========================
    //    // Utility
    //    // =========================
    //    if (pickedTag != EquipTag.Utility) return;

    //    // slot empty -> controlled by autoEquipWhenSlotEmpty
    //    if (equippedItem == null)
    //    {
    //        if (autoEquipWhenSlotEmpty)
    //        {
    //            AutoEquipItem(pickedItem);
    //        }
    //        return;
    //    }

    //    // same utility -> always stack
    //    if (equippedItem.definition == pickedItem.definition && equippedItem.IsStackable())
    //    {
    //        int totalAmount = inventory.GetTotalAmountByDefinition(pickedItem.definition);

    //        inventory.RemoveAllByDefinition(pickedItem.definition);
    //        equippedItem.amount += totalAmount;

    //        equipmentUI.RefreshSlot(EquipTag.Utility);
    //        return;
    //    }

    //    // utility replacement -> ALWAYS compare priority
    //    if (!IsHigherPriority(pickedItem.definition, equippedItem.definition, utilityPriorityList))
    //    {
    //        return;
    //    }

    //    int utilityOriginalIndex = inventory.GetItemIndex(pickedItem);

    //    Item utilityToEquip = BuildUtilityItemToEquip(pickedItem.definition);
    //    if (utilityToEquip == null) return;

    //    Item oldUtility = equipmentUI.EquipItem(utilityToEquip);
    //    SetCurrentUtilityChain(utilityToEquip);

    //    if (oldUtility != null)
    //    {
    //        inventory.InsertItemAt(oldUtility, utilityOriginalIndex);
    //    }
    //}

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

    private bool IsHigherPriority(ItemDefinition pickedDefinition, ItemDefinition equippedDefinition, List<ItemDefinition> priorityList)
    {
        int pickedPriority = GetPriorityIndex(pickedDefinition, priorityList);
        int equippedPriority = GetPriorityIndex(equippedDefinition, priorityList);

        if (pickedPriority < 0)
        {
            return false;
        }

        if (equippedPriority < 0)
        {
            return true;
        }

        return pickedPriority < equippedPriority;
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
                amount = totalAmount,
                worldScale = sampleItem.worldScale
            };
        }
        else
        {
            inventory.RemoveOneItem(sampleItem);

            return new Item
            {
                definition = definition,
                amount = 1,
                worldScale = sampleItem.worldScale
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

        // 关键：保留完整数据，尤其是 worldScale
        Item droppedItem = equippedItem.Clone();

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

    //private void OnTriggerEnter2D(Collider2D collider)
    //{
    //    ItemWorld itemWorld = collider.GetComponent<ItemWorld>();

    //    if (itemWorld != null && itemWorld.CanBePickedUp())
    //    {
    //        Item pickedUpItem = itemWorld.GetItem();
    //        if (pickedUpItem == null || pickedUpItem.definition == null) return;

    //        if (!firstItemFound)
    //        {
    //            firstItemFound = true;
    //            playerDialogue.ShowDialogue();
    //        }

    //        if (pickedUpItem.IsLoot())
    //        {
    //            // Loot don't auto pick up, just record that there is a loot nearby
    //            nearbyLoot = itemWorld;
    //        }
    //        else
    //        {
    //            // normal item pick up immediately
    //            inventory.AddItem(pickedUpItem);
    //            TryAutoEquipPickedUpItem(pickedUpItem);
    //            itemWorld.DestroySelf();
    //        }
    //    }
    //}
    private void OnTriggerEnter2D(Collider2D collider)
    {
        ItemWorld itemWorld = collider.GetComponent<ItemWorld>();

        if (itemWorld == null || !itemWorld.CanBePickedUp())
        {
            return;
        }

        Item pickedUpItem = itemWorld.GetItem();

        if (pickedUpItem == null || pickedUpItem.definition == null)
        {
            return;
        }

        // Loot don't auto pick up, leave it to F interact
        if (pickedUpItem.IsLoot())
        {
            return;
        }

        // normal item pick up immediately
        if (!firstItemFound)
        {
            firstItemFound = true;
            playerDialogue.ShowDialogue();
        }

        inventory.AddItem(pickedUpItem);
        TryAutoEquipPickedUpItem(pickedUpItem);

        itemWorld.DestroySelf();
    }
    private void OnTriggerExit2D(Collider2D collider)
    {
        ItemWorld itemWorld = collider.GetComponent<ItemWorld>();

        if (itemWorld != null && itemWorld == nearbyLoot)
        {
            nearbyLoot = null;
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