using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralLoot {

    #region Singleton

    private static ProceduralLoot inst;

    private ProceduralLoot(){}

    public static ProceduralLoot Instance
    {
        get
        {
            if (inst == null)
            {
                inst = new ProceduralLoot();
            }
            return inst;
        }
    }

    #endregion

    private float baseHelmArmor = 10f;
    private float baseChestArmor = 20f;
    private float basePantsArmor = 15f;
    private float baseBootsArmor = 5f;

    private Equipment spawnedEquipment;
    private GameManager manager = GameManager.instance;

    public void RandomizeLoot(GameObject equipment)
    {
        spawnedEquipment = equipment.GetComponent<ItemPickup>().equipment;

        switch(spawnedEquipment.equipType)
        {
            case EquipmentType.Helm:
                RandomizeHelm();
                break;
            case EquipmentType.Chest:
                RandomizeChest();
                break;
            case EquipmentType.Pants:
                RandomizePants();
                break;
            case EquipmentType.Boots:
                RandomizeBoots();
                break;
        }
    }

    private void RandomizeHelm()
    {
        spawnedEquipment.armorModifier = GetRange(baseHelmArmor);
        spawnedEquipment.name = "Helm" + RandomizeName();
    }

    private void RandomizeChest()
    {
        spawnedEquipment.armorModifier = GetRange(baseChestArmor);
        spawnedEquipment.name = "Chest" + RandomizeName();
    }

    private void RandomizePants()
    {
        spawnedEquipment.armorModifier = GetRange(basePantsArmor);
        spawnedEquipment.name = "Pants" + RandomizeName();
    }

    private void RandomizeBoots()
    {
        spawnedEquipment.armorModifier = GetRange(baseBootsArmor);
        spawnedEquipment.name = "Boots" + RandomizeName();
    }

    private float GetRange(float baseValue)
    {
        int currentLevel = manager.getLevel();
        return (Random.Range(baseValue - (baseValue / 2f), baseValue + (baseValue / 2f)) * currentLevel);
    }

    private string RandomizeName()
    {
        int temp = Random.Range(1, 11);
        string name = "";

        switch(temp)
        {
            case 1:
                name = " of Agility";
                break;
            case 2:
                name = " of Strength";
                break;
            case 3:
                name = " of Intelligence";
                break;
            case 4:
                name = " of Power";
                break;
            case 5:
                name = " of Cleansing";
                break;
            case 6:
                name = " of Fire";
                break;
            case 7:
                name = " of Water";
                break;
            case 8:
                name = " of Earth";
                break;
            case 9:
                name = " of Air";
                break;
            case 10:
                name = "";
                break;

        }
        return name;
    }
}
