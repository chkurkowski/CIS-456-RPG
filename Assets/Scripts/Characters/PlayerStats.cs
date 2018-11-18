using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats {

    [SerializeField] private const float maxHealth = 100f;
    [SerializeField] private static float health = 100f;
    [SerializeField] private static int gold = 0;
    private static PlayerStats inst;

    private PlayerStats()
    {
    }

    public static PlayerStats Instance
    {
        get
        {
            if (inst == null)
            {
                inst = new PlayerStats();
            }
            return inst;
        }
    }

    public void Reset()
    {
        health = maxHealth;
        gold = 0;
    }

    public float getHealth()
    {
        return health;
    }

    public int getGold()
    {
        return gold;
    }

    public void setHealth(float h)
    {
        health = h;
    }

    public void setGold(int g)
    {
        gold = g;
    }

    public void addGold(int g)
    {
        gold += g;
    }

    public float getHealthPercentage()
    {
        return health / maxHealth;
    }
}
