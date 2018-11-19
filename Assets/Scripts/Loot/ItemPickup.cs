using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPickup : MonoBehaviour {

    public Items item;

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
        Debug.Log("Picking up " + item.name);
        bool pickedUp = InventoryManager.instance.Add(item);
        if (pickedUp)
            Destroy(gameObject);
    }

}
