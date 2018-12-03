﻿using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour {

    #region Singleton
    public static InventoryManager instance;

    private void Awake()
    {
        if(instance != null)
        {
            Debug.LogWarning("More than one instance of Inventory Manager exists");
        }
        instance = this;
    }
    #endregion

    public float inventorySize;
    public Canvas inventory;
    public InventoryItems currItems = InventoryItems.Instance;

    public delegate void OnItemChanged();
    public OnItemChanged onItemChangedCallback;

    public bool Add(Items item)
    {
        if(currItems.items.Count >= inventorySize)
        {
            return false;
        }
        currItems.items.Add(item);

        if (onItemChangedCallback != null)
            onItemChangedCallback.Invoke();

        return true;
    }

    public void Remove(Items item)
    {
        currItems.items.Remove(item);

        if (onItemChangedCallback != null)
            onItemChangedCallback.Invoke();
    }

    public void RemoveAll()
    {
        currItems.items.Clear();
    }
}
