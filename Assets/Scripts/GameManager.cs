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
    public GameObject inGameUI;
    public GameObject inventoryUI;
    public GameObject gameOverUI;

    private static int initialNumOfRooms = 10;

    private PlayerStats stats;
    private CharController charController;

    [SerializeField] private static int level = 1;

    private void Start()
    {
        stats = PlayerStats.Instance;
        charController = FindObjectOfType<CharController>();
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
        charController.canMove = false;
        inGameUI.SetActive(false);
        inventoryUI.SetActive(false);
        gameOverUI.SetActive(true);
    }

    public void Restart()
    {
        level = 1;
        PlayerStats.Instance.Reset();
        EquipmentManager.instance.UnequipAll();
        InventoryManager.instance.RemoveAll();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        level = 1;
        PlayerStats.Instance.Reset();
        EquipmentManager.instance.UnequipAll();
        InventoryManager.instance.RemoveAll();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }
}
