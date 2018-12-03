using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawning : MonoBehaviour
{
    public GameObject enemy;
    public GameObject tankEnemy;
    public Transform enemiesList;
    private List<GameObject> enemies;

    [SerializeField] List<float> EnemyOnexOneSpawnChances = new List<float>(new float[] { 0.5f, 0.5f });
    [SerializeField] List<float> EnemyOnexTwoSpawnChances = new List<float>(new float[] { 0.5f, 0.5f, 0.5f });
    [SerializeField] List<float> EnemyTwoxOneSpawnChances = new List<float>(new float[] { 0.5f, 0.5f, 0.5f });
    [SerializeField] List<float> EnemyTwoxTwoSpawnChances = new List<float>(new float[] { 0.75f, 0.75f, 0.5f, 0.5f });
    [SerializeField] List<float> EnemyThreexThreeSpawnChances = new List<float>(new float[] { 1f, 0.75f, 0.75f, 0.5f, 0.5f });

    [SerializeField] List<float> TankEnemyOnexOneSpawnChances = new List<float>(new float[] { 0.05f });
    [SerializeField] List<float> TankEnemyOnexTwoSpawnChances = new List<float>(new float[] { 0.15f });
    [SerializeField] List<float> TankEnemyTwoxOneSpawnChances = new List<float>(new float[] { 0.15f });
    [SerializeField] List<float> TankEnemyTwoxTwoSpawnChances = new List<float>(new float[] { 0.25f });
    [SerializeField] List<float> TankEnemyThreexThreeSpawnChances = new List<float>(new float[] { 0.75f, 0.25f });

    private RoomGeneration roomGen;
    private List<Room> rooms;

    //Useful Vectors
    private Vector2 OnexOne = new Vector2(1f, 1f);
    private Vector2 OnexTwo = new Vector2(1f, 2f);
    private Vector2 TwoxOne = new Vector2(2f, 1f);
    private Vector2 TwoxTwo = new Vector2(2f, 2f);
    private Vector2 ThreexThree = new Vector2(3f, 3f);

    public void spawnAllEnemies()
    {
        enemies = new List<GameObject>();

        roomGen = FindObjectOfType<RoomGeneration>();
        rooms = roomGen.getAllRooms();

        spawnEnemies();
	}

    private void spawnEnemies()
    {
        foreach (Room room in rooms)
        {
            if (room.type.ToLower().Equals("spawn"))
            {
                continue;
            }

            List<float> enemySpawnChances = getSpawnChances(enemy, room.size);
            List<float> tankEnemySpawnChances = getSpawnChances(tankEnemy, room.size);
            int numEnemiesSpawned = 0;
            int numTankEnemiesSpawned = 0;
            bool spawnEnemy = true;
            bool spawnTankEnemy = true;

            do
            {
                float random = Random.value;

                if (numEnemiesSpawned < enemySpawnChances.Count
                    && random <= enemySpawnChances[numEnemiesSpawned])
                {
                    GameObject newEnemy = Instantiate(enemy, room.getRandomPosition(), Quaternion.identity);
                    enemies.Add(newEnemy);
                    numEnemiesSpawned++;
                    newEnemy.transform.parent = enemiesList;
                }
                else
                {
                    spawnEnemy = false;
                }

                if (numTankEnemiesSpawned < tankEnemySpawnChances.Count
                    && random <= tankEnemySpawnChances[numTankEnemiesSpawned])
                {
                    GameObject newTankEnemy = Instantiate(tankEnemy, room.getRandomPosition(), Quaternion.identity);
                    enemies.Add(newTankEnemy);
                    numTankEnemiesSpawned++;
                    newTankEnemy.transform.parent = enemiesList;
                }
                else
                {
                    spawnTankEnemy = false;
                }
            }
            while (spawnEnemy || spawnTankEnemy);
        }
    }

    private List<float> getSpawnChances(GameObject enemyType, Vector2 roomSize)
    {
        if (enemyType == enemy)
        {
            if (roomSize == OnexOne)
            {
                return EnemyOnexOneSpawnChances;
            }
            else if (roomSize == OnexTwo)
            {
                return EnemyOnexTwoSpawnChances;
            }
            else if (roomSize == TwoxOne)
            {
                return EnemyTwoxOneSpawnChances;
            }
            else if (roomSize == TwoxTwo)
            {
                return EnemyTwoxTwoSpawnChances;
            }
            else
            {
                return EnemyThreexThreeSpawnChances;
            }
        }
        else if (enemyType == tankEnemy)
        {
            if (roomSize == OnexOne)
            {
                return TankEnemyOnexOneSpawnChances;
            }
            else if (roomSize == OnexTwo)
            {
                return TankEnemyOnexTwoSpawnChances;
            }
            else if (roomSize == TwoxOne)
            {
                return TankEnemyTwoxOneSpawnChances;
            }
            else if (roomSize == TwoxTwo)
            {
                return TankEnemyTwoxTwoSpawnChances;
            }
            else
            {
                return TankEnemyThreexThreeSpawnChances;
            }
        }
        else
        {
            return EnemyOnexOneSpawnChances;
        }
    }
}
