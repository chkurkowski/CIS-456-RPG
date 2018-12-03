using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Potions")]
public class Potions : Items {

    public PotionType potion;

    public int potionStrength = 0;

    private PlayerStats player = PlayerStats.Instance;

    public override void Use()
    {
        base.Use();

        PotionEffect();
        RemoveFromInventory();
    }

    private void PotionEffect()
    {
        if(potionStrength != 0 && potion == PotionType.Health)
        {
            player.setHealth(player.getHealth() + potionStrength);
        }
        else if(potionStrength != 0 && potion == PotionType.Mana)
        {
            //TODO Implement Mana
        }
    }
}

public enum PotionType { Health, Mana};