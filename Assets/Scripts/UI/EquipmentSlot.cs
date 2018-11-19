using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentSlot : InventorySlot {

    Equipment equipment;

    private void Awake()
    {
        equipment = (Equipment)item;
    }

    new public void AddItem(Items newItem)
    {
        item = newItem;

        icon.sprite = item.icon;
        icon.enabled = true;
        //removeButton.interactable = true;
    }

    public override void OnRemoveButton()
    {
        int itemSlot = (int)equipment.equipType;
        EquipmentManager.instance.Unequip(itemSlot);
        InventoryManager.instance.Add(equipment);
    }

}
