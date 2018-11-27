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

    //public EquipmentSlot[] currentSlots;

    public Equipment[] currentEquipment;

    private PlayerStats stats;

    private InventoryManager inventory;

    public delegate void OnEquipmentChanged(Equipment newItem, Equipment oldItem);
    public OnEquipmentChanged onEquipmentChanged;

    private void Start()
    {
        int numSlots = System.Enum.GetNames(typeof(EquipmentType)).Length;
        currentEquipment = new Equipment[numSlots];
        inventory = InventoryManager.instance;
        stats = PlayerStats.Instance;
    }

    public void Equip(Equipment newItem)
    {
        int slotIndex = (int)newItem.equipType;

        Equipment oldItem = null;

        if(currentEquipment[slotIndex] != null)
        {
            oldItem = currentEquipment[slotIndex];
            inventory.Add(oldItem);
        }

        if (onEquipmentChanged != null)
            onEquipmentChanged.Invoke(newItem, oldItem);

        currentEquipment[slotIndex] = newItem;
        PassEquipStats();
    }

    public void Unequip(int slotIndex)
    {
        if(currentEquipment[slotIndex] != null)
        {
            Equipment oldItem = currentEquipment[slotIndex];
            inventory.Add(oldItem);

            if (onEquipmentChanged != null)
                onEquipmentChanged.Invoke(null, oldItem);

            currentEquipment[slotIndex] = null;
            PassEquipStats();
        }
    }

    public void UnequipAll()
    {
        for (int i = 0; i < currentEquipment.Length; i++)
        {
            Unequip(i);
        }
        PassEquipStats();
    }

    public void PassEquipStats()
    {
        float totalArmor = 0;
        float totalDamage = 0;

        for (int i = 0; i < currentEquipment.Length; i++)
        {
            if(currentEquipment[i] != null)
            {
                totalArmor += currentEquipment[i].armorModifier;
                totalDamage += currentEquipment[i].damageModifier;
            }
        }

        stats.SetArmorModifier(totalArmor);
        stats.SetDamageModifier(totalDamage);
    }
}
