using UnityEngine;

public class InventoryUi : MonoBehaviour {

    public Transform backPanel;

    public GameObject inventoryUI;

    private InventoryManager inventory;

    private InventorySlot[] slots;

	// Use this for initialization
	void Start () {
        inventory = InventoryManager.instance;
        inventory.onItemChangedCallback += UpdateUI;

        slots = backPanel.GetComponentsInChildren<InventorySlot>();
	}

    // Update is called once per frame
    void Update()
    {

        if (Input.GetKeyDown(KeyCode.I))
        {
            inventoryUI.SetActive(!inventoryUI.activeSelf);
        }
    }

    private void UpdateUI()
    {
        //Debug.Log("Updating UI");

        for (int i = 0; i < slots.Length; i++)
        {
            if (i < inventory.items.Count)
            {
                slots[i].AddItem(inventory.items[i]);
            }
            else
            {
                slots[i].ClearSlot();
            }
        }
    }
}
