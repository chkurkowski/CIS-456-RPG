using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawning : MonoBehaviour
{
    public GameObject enemy;

    [SerializeField] List<float> OnexOneSpawnChances;
    [SerializeField] List<float> OnexTwoSpawnChances;
    [SerializeField] List<float> TwoxOneSpawnChances;
    [SerializeField] List<float> TwoxTwoSpawnChances;
    [SerializeField] List<float> ThreexThreeSpawnChances;

    private RoomGeneration roomGen;
    private List<Room> rooms;

    //Useful Vectors
    private Vector2 OnexOne = new Vector2(1f, 1f);
    private Vector2 OnexTwo = new Vector2(1f, 2f);
    private Vector2 TwoxOne = new Vector2(2f, 1f);
    private Vector2 TwoxTwo = new Vector2(2f, 2f);
    private Vector2 ThreexThree = new Vector2(3f, 3f);

    void Start ()
    {
        roomGen = FindObjectOfType<RoomGeneration>();
        rooms = roomGen.getAllRooms();

        spawnEnemies();
	}

    private void spawnEnemies()
    {
        foreach (Room room in rooms)
        {
            List<float> spawnChances = getSpawnChances(room.size);
            int numEnemiesSpawned = 0;
            bool spawnEnemy = true;

            do
            {
                float random = Random.value;

                if (numEnemiesSpawned < spawnChances.Count
                    && random <= spawnChances[numEnemiesSpawned])
                {
                    //GameObject rm = Instantiate(enemy, room.getRandomPosition(), Quaternion.identity);
                    numEnemiesSpawned++;
                }
                else
                {
                    spawnEnemy = false;
                }
            }
            while (spawnEnemy);
        }
    }

    private List<float> getSpawnChances(Vector2 roomSize)
    {
        if (roomSize == OnexOne)
        {
            return OnexOneSpawnChances;
        }
        else if (roomSize == OnexTwo)
        {
            return OnexTwoSpawnChances;
        }
        else if (roomSize == TwoxOne)
        {
            return TwoxOneSpawnChances;
        }
        else if (roomSize == TwoxTwo)
        {
            return TwoxTwoSpawnChances;
        }
        else
        {
            return ThreexThreeSpawnChances;
        }
    }
}
