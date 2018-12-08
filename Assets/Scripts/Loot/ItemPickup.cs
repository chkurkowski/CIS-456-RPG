using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPickup : MonoBehaviour {

    public Items item;

    public Equipment equipment;

    public float radius = 2f;
    public bool focused = false;

    private Transform player;

    private bool hasInteracted = false;

    private void Awake()
    {
        player = GameObject.FindWithTag("Player").GetComponent<Transform>();
    }

    private void Update()
    {
        if(focused)
        {
            float distance = Vector3.Distance(player.position, this.transform.position);
            if(!hasInteracted && distance <= radius)
            {
                hasInteracted = true;
                Interact();
            }
        }
    }

    public void Interact()
    {
        PickUp();
    }

    private void PickUp()
    {
        bool pickedUp = false;
        //Debug.Log("Picking up " + item.name);

        if(item != null)
        {
            pickedUp = InventoryManager.instance.Add(item);
        }
        else if(equipment != null)
        {
            pickedUp = InventoryManager.instance.Add(equipment);
        }
        if (pickedUp)
            Destroy(gameObject);
    }

}
