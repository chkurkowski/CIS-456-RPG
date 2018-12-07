using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {

    #region Singleton

    public static GameManager instance;

    private void Awake()
    {
        instance = this;
    }

    #endregion

    public Text goldText;

    private static int initialNumOfRooms = 10;
    private PlayerStats stats;
    [SerializeField]
    private static int level = 1;

    private void Start()
    {
        stats = PlayerStats.Instance;
    }

    public int getLevel()
    {
        return level;
    }


    public int getNewLevelInitialNumOfRooms()
    {
        if (level == 1)
        {
            initialNumOfRooms = 10;
        }
        else if (level >= 16)
        {
            initialNumOfRooms = 55;
        }
        else if (level >= 13)
        {
            initialNumOfRooms++;
        }
        else if (level >= 10)
        {
            initialNumOfRooms += 2;
        }
        else if (level >= 7)
        {
            initialNumOfRooms += 3;
        }
        else if (level >= 4)
        {
            initialNumOfRooms += 4;
        }
        else
        {
            initialNumOfRooms += 5;
        }

        return initialNumOfRooms;
    }

    public void UpdateGoldText()
    {
        goldText.text = stats.getGold().ToString();
    }

    public void levelPP()
    {
        level++;
    }

    public void LoadScene(string s)
    {
        SceneManager.LoadScene(s);
    }

	public void EndGame()
    {
        //TODO: Game over screen ON THIS LINE
        level = 1;
        PlayerStats.Instance.Reset();
        EquipmentManager.instance.UnequipAll();
        InventoryManager.instance.RemoveAll();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
