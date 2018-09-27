using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RoomGeneration : MonoBehaviour
{

    public Transform map;
    public GameObject room, roomDoorAll, roomTri, roomCorner, roomCorridor, roomDoorOne;
    public GameObject character;
    private NavigationBaker baker;

    [SerializeField] int areaSizeX = 5; //Size of the grid on the x axis
    [SerializeField] int areaSizeY = 5; //Size of the grid on the y axis
    [SerializeField] int numOfRooms = 20; //Number of rooms to add to the grid

    [SerializeField] float startBranchProb = 1.0f; //Branch probability when the first rooms are being created
    [SerializeField] float endBranchProb = 0.01f; //Branch probability when the last rooms are being created
    float branchProb; //Actual branch probability that gets decreased/increased over the course of adding all of the rooms
    float changeInProb; //The difference between startBranchProb and endBranchProb
    bool decreasing; //Whether the branchProb is decreasing or increasing

    //2D array where the rooms are added
    Room[,] rooms;

    //List of all rooms and their locations in the "rooms" array
    List<Vector2> takenPos = new List<Vector2>();

    //Placeholder vector in-case a randomBranchPosition can't be found in a timely manner
    Vector2 errorVector = new Vector2(3.14f, 3.14f);

    // Use this for initialization
    void Start()
    {
        baker = FindObjectOfType<NavigationBaker>();
        baker.roomCount = numOfRooms;

        branchProb = startBranchProb;
        changeInProb = Mathf.Abs(startBranchProb - endBranchProb);

        if (startBranchProb >= endBranchProb)
        {
            decreasing = true;
        }
        else
        {
            decreasing = false;
        }

        //If there are more rooms than can fit in the grid
        if (numOfRooms >= (areaSizeX * areaSizeY))
        {
            numOfRooms = Mathf.RoundToInt(areaSizeX * areaSizeY);
        }

        CreateRooms();
        SetRoomDoors();
        DebugPrintArray();
        BuildPrimitives();
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

            //Decreases/Increases branchProb for this iteration
            if (decreasing)
            {
                float num = getBranchY(((float)i) / (numOfRooms - 1));
                branchProb = Mathf.Clamp((startBranchProb - (changeInProb - (changeInProb * num))), endBranchProb, startBranchProb);
            }
            else
            {
                float num = getBranchY(((float)i) / (numOfRooms - 1));
                branchProb = Mathf.Clamp((startBranchProb + (changeInProb - (changeInProb * num))), startBranchProb, endBranchProb);
            }

            //Determines if the random position of the new room will have more than one neighbor and uses branchProb to
            //decide whether or not to force the new room to be a branch position (a position with only one neighbor)
            if (getNumNeighbors(temp) > 1 && branchProb > Random.value)
            {
                Vector2 tempBranch = getRandomBranchPosition();

                //If it is the errorVector, a branch position couldn't be found and will use the original random position
                if (tempBranch != errorVector)
                {
                    temp = tempBranch;
                }
            }

            //Actually insert the room to the "rooms" array
            rooms[((int)temp.x), ((int)temp.y)] = new Room(temp, 0);
            takenPos.Insert(0, temp);
        }
    }

    //Equation used in decreasing/increasing branchProb over time (curve fit)
    private float getBranchY(float x)
    {
        return ((-2.308f * Mathf.Pow(x, 3)) + (4.972f * Mathf.Pow(x, 2)) + (-3.620f * x) + 0.930f);
    }

    //Gets a random position that's adjacent to a random room
    //TODO: Apply efficiency changes from getRandomBranchPosition
    private Vector2 getRandomPosition()
    {
        Vector2 randomPos;
        bool validRandomPos;

        do
        {
            validRandomPos = true;

            //Pick a random room that's already in the grid
            //TODO: Make more efficient so it remembers which rooms have available neighbors instead of choosing at random
            int index = Mathf.RoundToInt(Random.value * (takenPos.Count - 1));

            int x = (int)takenPos[index].x;
            int y = (int)takenPos[index].y;

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
        }
        while (!validRandomPos);

        return randomPos;
    }

    //Gets a random position that's adjacent to only one random room (branching)
    private Vector2 getRandomBranchPosition()
    {
        Vector2 randomPos;
        bool validRandomPos;
        int index;
        int iterationsMain = 0; //Iterations of the main do while loop
        int iterationsRoom = 0; //Iterations of the do while loop that selects a random room with only one neighbor
        int iterationsDir = 0; //Iterations of the do while loop that selects which direction to deviate from the random room

        do
        {
            //Pick a random room that's already in the grid that has only one neighbor
            //TODO: Make more efficient so it remembers which rooms have available neighbors instead of choosing at random
            iterationsRoom = 0;
            do
            {
                index = Mathf.RoundToInt(Random.value * (takenPos.Count - 1));
                iterationsRoom++;
            }
            while (getNumNeighbors(new Vector2((int)takenPos[index].x, (int)takenPos[index].y)) > 1 && iterationsRoom < 100);

            int x = (int)takenPos[index].x;
            int y = (int)takenPos[index].y;

            //Move one (random) direction over from the random room we selected
            iterationsDir = 0;
            bool isValidDir;
            do
            {
                isValidDir = true;

                float dir = Random.value;

                if (dir < 0.25f && !(takenPos.Contains(new Vector2(x, y - 1)))) //Down
                {
                    y -= 1;
                }
                else if (dir < 0.50f && !(takenPos.Contains(new Vector2(x - 1, y)))) //Left
                {
                    x -= 1;
                }
                else if (dir < .75f && !(takenPos.Contains(new Vector2(x + 1, y)))) //Right
                {
                    x += 1;
                }
                else if (dir >= .75 && !(takenPos.Contains(new Vector2(x, y + 1))))//Up
                {
                    y += 1;
                }
                else
                {
                    isValidDir = false;
                    iterationsDir++;
                }
            }
            while (!isValidDir && iterationsDir < 100);

            randomPos = new Vector2(x, y);

            //If this new location does not meet branching requirements
            if (iterationsRoom >= 100
                || iterationsDir >= 100
                || takenPos.Contains(randomPos)
                || getNumNeighbors(randomPos) > 1
                || x >= areaSizeX
                || x < 0
                || y >= areaSizeY
                || y < 0)
            {
                validRandomPos = false;
                iterationsMain++;
            }
            else
            {
                validRandomPos = true;
            }
        }
        while (!validRandomPos && iterationsMain < 100);

        //If a branch position was unable to be found
        if (iterationsMain >= 100)
        {
            return errorVector;
        }

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
                if (rooms[x, y] == null)
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

    //Gets the number of neighboring rooms surrounding a given room
    private int getNumNeighbors(Vector2 location)
    {
        int numRooms = 0;

        if (takenPos.Contains(location + Vector2.down))
        {
            numRooms++;
        }
        if (takenPos.Contains(location + Vector2.left))
        {
            numRooms++;
        }
        if (takenPos.Contains(location + Vector2.right))
        {
            numRooms++;
        }
        if (takenPos.Contains(location + Vector2.up))
        {
            numRooms++;
        }

        return numRooms;

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

    //Instatiates the rooms and offsets them to build out the map
    private void BuildPrimitives()
    {
        float offsetX = 0;
        float offsetZ = 0;

        Quaternion rot = Quaternion.identity;

        for (int x = 0; x < areaSizeX; x++)
        {
            for (int y = 0; y < areaSizeY; y++)
            {
                if (rooms[x, y] != null)
                {
                    //Checks how many doors a room has
                    int doorCount = 0;
                    if (rooms[x, y].doorTop)
                    {
                        doorCount++;
                        rot = Quaternion.LookRotation(Vector3.back);
                    }
                    if (rooms[x, y].doorBottom)
                    {
                        doorCount++;
                        rot = Quaternion.LookRotation(Vector3.forward);
                    }
                    if (rooms[x, y].doorLeft)
                    {
                        doorCount++;
                        rot = Quaternion.LookRotation(Vector3.right);
                    }
                    if (rooms[x, y].doorRight)
                    {
                        doorCount++;
                        rot = Quaternion.LookRotation(Vector3.left);
                    }

                    //Spawns a different room based on the amount of doors/neighboring rooms then parents them to the map object in the world.

                    if(doorCount > 3)
                    {
                        GameObject rm = Instantiate(roomDoorAll, new Vector3(offsetX, 0, offsetZ), Quaternion.identity);
                        rm.transform.parent = map;
                        FillNavBaker(rm);
                    }
                    else if(doorCount > 2)
                    {
                        GameObject rm = Instantiate(roomTri, new Vector3(offsetX, 0, offsetZ), Quaternion.identity);
                        rm.transform.parent = map;
                        FillNavBaker(rm);
                    }
                    else if(doorCount > 1)
                    {
                        if((rooms[x, y].doorTop && rooms[x, y].doorBottom) || (rooms[x, y].doorLeft && rooms[x, y].doorRight))
                        {
                            GameObject rm = Instantiate(roomCorridor, new Vector3(offsetX, 0, offsetZ), Quaternion.identity);
                            rm.transform.parent = map;
                            FillNavBaker(rm);
                        }
                        else
                        {
                            GameObject rm = Instantiate(roomCorner, new Vector3(offsetX, 0, offsetZ), Quaternion.identity);
                            rm.transform.parent = map;
                            FillNavBaker(rm);
                        }
                    }
                    else
                    {
                        GameObject rm = Instantiate(roomDoorOne, new Vector3(offsetX, 0, offsetZ), rot);
                        rm.transform.parent = map;
                        FillNavBaker(rm);
                    }

                }
                offsetZ += 10;
            }
            offsetX += 10;
            offsetZ = 0;

        }
        map.transform.position = Vector3.zero;
        SetCharToMap();
    }

    private void FillNavBaker(GameObject rm)
    {
        baker.surfaces.Add(rm.GetComponent<NavMeshSurface>());
        print(baker.surfaces[baker.surfaces.Count - 1]);
    }

    private void SetCharToMap()
    {
        character.transform.position = baker.surfaces[Random.Range(0, baker.surfaces.Count)].gameObject.transform.position;
    }
}
