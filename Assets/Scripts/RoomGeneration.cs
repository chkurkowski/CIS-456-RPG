using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomGeneration : MonoBehaviour {

    [SerializeField] int areaSizeX = 5; //Size of the grid on the x axis
    [SerializeField] int areaSizeY = 5; //Size of the grid on the y axis
    [SerializeField] int numOfRooms = 10; //Number of rooms to add to the grid

    //2D array where the rooms are added
    Room[,] rooms;

    //List of all rooms and their locations in the "rooms" array
    List<Vector2> takenPos = new List<Vector2>(); 


	// Use this for initialization
	void Start ()
    {
        //If there are more rooms than can fit in the grid
        if (numOfRooms >= (areaSizeX * areaSizeY))
        {
            numOfRooms = Mathf.RoundToInt(areaSizeX * areaSizeY);
        }

        CreateRooms();
        SetRoomDoors();
        DebugPrintArray();
    }

    //Populates the "rooms" array with rooms
    private void CreateRooms()
    {
        //Initializes the rooms array
        rooms = new Room[areaSizeX, areaSizeY];

        //Add starter room in middle (TODO: probably want a different type for the starter room)
        Vector2 startRoom = new Vector2((int)(areaSizeX / 2), (int)(areaSizeY / 2));
        rooms[((int)(areaSizeX / 2)), ((int)(areaSizeY / 2))] = new Room(startRoom, 0);
        takenPos.Insert(0, startRoom);

        //Add each room to the grid
        for (int i = 0; i < numOfRooms - 1; i++)
        {
            //Get temp position of new room
            Vector2 temp = getRandomPosition();

            //TODO: Check how many neighbors it has
            //TODO: Math to encourage branching and select a "better" new room (so it's not one giant cube of rooms)

            //Actually insert the room to the "rooms" array
            rooms[((int) temp.x), ((int) temp.y)] = new Room(temp, 0);
            takenPos.Insert(0, temp);
        }
    }

    //Gets a random position that's adjacent to a random room
    private Vector2 getRandomPosition()
    {
        Vector2 randomPos;
        bool validRandomPos;

        do
        {
            //Pick a random room that's already in the grid
            //TODO: Make more efficient so it remembers which rooms have available neighbors instead of choosing at random
            int index = Mathf.RoundToInt(Random.value * (takenPos.Count - 1));

            int x = (int) takenPos[index].x;
            int y = (int) takenPos[index].y;

            //Move one (random) direction over from the random room we selected
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

            //If this new location already has a room there or if it's not in the grid, go through the loop again
            if (takenPos.Contains(randomPos) || x >= areaSizeX || x < 0 || y >= areaSizeY || y < 0)
            {
                validRandomPos = false;
            }
            else
            {
                //Not having this else statement made Unity crash for an hour straight before I realized...
                validRandomPos = true;
            }
        }
        while (!validRandomPos);

        return randomPos;
    }

    //Once all the rooms are put on the grid, mark each room connect to its neighbors
    //TODO: Possibly make it so some rooms only have one entrance (ie. a room surrounded by 3 rooms can be entered from only one of the surrouding rooms)
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

    //Solely to visualize the randomized rooms until we actually implement it
    private void DebugPrintArray()
    {
        string line = "";
        for (int x = 0; x < areaSizeX; x++)
        {
            for (int y = 0; y < areaSizeY; y++)
            {
                if (rooms[x, y] != null)
                {
                    line += "[R] ";
                }
                else
                {
                    line += "[X] ";
                }
            }
            Debug.Log(line);
            line = "";
        }
    }
}
