using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour {

    public Image icon;
    public Button removeButton;

    public Items item;

    public void UseItem()
    {
        item.Use();
    }

    public void AddItem(Items newItem)
    {
        item = newItem;

        icon.sprite = item.icon;
        icon.enabled = true;
        removeButton.interactable = true;
    }

    public void ClearSlot()
    {
        item = null;

        icon.sprite = null;
        icon.enabled = false;
        removeButton.interactable = false;
    }

    public virtual void OnRemoveButton()
    {
        InventoryManager.instance.Remove(item);
    }

}
