using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentUI : MonoBehaviour {

    public Transform currentItems;

    private EquipmentManager equipment;

    private EquipmentSlot[] slots;

    private void Start()
    {
        equipment = EquipmentManager.instance;
        equipment.onEquipmentChanged += UpdateEquipUI;

        slots = currentItems.GetComponentsInChildren<EquipmentSlot>();
    }

    private void UpdateEquipUI()
    {
        Debug.Log("Updating Equipment");

        //TODO reod this loop to add only to the relevant section instead of looping through the whole thing. Maybe hard code it?

        //if (newItem == null && oldItem == null)
        //{
            for(int i = 0; i < slots.Length; i++)
            {
                if(EquippedItems.Instance.currentEquipment[i] != null)
                {
                    slots[i].AddItem(EquippedItems.Instance.currentEquipment[i]);
                }
                else
                {
                    slots[i].ClearSlot();
                }
            }
        //}

        //if (newItem != null)
        //{
        //    if (newItem.equipType.ToString() == "Helm")
        //    {
        //        slots[0].AddItem(newItem);
        //    }
        //    else if (newItem.equipType.ToString() == "Chest")
        //    {
        //        slots[1].AddItem(newItem);
        //    }
        //    else if (newItem.equipType.ToString() == "Pants")
        //    {
        //        slots[2].AddItem(newItem);
        //    }
        //    else if (newItem.equipType.ToString() == "Boots")
        //    {
        //        slots[3].AddItem(newItem);
        //    }
        //    else if (newItem.equipType.ToString() == "Weapon")
        //    {
        //        slots[4].AddItem(newItem);
        //    }
        //}
        //else 
        //{
        //    if (oldItem.equipType.ToString() == "Helm")
        //    {
        //        slots[0].ClearSlot();
        //    }
        //    else if (oldItem.equipType.ToString() == "Chest")
        //    {
        //        slots[1].ClearSlot();
        //    }
        //    else if (oldItem.equipType.ToString() == "Pants")
        //    {
        //        slots[2].ClearSlot();
        //    }
        //    else if (oldItem.equipType.ToString() == "Boots")
        //    {
        //        slots[3].ClearSlot();
        //    }
        //    else if (oldItem.equipType.ToString() == "Weapon")
        //    {
        //        slots[4].ClearSlot();
        //    }
        //}
    }
}
