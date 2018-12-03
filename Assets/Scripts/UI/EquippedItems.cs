
public class EquippedItems {

    public Equipment[] currentEquipment;

    private EquippedItems()
    {
    }

    public void GenerateArray(int i)
    {
        if(currentEquipment == null)
            currentEquipment = new Equipment[i];
        return;
    }

    #region Singleton

    private static EquippedItems inst;

    public static EquippedItems Instance
    {
        get
        {
            if (inst == null)
            {
                inst = new EquippedItems();
            }
            return inst;
        }
    }

    #endregion



}
