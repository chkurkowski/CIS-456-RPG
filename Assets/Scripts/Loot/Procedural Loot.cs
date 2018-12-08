using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralLoot : MonoBehaviour {

    #region Singleton

    public static ProceduralLoot instance;

    private void Awake()
    {
        instance = this;
    }

    #endregion

    private Items spawnedEquipment;
    private GameManager manager = GameManager.instance;

    public void RandomizeLoot(GameObject equipment)
    {
        spawnedEquipment = equipment.GetComponent<ItemPickup>().item;
    }
}
