using System.Collections.Generic;

public class InventoryItems {

    #region Singleton

    private static InventoryItems inst;

    private InventoryItems()
    {
    }

    public static InventoryItems Instance
    {
        get
        {
            if (inst == null)
            {
                inst = new InventoryItems();
            }
            return inst;
        }
    }

    #endregion

    public List<Items> items = new List<Items>();

}
