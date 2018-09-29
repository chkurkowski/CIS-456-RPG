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
    //List of all rooms that have at least one open neighboring position
    List<Vector2> openTakenPos = new List<Vector2>();
    //List of all rooms that have at most one neighboring position
    List<Vector2> singleNeighborTakenPos = new List<Vector2>();

    //Placeholder vector in case a randomBranchPosition can't be found in a timely manner
    Vector2 errorVector = new Vector2(3.14f, 3.14f);

    // Initilization
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
        openTakenPos.Insert(0, startRoom);
        singleNeighborTakenPos.Insert(0, startRoom);

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

            int tempNeighbors = getNumNeighbors(temp);

            //Determines if the random position of the new room will have more than one neighbor and uses branchProb to
            //decide whether or not to force the new room to be a branch position (a position with only one neighbor)
            if (tempNeighbors > 1 && branchProb > Random.value)
            {
                Vector2 tempBranch = getRandomBranchPosition();

                //If it isn't the error vector, a branch position was found and it will be the position of the new room
                if (tempBranch != errorVector)
                {
                    temp = tempBranch;
                    tempNeighbors = getNumNeighbors(temp);
                }
            }

            //Actually insert the room to the "rooms" array
            rooms[((int)temp.x), ((int)temp.y)] = new Room(temp, 0);
            takenPos.Insert(0, temp);

            if (tempNeighbors < 4)
            {
                openTakenPos.Insert(0, temp);
            }
            if (tempNeighbors <= 1)
            {
                singleNeighborTakenPos.Insert(0, temp);
            }

            removeNotOpenTakenPos(temp);
            removeNotSingleNeighborTakenPos(temp);
        }
    }

    //Equation used in decreasing/increasing branchProb over time (curve fit)
    private float getBranchY(float x)
    {
        return ((-2.308f * Mathf.Pow(x, 3)) + (4.972f * Mathf.Pow(x, 2)) + (-3.620f * x) + 0.930f);
    }

    //Removes rooms that do not have an opening neighboring position from the openTakenPos list
    private void removeNotOpenTakenPos(Vector2 location)
    {
        if (openTakenPos.Contains(location + Vector2.down) && getNumNeighbors(location + Vector2.down) >= 4)
        {
            openTakenPos.Remove(location + Vector2.down);
        }
        if (openTakenPos.Contains(location + Vector2.left) && getNumNeighbors(location + Vector2.left) >= 4)
        {
            openTakenPos.Remove(location + Vector2.left);
        }
        if (openTakenPos.Contains(location + Vector2.right) && getNumNeighbors(location + Vector2.right) >= 4)
        {
            openTakenPos.Remove(location + Vector2.right);
        }
        if (openTakenPos.Contains(location + Vector2.up) && getNumNeighbors(location + Vector2.up) >= 4)
        {
            openTakenPos.Remove(location + Vector2.up);
        }
    }

    //Removes rooms that do not have at most one neighboring position from the singleNeighborTakenPos list
    private void removeNotSingleNeighborTakenPos(Vector2 location)
    {
        if (singleNeighborTakenPos.Contains(location + Vector2.down) && getNumNeighbors(location + Vector2.down) > 1)
        {
            singleNeighborTakenPos.Remove(location + Vector2.down);
        }
        if (singleNeighborTakenPos.Contains(location + Vector2.left) && getNumNeighbors(location + Vector2.left) > 1)
        {
            singleNeighborTakenPos.Remove(location + Vector2.left);
        }
        if (singleNeighborTakenPos.Contains(location + Vector2.right) && getNumNeighbors(location + Vector2.right) > 1)
        {
            singleNeighborTakenPos.Remove(location + Vector2.right);
        }
        if (singleNeighborTakenPos.Contains(location + Vector2.up) && getNumNeighbors(location + Vector2.up) > 1)
        {
            singleNeighborTakenPos.Remove(location + Vector2.up);
        }
    }

    //Gets a random position that's adjacent to a random room
    private Vector2 getRandomPosition()
    {
        Vector2 randomPos;
        bool validRandomPos;
        int index;
        int iterationsDir = 0; //Iterations of the do while loop that selects which direction to deviate from the random room

        do
        {
            //Pick a random room that's already in the grid that doesn't have four neighbors
            index = Mathf.RoundToInt(Random.value * (openTakenPos.Count - 1));

            int x = (int)openTakenPos[index].x;
            int y = (int)openTakenPos[index].y;

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
                else if (dir >= .75 && !(takenPos.Contains(new Vector2(x, y + 1)))) //Up
                {
                    y += 1;
                }
                else
                {
                    isValidDir = false;
                    iterationsDir++;
                }
            }
            while (!isValidDir && iterationsDir < 16);

            randomPos = new Vector2(x, y);

            //If this new location does not meet location requirements
            if (iterationsDir >= 16
                || takenPos.Contains(randomPos)
                || x >= areaSizeX
                || x < 0
                || y >= areaSizeY
                || y < 0)
            {
                validRandomPos = false;
            }
            else
            {
                validRandomPos = true;
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
        int iterationsDir = 0; //Iterations of the do while loop that selects which direction to deviate from the random room

        do
        {
            //Pick a random room that's already in the grid that has only one neighbor
            index = Mathf.RoundToInt(Random.value * (singleNeighborTakenPos.Count - 1));

            int x = (int)singleNeighborTakenPos[index].x;
            int y = (int)singleNeighborTakenPos[index].y;

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
                else if (dir >= .75 && !(takenPos.Contains(new Vector2(x, y + 1)))) //Up
                {
                    y += 1;
                }
                else
                {
                    isValidDir = false;
                    iterationsDir++;
                }
            }
            while (!isValidDir && iterationsDir < 16);

            randomPos = new Vector2(x, y);

            //If this new location does not meet branching requirements
            if (iterationsDir >= 16
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
        while (!validRandomPos && iterationsMain < (2 * numOfRooms));

        //If a branch position was unable to be found
        if (iterationsMain >= (2 * numOfRooms))
        {
            return errorVector;
        }

        return randomPos;
    }

    //Once all the rooms are put on the grid, mark each room connect to its neighbors
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

        if (hasBottomNeighbor(location))
        {
            numRooms++;
        }
        if (hasLeftNeighbor(location))
        {
            numRooms++;
        }
        if (hasRightNeighbor(location))
        {
            numRooms++;
        }
        if (hasUpperNeighbor(location))
        {
            numRooms++;
        }

        return numRooms;

    }

    private bool hasBottomNeighbor(Vector2 location)
    {
        return (takenPos.Contains(location + Vector2.down));
    }

    private bool hasLeftNeighbor(Vector2 location)
    {
        return (takenPos.Contains(location + Vector2.left));
    }

    private bool hasRightNeighbor(Vector2 location)
    {
        return (takenPos.Contains(location + Vector2.right));
    }

    private bool hasUpperNeighbor(Vector2 location)
    {
        return (takenPos.Contains(location + Vector2.up));
    }

    private void BuildPrimitives()
    {
        Vector3 rot;
        int roomSize = 10; //Will have to be gotten for every room if we add different sizes down the line

        for (int i = 0; i < takenPos.Count; i++)
        {
            float offsetX = takenPos[i].x * roomSize;
            float offsetZ = takenPos[i].y * roomSize;

            int doorCount = getNumNeighbors(takenPos[i]);

            if (doorCount > 3)
            {
                GameObject rm = Instantiate(roomDoorAll, new Vector3(offsetX, 0, offsetZ), Quaternion.identity);
                rm.transform.parent = map;
                FillNavBaker(rm);
            }
            else if (doorCount > 2)
            {
                rot = getTriRotation(takenPos[i]);
                GameObject rm = Instantiate(roomTri, new Vector3(offsetX, 0, offsetZ), Quaternion.Euler(rot));
                rm.transform.parent = map;
                FillNavBaker(rm);
            }
            else if (doorCount > 1)
            {
                if ((hasUpperNeighbor(takenPos[i]) && hasBottomNeighbor(takenPos[i]))
                    || (hasLeftNeighbor(takenPos[i]) && hasRightNeighbor(takenPos[i])))
                {
                    rot = getCorridorRotation(takenPos[i]);
                    GameObject rm = Instantiate(roomCorridor, new Vector3(offsetX, 0, offsetZ), Quaternion.Euler(rot));
                    rm.transform.parent = map;
                    FillNavBaker(rm);
                }
                else
                {
                    rot = getCornerRotation(takenPos[i]);
                    GameObject rm = Instantiate(roomCorner, new Vector3(offsetX, 0, offsetZ), Quaternion.Euler(rot));
                    rm.transform.parent = map;
                    FillNavBaker(rm);
                }
            }
            else
            {
                rot = getSingleRotation(takenPos[i]);
                GameObject rm = Instantiate(roomDoorOne, new Vector3(offsetX, 0, offsetZ), Quaternion.Euler(rot));
                rm.transform.parent = map;
                FillNavBaker(rm);
            }
        }

        map.transform.position = Vector3.zero;
        SetCharToMap();
    }

    private Vector3 getTriRotation(Vector2 location)
    {
        bool bottom = hasBottomNeighbor(location);
        bool left = hasLeftNeighbor(location);
        bool right = hasRightNeighbor(location);
        bool upper = hasUpperNeighbor(location);

        if (right && bottom && left)
        {
            return new Vector3(0, 0, 0);
        }
        else if (bottom && left && upper)
        {
            return new Vector3(0, 90, 0);
        }
        else if (left && upper && right)
        {
            return new Vector3(0, 180, 0);
        }
        else
        {
            return new Vector3(0, 270, 0);
        }
    }

    private Vector3 getCorridorRotation(Vector2 location)
    {
        bool bottom = hasBottomNeighbor(location);
        bool upper = hasUpperNeighbor(location);

        if (bottom && upper)
        {
            return new Vector3(0, 0, 0);
        }
        else
        {
            return new Vector3(0, 90, 0);
        }
    }

    private Vector3 getCornerRotation(Vector2 location)
    {
        bool bottom = hasBottomNeighbor(location);
        bool left = hasLeftNeighbor(location);
        bool right = hasRightNeighbor(location);
        bool upper = hasUpperNeighbor(location);

        if (bottom && left)
        {
            return new Vector3(0, 0, 0);
        }
        else if (left && upper)
        {
            return new Vector3(0, 90, 0);
        }
        else if (upper && right)
        {
            return new Vector3(0, 180, 0);
        }
        else
        {
            return new Vector3(0, 270, 0);
        }
    }

    private Vector3 getSingleRotation(Vector2 location)
    {
        bool bottom = hasBottomNeighbor(location);
        bool left = hasLeftNeighbor(location);
        bool upper = hasUpperNeighbor(location);

        if (bottom)
        {
            return new Vector3(0, 0, 0);
        }
        else if (left)
        {
            return new Vector3(0, 90, 0);
        }
        else if (upper)
        {
            return new Vector3(0, 180, 0);
        }
        else
        {
            return new Vector3(0, 270, 0);
        }
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
