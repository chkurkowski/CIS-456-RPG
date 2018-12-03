using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{

    //TODO Create a singleton that holds the currentEquipment array so that it is not reset when you go to the next level.

    #region Singleton

    public static EquipmentManager instance;

    private void Awake()
    {
        instance = this;
    }

    #endregion

    private EquippedItems equipment = EquippedItems.Instance;

    private PlayerStats stats;

    private InventoryManager inventory;

    public delegate void OnEquipmentChanged();
    public OnEquipmentChanged onEquipmentChanged;

    private void Start()
    {
        int numSlots = System.Enum.GetNames(typeof(EquipmentType)).Length;
        equipment.GenerateArray(numSlots);
        inventory = InventoryManager.instance;
        stats = PlayerStats.Instance;
        //stats.GetSavedEquipment();
    }

    public void Equip(Equipment newItem)
    {
        int slotIndex = (int)newItem.equipType;

        Equipment oldItem = null;

        if(equipment.currentEquipment[slotIndex] != null)
        {
            oldItem = equipment.currentEquipment[slotIndex];
            inventory.Add(oldItem);
        }

        equipment.currentEquipment[slotIndex] = newItem;

        if (onEquipmentChanged != null)
            onEquipmentChanged.Invoke();

        PassEquipStats();
    }

    public void Unequip(int slotIndex)
    {
        if(equipment.currentEquipment[slotIndex] != null)
        {
            Equipment oldItem = equipment.currentEquipment[slotIndex];
            inventory.Add(oldItem);

            equipment.currentEquipment[slotIndex] = null;

            if (onEquipmentChanged != null)
                onEquipmentChanged.Invoke();

            PassEquipStats();
        }
    }

    public void UnequipAll()
    {
        for (int i = 0; i < equipment.currentEquipment.Length; i++)
        {
            Unequip(i);
        }
        PassEquipStats();
    }

    public void PassEquipStats()
    {
        float totalArmor = 0;
        float totalDamage = 0;

        for (int i = 0; i < equipment.currentEquipment.Length; i++)
        {
            if(equipment.currentEquipment[i] != null)
            {
                totalArmor += equipment.currentEquipment[i].armorModifier;
                totalDamage += equipment.currentEquipment[i].damageModifier;
            }
        }

        stats.SetArmorModifier(totalArmor);
        stats.SetDamageModifier(totalDamage);
    }
}
