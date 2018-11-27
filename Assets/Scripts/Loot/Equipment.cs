using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Equipment")]
public class Equipment : Items
{
    public EquipmentType equipType;

    public float armorModifier;
    public float damageModifier;

    public override void Use()
    {
        base.Use();

        EquipmentManager.instance.Equip(this);
        RemoveFromInventory();
    }

}

public enum EquipmentType { Helm, Chest, Pants, Boots, Weapon };