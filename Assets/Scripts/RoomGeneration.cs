using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomGeneration : MonoBehaviour {

    [SerializeField] int areaSizeX = 5;
    [SerializeField] int areaSizeY = 5;
    [SerializeField] int numOfRooms = 10;

    Vector2 areaSize;

    Room[,] rooms;
    List<Vector2> takenPos = new List<Vector2>();


	// Use this for initialization
	void Start () {
        areaSize = new Vector2(areaSizeX, areaSizeY);

        if (numOfRooms >= (areaSizeX * areaSizeY))
        {
            numOfRooms = Mathf.RoundToInt(areaSizeX * areaSizeY);
        }

        CreateRooms();
        SetRoomDoors();
    }

    private void CreateRooms()
    {
        rooms = new Room[areaSizeX, areaSizeY];

        //Add starter room in middle (TODO: probably want a different type)
        Vector2 startRoom = new Vector2((int)(areaSizeX / 2), (int)(areaSizeY / 2));
        rooms[(int)(areaSizeX / 2), (int)(areaSizeY / 2)] = new Room(startRoom, 0);
        takenPos.Add(startRoom);

        for (int i = 0; i < numOfRooms; i++)
        {
            //Get temp position of new room
            Vector2 temp = getRandomPosition();

            //Check how many neighbors it has
            //Math to encourage branching and select a "better" new room

            //Actually insert the room
            rooms[(int) temp.x, (int) temp.y] = new Room(temp, 0);
            takenPos.Add(temp);
        }
    }

    private Vector2 getRandomPosition()
    {
        Vector2 randomPos;
        bool validRandomPos = true;

        do
        {
            //TODO: Make more efficient so it remembers which rooms have available neighbors instead of choosing at random
            int index = Mathf.RoundToInt(Random.value * (numOfRooms - 1));
            int x = (int) takenPos[index].x;
            int y = (int) takenPos[index].y;

            float dir = Random.value;

            if (dir < 0.25f) //Down
            {
                y -= 1;
            }
            else if (dir < 0.50f) //Left
            {
                x -= 1;
            }
            else if (dir < .75f) //Right
            {
                x += 1;
            }
            else //Up
            {
                y += 1;
            }

            randomPos = new Vector2(x, y);

            if (takenPos.Contains(randomPos) || x >= areaSizeX || x < -areaSizeX || y >= areaSizeY || y < -areaSizeY)
            {
                validRandomPos = false;
            }
        }
        while (!validRandomPos);

        return randomPos;
    }

    private void SetRoomDoors()
    {
        for (int x = 0; x < areaSizeX; x++)
        {
            for (int y = 0; y < areaSizeY; y++)
            {
                //If there is no room at the position
                if (rooms[x,y] == null)
                {
                    continue;
                }

                //Check if room below
                if (y - 1 < 0)
                {
                    rooms[x, y].doorBottom = false;
                }
                else
                {
                    rooms[x, y].doorBottom = (rooms[x, y - 1] != null);
                }

                //Check if room left
                if (x - 1 < 0)
                {
                    rooms[x, y].doorLeft = false;
                }
                else
                {
                    rooms[x, y].doorLeft = (rooms[x - 1, y] != null);
                }

                //Check if room right
                if (x + 1 >= areaSizeX)
                {
                    rooms[x, y].doorRight = false;
                }
                else
                {
                    rooms[x, y].doorRight = (rooms[x + 1, y] != null);
                }

                //Check if room above
                if (y + 1 >= areaSizeY)
                {
                    rooms[x, y].doorTop = false;
                }
                else
                {
                    rooms[x, y].doorTop = (rooms[x, y + 1] != null);
                }
            }
        }
    }
}
