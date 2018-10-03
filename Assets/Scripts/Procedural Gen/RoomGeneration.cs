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

    //List of all rooms
    List<Room> rooms = new List<Room>();
    //List of all room locations
    List<Vector2> takenPos = new List<Vector2>();
    //List of all room locations that have at least one open neighboring position
    List<Vector2> openTakenPos = new List<Vector2>();
    //List of all room locations that have at most one neighboring position
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
        //Add starter room in middle
        //TODO: Different type for starter room (Change 0 to another number)
        Room startRoom = new Room(new Vector2(areaSizeX / 2, areaSizeY / 2), 0);
        rooms.Insert(0, startRoom);
        takenPos.Insert(0, startRoom.location);
        openTakenPos.Insert(0, startRoom.location);
        singleNeighborTakenPos.Insert(0, startRoom.location);

        //Add each room to the grid
        for (int i = 0; i < numOfRooms - 1; i++)
        {
            //Determine type of new Room (somehow)
            int tempType = 0;

            //Get temp position of new room
            Vector2 tempLoc = getRandomPosition();
            Room tempRoom = new Room(tempLoc, tempType);

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

            int tempNeighbors = getNumNeighbors(tempRoom);

            //Determines if the random position of the new room will have more than one neighbor and uses branchProb to
            //decide whether or not to force the new room to be a branch position (a position with only one neighbor)
            if (tempNeighbors > 1 && branchProb > Random.value)
            {
                Vector2 tempBranchLoc = getRandomBranchPosition();
                Room tempBranchRoom = new Room(tempBranchLoc, tempType);

                //If it isn't the error vector, a branch position was found and it will be the position of the new room
                if (tempBranchLoc != errorVector)
                {
                    tempLoc = tempBranchLoc;
                    tempNeighbors = getNumNeighbors(tempBranchRoom);
                }
            }

            //Actually insert the room
            Room newRoom = new Room(tempLoc, tempType);
            rooms.Insert(0, newRoom);
            takenPos.Insert(0, newRoom.location);

            if (tempNeighbors < 4)
            {
                openTakenPos.Insert(0, newRoom.location);
            }
            if (tempNeighbors <= 1)
            {
                singleNeighborTakenPos.Insert(0, newRoom.location);
            }

            removeNotOpenTakenPos(newRoom);
            removeNotSingleNeighborTakenPos(newRoom);
        }
    }

    //Equation used in decreasing/increasing branchProb over time (curve fit)
    private float getBranchY(float x)
    {
        return ((-2.308f * Mathf.Pow(x, 3)) + (4.972f * Mathf.Pow(x, 2)) + (-3.620f * x) + 0.930f);
    }

    //Removes rooms that do not have an opening neighboring position from the openTakenPos list
    private void removeNotOpenTakenPos(Room room)
    {
        Room tempBottomRoom = new Room(room.location + Vector2.down, room.type);
        Room tempLeftRoom = new Room(room.location + Vector2.left, room.type);
        Room tempRightRoom = new Room(room.location + Vector2.right, room.type);
        Room tempUpperRoom = new Room(room.location + Vector2.up, room.type);

        if (openTakenPos.Contains(tempBottomRoom.location) && getNumNeighbors(tempBottomRoom) >= 4)
        {
            openTakenPos.Remove(tempBottomRoom.location);
        }
        if (openTakenPos.Contains(tempLeftRoom.location) && getNumNeighbors(tempLeftRoom) >= 4)
        {
            openTakenPos.Remove(tempLeftRoom.location);
        }
        if (openTakenPos.Contains(tempRightRoom.location) && getNumNeighbors(tempRightRoom) >= 4)
        {
            openTakenPos.Remove(tempRightRoom.location);
        }
        if (openTakenPos.Contains(tempUpperRoom.location) && getNumNeighbors(tempUpperRoom) >= 4)
        {
            openTakenPos.Remove(tempUpperRoom.location);
        }
    }

    //Removes rooms that do not have at most one neighboring position from the singleNeighborTakenPos list
    private void removeNotSingleNeighborTakenPos(Room room)
    {
        Room tempBottomRoom = new Room(room.location + Vector2.down, room.type);
        Room tempLeftRoom = new Room(room.location + Vector2.left, room.type);
        Room tempRightRoom = new Room(room.location + Vector2.right, room.type);
        Room tempUpperRoom = new Room(room.location + Vector2.up, room.type);

        if (singleNeighborTakenPos.Contains(tempBottomRoom.location) && getNumNeighbors(tempBottomRoom) > 1)
        {
            singleNeighborTakenPos.Remove(tempBottomRoom.location);
        }
        if (singleNeighborTakenPos.Contains(tempLeftRoom.location) && getNumNeighbors(tempLeftRoom) > 1)
        {
            singleNeighborTakenPos.Remove(tempLeftRoom.location);
        }
        if (singleNeighborTakenPos.Contains(tempRightRoom.location) && getNumNeighbors(tempRightRoom) > 1)
        {
            singleNeighborTakenPos.Remove(tempRightRoom.location);
        }
        if (singleNeighborTakenPos.Contains(tempUpperRoom.location) && getNumNeighbors(tempUpperRoom) > 1)
        {
            singleNeighborTakenPos.Remove(tempUpperRoom.location);
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
            Room newRoom = new Room(randomPos, 0);

            //If this new location does not meet location requirements
            if (iterationsDir >= 16
                || takenPos.Contains(newRoom.location)
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
            Room newRoom = new Room(randomPos, 0);

            //If this new location does not meet branching requirements
            if (iterationsDir >= 16
                || takenPos.Contains(newRoom.location)
                || getNumNeighbors(newRoom) > 1
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

    private void SetRoomDoors()
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            if (hasBottomNeighbor(rooms[i]))
            {
                rooms[i].doorBottom = true;
            }
            else
            {
                rooms[i].doorBottom = false;
            }

            if (hasLeftNeighbor(rooms[i]))
            {
                rooms[i].doorLeft = true;
            }
            else
            {
                rooms[i].doorLeft = false;
            }

            if (hasRightNeighbor(rooms[i]))
            {
                rooms[i].doorRight = true;
            }
            else
            {
                rooms[i].doorRight = false;
            }

            if (hasUpperNeighbor(rooms[i]))
            {
                rooms[i].doorUpper = true;
            }
            else
            {
                rooms[i].doorUpper = false;
            }
        }
    }

    //Gets the number of neighboring rooms surrounding a given room
    private int getNumNeighbors(Room room)
    {
        int numRooms = 0;

        if (hasBottomNeighbor(room))
        {
            numRooms++;
        }
        if (hasLeftNeighbor(room))
        {
            numRooms++;
        }
        if (hasRightNeighbor(room))
        {
            numRooms++;
        }
        if (hasUpperNeighbor(room))
        {
            numRooms++;
        }

        return numRooms;

    }

    private bool hasBottomNeighbor(Room room)
    {
        return (takenPos.Contains(room.location + Vector2.down));
    }

    private bool hasLeftNeighbor(Room room)
    {
        return (takenPos.Contains(room.location + Vector2.left));
    }

    private bool hasRightNeighbor(Room room)
    {
        return (takenPos.Contains(room.location + Vector2.right));
    }

    private bool hasUpperNeighbor(Room room)
    {
        return (takenPos.Contains(room.location + Vector2.up));
    }

    private void BuildPrimitives()
    {
        Vector3 rot;
        int gridSize = 10;

        for (int i = 0; i < rooms.Count; i++)
        {
            float offsetX = rooms[i].location.x * gridSize;
            float offsetZ = rooms[i].location.y * gridSize;

            int doorCount = getNumNeighbors(rooms[i]);

            if (doorCount > 3)
            {
                GameObject rm = Instantiate(roomDoorAll, new Vector3(offsetX, 0, offsetZ), Quaternion.identity);
                rm.transform.parent = map;
                FillNavBaker(rm);
            }
            else if (doorCount > 2)
            {
                rot = getTriRotation(rooms[i]);
                GameObject rm = Instantiate(roomTri, new Vector3(offsetX, 0, offsetZ), Quaternion.Euler(rot));
                rm.transform.parent = map;
                FillNavBaker(rm);
            }
            else if (doorCount > 1)
            {
                if ((hasUpperNeighbor(rooms[i]) && hasBottomNeighbor(rooms[i]))
                    || (hasLeftNeighbor(rooms[i]) && hasRightNeighbor(rooms[i])))
                {
                    rot = getCorridorRotation(rooms[i]);
                    GameObject rm = Instantiate(roomCorridor, new Vector3(offsetX, 0, offsetZ), Quaternion.Euler(rot));
                    rm.transform.parent = map;
                    FillNavBaker(rm);
                }
                else
                {
                    rot = getCornerRotation(rooms[i]);
                    GameObject rm = Instantiate(roomCorner, new Vector3(offsetX, 0, offsetZ), Quaternion.Euler(rot));
                    rm.transform.parent = map;
                    FillNavBaker(rm);
                }
            }
            else
            {
                rot = getSingleRotation(rooms[i]);
                GameObject rm = Instantiate(roomDoorOne, new Vector3(offsetX, 0, offsetZ), Quaternion.Euler(rot));
                rm.transform.parent = map;
                FillNavBaker(rm);
            }
        }

        map.transform.position = Vector3.zero;
        SetCharToMap();
    }

    private Vector3 getTriRotation(Room room)
    {
        bool bottom = hasBottomNeighbor(room);
        bool left = hasLeftNeighbor(room);
        bool right = hasRightNeighbor(room);
        bool upper = hasUpperNeighbor(room);

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

    private Vector3 getCorridorRotation(Room room)
    {
        bool bottom = hasBottomNeighbor(room);
        bool upper = hasUpperNeighbor(room);

        if (bottom && upper)
        {
            return new Vector3(0, 0, 0);
        }
        else
        {
            return new Vector3(0, 90, 0);
        }
    }

    private Vector3 getCornerRotation(Room room)
    {
        bool bottom = hasBottomNeighbor(room);
        bool left = hasLeftNeighbor(room);
        bool right = hasRightNeighbor(room);
        bool upper = hasUpperNeighbor(room);

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

    private Vector3 getSingleRotation(Room room)
    {
        bool bottom = hasBottomNeighbor(room);
        bool left = hasLeftNeighbor(room);
        bool upper = hasUpperNeighbor(room);

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
