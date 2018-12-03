using UnityEngine;

public class InventoryUi : MonoBehaviour {

    public Transform backPanel;

    public GameObject inventoryUI;

    private InventoryManager inventory;
    private InventoryItems currItems = InventoryItems.Instance;

    private InventorySlot[] slots;

    private bool inventoryOpen = false;

	// Use this for initialization
	void Start () {
        inventory = InventoryManager.instance;
        inventory.onItemChangedCallback += UpdateUI;

        slots = backPanel.GetComponentsInChildren<InventorySlot>();
	}

    // Update is called once per frame
    void Update()
    {
        OpenInventory();
    }

    private void OpenInventory()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            inventory.onItemChangedCallback.Invoke();
            EquipmentManager.instance.onEquipmentChanged.Invoke();
            GameManager.instance.UpdateGoldText();
            inventoryUI.SetActive(!inventoryUI.activeSelf);
        }
    }

    private void UpdateUI()
    {
        //Debug.Log("Updating UI");

        for (int i = 0; i < slots.Length; i++)
        {
            if (i < currItems.items.Count)
            {
                slots[i].AddItem(currItems.items[i]);
            }
            else
            {
                slots[i].ClearSlot();
            }
        }
    }
}
