using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawning : MonoBehaviour
{
    public GameObject enemy;
    public Transform enemies;

    [SerializeField] List<float> OnexOneSpawnChances = new List<float>(new float[] { 0.75f, 0.50f, 0.25f });
    [SerializeField] List<float> OnexTwoSpawnChances = new List<float>(new float[] { 1f, 0.75f, 0.5f, 0.25f });
    [SerializeField] List<float> TwoxOneSpawnChances = new List<float>(new float[] { 1f, 0.75f, 0.5f, 0.25f });
    [SerializeField] List<float> TwoxTwoSpawnChances = new List<float>(new float[] { 1f, 1f, 0.75f, 0.5f, 0.25f });
    [SerializeField] List<float> ThreexThreeSpawnChances = new List<float>(new float[] { 1f, 1f, 1f, 0.75f, 0.75f, 0.75f, 0.5f, 0.5f, 0.25f });

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
                    GameObject newEnemy = Instantiate(enemy, room.getRandomPosition(), Quaternion.identity);
                    numEnemiesSpawned++;
                    Debug.Log("Spawned enemy in Room " + room.roomRef.transform.position + " at pos: " + room.getRandomPosition());
                    newEnemy.transform.parent = enemies;
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
