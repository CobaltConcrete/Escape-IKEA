using System.Collections.Generic;
using System;
using UnityEngine;

public class Inventory
{
    public event EventHandler OnItemListChanged;
    public event EventHandler OnLootListChanged;

    private List<Item> itemList;
    private List<Item> lootList;

    private Action<Item> useItemAction;

    public Inventory(Action<Item> useItemAction)
    {
        this.useItemAction = useItemAction;
        itemList = new List<Item>();
        lootList = new List<Item>();
    }
    public int GetTotalAmountByDefinition(ItemDefinition definition)
    {
        int total = 0;

        foreach (Item item in itemList)
        {
            if (item != null && item.definition == definition)
            {
                total += item.amount;
            }
        }

        return total;
    }
    public void RemoveAllByDefinition(ItemDefinition definition)
    {
        if (definition == null) return;

        for (int i = itemList.Count - 1; i >= 0; i--)
        {
            if (itemList[i] != null && itemList[i].definition == definition)
            {
                itemList.RemoveAt(i);
            }
        }

        OnItemListChanged?.Invoke(this, EventArgs.Empty);
    }

    public Item FindFirstItemByDefinition(ItemDefinition definition)
    {
        foreach (Item item in itemList)
        {
            if (item.definition == definition)
            {
                return item;
            }
        }

        return null;
    }

    public void AddItem(Item item)
    {
        if (item == null || item.definition == null) return;

        if (item.IsStackable())
        {
            bool itemAlreadyInInventory = false;

            foreach (Item inventoryItem in itemList)
            {
                if (inventoryItem.definition == item.definition)
                {
                    inventoryItem.amount += item.amount;
                    itemAlreadyInInventory = true;
                    break;
                }
            }

            if (!itemAlreadyInInventory)
            {
                itemList.Add(item);
            }
        }
        else
        {
            itemList.Add(item);
        }

        OnItemListChanged?.Invoke(this, EventArgs.Empty);
    }
    public void AddLoot(Item item)
    {
        if (item == null || item.definition == null) return;

        if (item.IsStackable())
        {
            bool itemAlreadyInLoot = false;

            foreach (Item lootItem in lootList)
            {
                if (lootItem.definition == item.definition)
                {
                    lootItem.amount += item.amount;
                    itemAlreadyInLoot = true;
                    break;
                }
            }

            if (!itemAlreadyInLoot)
            {
                lootList.Add(item);
            }
        }
        else
        {
            lootList.Add(item);
        }

        OnLootListChanged?.Invoke(this, EventArgs.Empty);
    }

    public int GetItemIndex(Item item)
    {
        return itemList.IndexOf(item);
    }
    public List<Item> GetLootList()
    {
        return lootList;
    }

    public void InsertItemAt(Item item, int index)
    {
        if (item == null || item.definition == null) return;

        if (item.IsStackable())
        {
            bool itemAlreadyInInventory = false;

            foreach (Item inventoryItem in itemList)
            {
                if (inventoryItem.definition == item.definition)
                {
                    inventoryItem.amount += item.amount;
                    itemAlreadyInInventory = true;
                    break;
                }
            }

            if (!itemAlreadyInInventory)
            {
                index = Mathf.Clamp(index, 0, itemList.Count);
                itemList.Insert(index, item);
            }
        }
        else
        {
            index = Mathf.Clamp(index, 0, itemList.Count);
            itemList.Insert(index, item);
        }

        OnItemListChanged?.Invoke(this, EventArgs.Empty);
    }

    public List<Item> GetItemList()
    {
        return itemList;
    }

    public void UseItem(Item item)
    {
        useItemAction(item);
    }

    public void RemoveItem(Item item)
    {
        if (item == null || item.definition == null) return;

        if (item.IsStackable())
        {
            Item itemInInventory = null;

            foreach (Item inventoryItem in itemList)
            {
                if (inventoryItem.definition == item.definition)
                {
                    inventoryItem.amount -= item.amount;
                    itemInInventory = inventoryItem;
                    break;
                }
            }

            if (itemInInventory != null && itemInInventory.amount <= 0)
            {
                itemList.Remove(itemInInventory);
            }
        }
        else
        {
            itemList.Remove(item);
        }

        OnItemListChanged?.Invoke(this, EventArgs.Empty);
    }
    public void RemoveLoot(Item item)
    {
        if (item == null || item.definition == null) return;

        if (item.IsStackable())
        {
            item.amount--;

            if (item.amount <= 0)
            {
                lootList.Remove(item);
            }
        }
        else
        {
            lootList.Remove(item);
        }

        OnLootListChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RemoveOneItem(Item item)
    {
        if (item == null) return;

        if (item.IsStackable())
        {
            item.amount--;

            if (item.amount <= 0)
            {
                itemList.Remove(item);
            }
        }
        else
        {
            itemList.Remove(item);
        }

        OnItemListChanged?.Invoke(this, EventArgs.Empty);
    }
}