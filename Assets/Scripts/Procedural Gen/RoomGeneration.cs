using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

public class RoomGeneration : MonoBehaviour
{
    public Transform map;
    public GameObject OnexOneRoom;
    public GameObject OnexTwoRoom;
    public GameObject TwoxOneRoom;
    public GameObject TwoxTwoRoom;
    public GameObject ThreexThreeRoom;
    public GameObject RoomDoor;
    public GameObject character;
    public GameObject teleporter;
    private NavigationBaker baker;
    private EnemySpawning spawner;


    [SerializeField] int areaSizeX = 50; //Size of the grid on the x axis
    [SerializeField] int areaSizeY = 50; //Size of the grid on the y axis
    [SerializeField] int numOfRoomsInitial = 50; //Number of rooms to add to the grid
    public int numOfRoomsFinal; //Number of rooms actually added (including cycles and branching off of cycles)


    [SerializeField] float startBranchProb = 0.33f; //Branch probability when the first rooms are being created
    [SerializeField] float endBranchProb = 0.66f; //Branch probability when the last rooms are being created
    private float branchProb; //Actual branch probability that gets decreased/increased over the course of adding all of the rooms
    private float changeInProb; //The difference between startBranchProb and endBranchProb
    private bool decreasing; //Whether the branchProb is decreasing or increasing
    [SerializeField] float cycleBranchProb = 0.75f; //Probability to branch off of the rooms added when adding a cycle


    [SerializeField] float OnexOneRoomProb = 0.55f;
    [SerializeField] float OnexTwoRoomProb = 0.15f;
    [SerializeField] float TwoxOneRoomProb = 0.15f;
    [SerializeField] float TwoxTwoRoomProb = 0.1f;
    [SerializeField] float ThreexThreeRoomProb = 0.05f;


    private List<Room> rooms = new List<Room>(); //List of all rooms
    private List<Room> openRooms = new List<Room>(); //List of all rooms that have at least one open neighboring position
    private List<Room> singleNeighborRooms = new List<Room>(); //List of all rooms that have at most one neighboring position
    private List<Vector2> takenPos = new List<Vector2>(); //List of all occupied locations in the grid


    //Useful Vectors
    private Vector2 OnexOne = new Vector2(1f, 1f);
    private Vector2 OnexTwo = new Vector2(1f, 2f);
    private Vector2 TwoxOne = new Vector2(2f, 1f);
    private Vector2 TwoxTwo = new Vector2(2f, 2f);
    private Vector2 ThreexThree = new Vector2(3f, 3f);

    //Placeholder vector/Room to detect if something went wrong
    private Vector2 errorVector = new Vector2(3.14f, 3.14f);
    private Room errorRoom;

    //Initilization
    void Start()
    {
        errorRoom = new Room(errorVector);

        //If there are more rooms than can fit in the grid
        if (numOfRoomsInitial>= (areaSizeX * areaSizeY))
        {
            numOfRoomsInitial= Mathf.RoundToInt(areaSizeX * areaSizeY);
        }

        baker = FindObjectOfType<NavigationBaker>();

        spawner = FindObjectOfType<EnemySpawning>();

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

        if ((OnexOneRoomProb + OnexTwoRoomProb + TwoxOneRoomProb + TwoxTwoRoomProb + ThreexThreeRoomProb) > 1.01f)
        {
            throw new System.ArgumentOutOfRangeException("The sum of you room probabilities is greater than 1!");
        }

        float error = 1f;

        if (((OnexOneRoomProb + error) * numOfRoomsInitial * OnexOne.x * OnexOne.y)
            + ((OnexTwoRoomProb + error) * numOfRoomsInitial * OnexTwo.x * OnexTwo.y)
            + ((TwoxOneRoomProb + error) * numOfRoomsInitial * TwoxOne.x * TwoxOne.y)
            + ((TwoxTwoRoomProb + error) * numOfRoomsInitial * TwoxTwo.x * TwoxTwo.y)
            + ((ThreexThreeRoomProb + error) * numOfRoomsInitial * ThreexThree.x * ThreexThree.y)
            >= areaSizeX * areaSizeY * Mathf.Abs((startBranchProb + endBranchProb) / 2))
        {
            throw new System.ArgumentOutOfRangeException("Your room probabilities are likely to exceed the area size! Either increase area size or decrease the number of rooms.");
        }

        CreateRooms();
        numOfRoomsFinal = rooms.Count;
        baker.roomCount = numOfRoomsFinal;
        BuildPrimitives();
        AddObjects();
        baker.generateNavMesh();
        SpawnEnemies();
    }

    //Populates the "rooms" array with rooms
    private void CreateRooms()
    {
        //Add starter room in middle
        //TODO: Different type for starter room (Change 0 to another number)
        Room startRoom = new Room(new Vector2(areaSizeX / 2, areaSizeY / 2), new Vector2(1f, 1f));
        rooms.Add(startRoom);
        addLocationsToTakenPos(startRoom);
        openRooms.Add(startRoom);
        singleNeighborRooms.Add(startRoom);

        //Add each room to the grid
        for (int i = 0; i < numOfRoomsInitial- 1; i++)
        {
            //Determine type and size of new Room (somehow)
            string tempType = "";
            Vector2 tempSize;
            Vector2 tempLoc;

            do
            {
                tempSize = getRoomSize();
                tempLoc = getRandomRoomPosition(tempSize);
            }
            while (tempLoc == errorVector);

            Room tempRoom = new Room(tempLoc, tempSize, tempType);

            //Decreases/Increases branchProb for this iteration
            if (decreasing)
            {
                float num = getBranchY(((float)i) / (numOfRoomsInitial - 1));
                branchProb = Mathf.Clamp((startBranchProb - (changeInProb - (changeInProb * num))), endBranchProb, startBranchProb);
            }
            else
            {
                float num = getBranchY(((float)i) / (numOfRoomsInitial - 1));
                branchProb = Mathf.Clamp((startBranchProb + (changeInProb - (changeInProb * num))), startBranchProb, endBranchProb);
            }

            int tempNeighbors = getNumNeighbors(tempRoom);
            int tempUniqueNeighbors = getNumUniqueNeighbors(tempRoom);

            //Determines if the random position of the new room will have more than one neighbor and uses branchProb to
            //decide whether or not to force the new room to be a branch position (a position with only one neighbor)
            //TODO: Check each side of the room depending on its size
            if (branchProb > Random.value)
            {
                Vector2 tempBranchLoc = getRandomBranchRoomPosition(tempSize);
                Room tempBranchRoom = new Room(tempBranchLoc, tempSize, tempType);

                //If it isn't the error vector, a branch position was found and it will be the position of the new room
                if (tempBranchLoc != errorVector)
                {
                    tempLoc = tempBranchLoc;
                    tempNeighbors = getNumNeighbors(tempBranchRoom);
                    tempUniqueNeighbors = getNumUniqueNeighbors(tempBranchRoom);
                }
            }

            //Actually insert the room
            Room newRoom = new Room(tempLoc, tempSize, tempType);
            rooms.Add(newRoom);

            addLocationsToTakenPos(newRoom);
            setNeighboringRooms(newRoom);
            setRoomDoors(newRoom);

            if (tempNeighbors < newRoom.maxNeighbors)
            {
                openRooms.Add(newRoom);
            }
            if (tempUniqueNeighbors <= 1)
            {
                singleNeighborRooms.Add(newRoom);
            }

            removeNotOpenRooms(newRoom);
            removeNotSingleNeighborRooms(newRoom);
        }

        addCycles();
    }

    private void AddObjects()
    {
        GameObject tp = Instantiate(teleporter, rooms[numOfRoomsInitial - 1].getRandomPosition(4) + new Vector3(0f, 0.4f, 0f), Quaternion.identity);
    }

    //Gets a random room size
    private Vector2 getRoomSize()
    {
        float OneOne = OnexOneRoomProb;
        float OneTwo = OneOne + OnexTwoRoomProb;
        float TwoOne = OneTwo + TwoxOneRoomProb;
        float TwoTwo = TwoOne + TwoxTwoRoomProb;
        float ThreeThree = TwoTwo + ThreexThreeRoomProb;
        float random = Random.value;
        if (random < OneOne)
        {
            return OnexOne;
        }
        else if (random < OneTwo)
        {
            return OnexTwo;
        }
        else if (random < TwoOne)
        {
            return TwoxOne;
        }
        else if (random < TwoTwo)
        {
            return TwoxTwo;
        }
        else if (random < ThreeThree)
        {
            return ThreexThree;
        }
        else
        {
            return getRoomSize();
        }
    }

    //Equation used in decreasing/increasing branchProb over time (curve fit)
    private float getBranchY(float x)
    {
        return ((-2.308f * Mathf.Pow(x, 3)) + (4.972f * Mathf.Pow(x, 2)) + (-3.620f * x) + 0.930f);
    }

    private void addLocationsToTakenPos(Room room)
    {
        for (int i = 0; i < room.locations.Count; i++)
        {

            takenPos.Add(room.locations[i]);
        }
    }

    private void setNeighboringRooms(Room room)
    {
        if (room.size == OnexOne)
        {
            Vector2 tempBottom = room.getMiddle() + Vector2.down;
            Vector2 tempLeft = room.getMiddle() + Vector2.left;
            Vector2 tempRight = room.getMiddle() + Vector2.right;
            Vector2 tempTop = room.getMiddle() + Vector2.up;

            if (takenPos.Contains(tempBottom))
            {
                Room bottom = openRoomsContains(tempBottom);
                room.setRoomBottom(bottom);
                setAsTopNeighborTo(room, bottom);
                setRoomDoors(bottom);
            }
            if (takenPos.Contains(tempLeft))
            {
                Room left = openRoomsContains(tempLeft);
                room.setRoomLeft(left);
                setAsRightNeighborTo(room, left);
                setRoomDoors(left);
            }
            if (takenPos.Contains(tempRight))
            {
                Room right = openRoomsContains(tempRight);
                room.setRoomRight(right);
                setAsLeftNeighborTo(room, right);
                setRoomDoors(right);
            }
            if (takenPos.Contains(tempTop))
            {
                Room top = openRoomsContains(tempTop);
                room.setRoomTop(top);
                setAsBottomNeighborTo(room, top);
                setRoomDoors(top);
            }
        }
        else if (room.size == OnexTwo)
        {
            Vector2 tempBottomLeft = room.getLeft() + Vector2.down;
            Vector2 tempBottomRight = room.getRight() + Vector2.down;
            Vector2 tempLeft = room.getLeft() + Vector2.left;
            Vector2 tempRight = room.getRight() + Vector2.right;
            Vector2 tempTopLeft = room.getLeft() + Vector2.up;
            Vector2 tempTopRight = room.getRight() + Vector2.up;

            if (takenPos.Contains(tempBottomLeft))
            {
                Room bottomLeft = openRoomsContains(tempBottomLeft);
                room.setRoomBottomLeft(bottomLeft);
                setAsTopNeighborTo(room, bottomLeft);
                setRoomDoors(bottomLeft);
            }
            if (takenPos.Contains(tempBottomRight))
            {
                Room bottomRight = openRoomsContains(tempBottomRight);
                room.setRoomBottomRight(bottomRight);
                setAsTopNeighborTo(room, bottomRight);
                setRoomDoors(bottomRight);
            }
            if (takenPos.Contains(tempLeft))
            {
                Room left = openRoomsContains(tempLeft);
                room.setRoomLeft(left);
                setAsRightNeighborTo(room, left);
                setRoomDoors(left);
            }
            if (takenPos.Contains(tempRight))
            {
                Room right = openRoomsContains(tempRight);
                room.setRoomRight(right);
                setAsLeftNeighborTo(room, right);
                setRoomDoors(right);
            }
            if (takenPos.Contains(tempTopLeft))
            {
                Room topLeft = openRoomsContains(tempTopLeft);
                room.setRoomTopLeft(topLeft);
                setAsBottomNeighborTo(room, topLeft);
                setRoomDoors(topLeft);
            }
            if (takenPos.Contains(tempTopRight))
            {
                Room topRight = openRoomsContains(tempTopRight);
                room.setRoomTopRight(topRight);
                setAsBottomNeighborTo(room, topRight);
                setRoomDoors(topRight);
            }
        }
        else if (room.size == TwoxOne)
        {
            Vector2 tempBottom = room.getBottom() + Vector2.down;
            Vector2 tempLeftBottom = room.getBottom() + Vector2.left;
            Vector2 tempLeftTop = room.getTop() + Vector2.left;
            Vector2 tempRightBottom = room.getBottom() + Vector2.right;
            Vector2 tempRightTop = room.getTop() + Vector2.right;
            Vector2 tempTop = room.getTop() + Vector2.up;

            if (takenPos.Contains(tempBottom))
            {
                Room bottom = openRoomsContains(tempBottom);
                room.setRoomBottom(bottom);
                setAsTopNeighborTo(room, bottom);
                setRoomDoors(bottom);
            }
            if (takenPos.Contains(tempLeftBottom))
            {
                Room leftBottom = openRoomsContains(tempLeftBottom);
                room.setRoomLeftBottom(leftBottom);
                setAsRightNeighborTo(room, leftBottom);
                setRoomDoors(leftBottom);
            }
            if (takenPos.Contains(tempLeftTop))
            {
                Room leftTop = openRoomsContains(tempLeftTop);
                room.setRoomLeftTop(leftTop);
                setAsRightNeighborTo(room, leftTop);
                setRoomDoors(leftTop);
            }
            if (takenPos.Contains(tempRightBottom))
            {
                Room rightBottom = openRoomsContains(tempRightBottom);
                room.setRoomRightBottom(rightBottom);
                setAsLeftNeighborTo(room, rightBottom);
                setRoomDoors(rightBottom);
            }
            if (takenPos.Contains(tempRightTop))
            {
                Room rightTop = openRoomsContains(tempRightTop);
                room.setRoomRightTop(rightTop);
                setAsLeftNeighborTo(room, rightTop);
                setRoomDoors(rightTop);
            }
            if (takenPos.Contains(tempTop))
            {
                Room top = openRoomsContains(tempTop);
                room.setRoomTop(top);
                setAsBottomNeighborTo(room, top);
                setRoomDoors(top);
            }
        }
        else if (room.size == TwoxTwo)
        {
            Vector2 tempBottomLeft = room.getBottomLeft() + Vector2.down;
            Vector2 tempBottomRight = room.getBottomRight() + Vector2.down;
            Vector2 tempLeftBottom = room.getLeftBottom() + Vector2.left;
            Vector2 tempLeftTop = room.getLeftTop() + Vector2.left;
            Vector2 tempRightBottom = room.getRightBottom() + Vector2.right;
            Vector2 tempRightTop = room.getRightTop() + Vector2.right;
            Vector2 tempTopLeft = room.getTopLeft() + Vector2.up;
            Vector2 tempTopRight = room.getTopRight() + Vector2.up;

            if (takenPos.Contains(tempBottomLeft))
            {
                Room bottomLeft = openRoomsContains(tempBottomLeft);
                room.setRoomBottomLeft(bottomLeft);
                setAsTopNeighborTo(room, bottomLeft);
                setRoomDoors(bottomLeft);
            }
            if (takenPos.Contains(tempBottomRight))
            {
                Room bottomRight = openRoomsContains(tempBottomRight);
                room.setRoomBottomRight(bottomRight);
                setAsTopNeighborTo(room, bottomRight);
                setRoomDoors(bottomRight);
            }
            if (takenPos.Contains(tempLeftBottom))
            {
                Room leftBottom = openRoomsContains(tempLeftBottom);
                room.setRoomLeftBottom(leftBottom);
                setAsRightNeighborTo(room, leftBottom);
                setRoomDoors(leftBottom);
            }
            if (takenPos.Contains(tempLeftTop))
            {
                Room leftTop = openRoomsContains(tempLeftTop);
                room.setRoomLeftTop(leftTop);
                setAsRightNeighborTo(room, leftTop);
                setRoomDoors(leftTop);
            }
            if (takenPos.Contains(tempRightBottom))
            {
                Room rightBottom = openRoomsContains(tempRightBottom);
                room.setRoomRightBottom(rightBottom);
                setAsLeftNeighborTo(room, rightBottom);
                setRoomDoors(rightBottom);
            }
            if (takenPos.Contains(tempRightTop))
            {
                Room rightTop = openRoomsContains(tempRightTop);
                room.setRoomRightTop(rightTop);
                setAsLeftNeighborTo(room, rightTop);
                setRoomDoors(rightTop);
            }
            if (takenPos.Contains(tempTopLeft))
            {
                Room topLeft = openRoomsContains(tempTopLeft);
                room.setRoomTopLeft(topLeft);
                setAsBottomNeighborTo(room, topLeft);
                setRoomDoors(topLeft);
            }
            if (takenPos.Contains(tempTopRight))
            {
                Room topRight = openRoomsContains(tempTopRight);
                room.setRoomTopRight(topRight);
                setAsBottomNeighborTo(room, topRight);
                setRoomDoors(topRight);
            }
        }
        else
        {
            Vector2 tempBottomLeft = room.getBottomLeft() + Vector2.down;
            Vector2 tempBottom = room.getBottom() + Vector2.down;
            Vector2 tempBottomRight = room.getBottomRight() + Vector2.down;
            Vector2 tempLeftBottom = room.getLeftBottom() + Vector2.left;
            Vector2 tempLeft = room.getLeft() + Vector2.left;
            Vector2 tempLeftTop = room.getLeftTop() + Vector2.left;
            Vector2 tempRightBottom = room.getRightBottom() + Vector2.right;
            Vector2 tempRight = room.getRight() + Vector2.right;
            Vector2 tempRightTop = room.getRightTop() + Vector2.right;
            Vector2 tempTopLeft = room.getTopLeft() + Vector2.up;
            Vector2 tempTop = room.getTop() + Vector2.up;
            Vector2 tempTopRight = room.getTopRight() + Vector2.up;

            if (takenPos.Contains(tempBottomLeft))
            {
                Room bottomLeft = openRoomsContains(tempBottomLeft);
                room.setRoomBottomLeft(bottomLeft);
                setAsTopNeighborTo(room, bottomLeft);
                setRoomDoors(bottomLeft);
            }
            if (takenPos.Contains(tempBottom))
            {
                Room bottom = openRoomsContains(tempBottom);
                room.setRoomBottom(bottom);
                setAsTopNeighborTo(room, bottom);
                setRoomDoors(bottom);
            }
            if (takenPos.Contains(tempBottomRight))
            {
                Room bottomRight = openRoomsContains(tempBottomRight);
                room.setRoomBottomRight(bottomRight);
                setAsTopNeighborTo(room, bottomRight);
                setRoomDoors(bottomRight);
            }
            if (takenPos.Contains(tempLeftBottom))
            {
                Room leftBottom = openRoomsContains(tempLeftBottom);
                room.setRoomLeftBottom(leftBottom);
                setAsRightNeighborTo(room, leftBottom);
                setRoomDoors(leftBottom);
            }
            if (takenPos.Contains(tempLeft))
            {
                Room left = openRoomsContains(tempLeft);
                room.setRoomLeft(left);
                setAsRightNeighborTo(room, left);
                setRoomDoors(left);
            }
            if (takenPos.Contains(tempLeftTop))
            {
                Room leftTop = openRoomsContains(tempLeftTop);
                room.setRoomLeftTop(leftTop);
                setAsRightNeighborTo(room, leftTop);
                setRoomDoors(leftTop);
            }
            if (takenPos.Contains(tempRightBottom))
            {
                Room rightBottom = openRoomsContains(tempRightBottom);
                room.setRoomRightBottom(rightBottom);
                setAsLeftNeighborTo(room, rightBottom);
                setRoomDoors(rightBottom);
            }
            if (takenPos.Contains(tempRight))
            {
                Room right = openRoomsContains(tempRight);
                room.setRoomRight(right);
                setAsLeftNeighborTo(room, right);
                setRoomDoors(right);
            }
            if (takenPos.Contains(tempRightTop))
            {
                Room rightTop = openRoomsContains(tempRightTop);
                room.setRoomRightTop(rightTop);
                setAsLeftNeighborTo(room, rightTop);
                setRoomDoors(rightTop);
            }
            if (takenPos.Contains(tempTopLeft))
            {
                Room topLeft = openRoomsContains(tempTopLeft);
                room.setRoomTopLeft(topLeft);
                setAsBottomNeighborTo(room, topLeft);
                setRoomDoors(topLeft);
            }
            if (takenPos.Contains(tempTop))
            {
                Room top = openRoomsContains(tempTop);
                room.setRoomTop(top);
                setAsBottomNeighborTo(room, top);
                setRoomDoors(top);
            }
            if (takenPos.Contains(tempTopRight))
            {
                Room topRight = openRoomsContains(tempTopRight);
                room.setRoomTopRight(topRight);
                setAsBottomNeighborTo(room, topRight);
                setRoomDoors(topRight);
            }
        }
    }

    private void setAsBottomNeighborTo(Room neighbor, Room room)
    {
        if (room.size == OnexOne || room.size == TwoxOne)
        {
            room.setRoomBottom(neighbor);
        }
        else if (room.size == OnexTwo)
        {
            if (neighbor.locations.Contains(room.getLeft() + Vector2.down))
            {
                room.setRoomBottomLeft(neighbor);
            }
            if (neighbor.locations.Contains(room.getRight() + Vector2.down))
            {
                room.setRoomBottomRight(neighbor);
            }
        }
        else if (room.size == TwoxTwo)
        {
            if (neighbor.locations.Contains(room.getBottomLeft() + Vector2.down))
            {
                room.setRoomBottomLeft(neighbor);
            }
            if (neighbor.locations.Contains(room.getBottomRight() + Vector2.down))
            {
                room.setRoomBottomRight(neighbor);
            }
        }
        else
        {
            if (neighbor.locations.Contains(room.getBottomLeft() + Vector2.down))
            {
                room.setRoomBottomLeft(neighbor);
            }
            if (neighbor.locations.Contains(room.getBottom() + Vector2.down))
            {
                room.setRoomBottom(neighbor);
            }
            if (neighbor.locations.Contains(room.getBottomRight() + Vector2.down))
            {
                room.setRoomBottomRight(neighbor);
            }
        }
    }

    private void setAsLeftNeighborTo(Room neighbor, Room room)
    {
        if (room.size == OnexOne || room.size == OnexTwo)
        {
            room.setRoomLeft(neighbor);
        }
        else if (room.size == TwoxOne)
        {
            if (neighbor.locations.Contains(room.getBottom() + Vector2.left))
            {
                room.setRoomLeftBottom(neighbor);
            }
            if (neighbor.locations.Contains(room.getTop() + Vector2.left))
            {
                room.setRoomLeftTop(neighbor);
            }
        }
        else if (room.size == TwoxTwo)
        {
            if (neighbor.locations.Contains(room.getBottomLeft() + Vector2.left))
            {
                room.setRoomLeftBottom(neighbor);
            }
            if (neighbor.locations.Contains(room.getTopLeft() + Vector2.left))
            {
                room.setRoomLeftTop(neighbor);
            }
        }
        else
        {
            if (neighbor.locations.Contains(room.getBottomLeft() + Vector2.left))
            {
                room.setRoomLeftBottom(neighbor);
            }
            if (neighbor.locations.Contains(room.getLeft() + Vector2.left))
            {
                room.setRoomLeft(neighbor);
            }
            if (neighbor.locations.Contains(room.getTopLeft() + Vector2.left))
            {
                room.setRoomLeftTop(neighbor);
            }
        }
    }

    private void setAsRightNeighborTo(Room neighbor, Room room)
    {
        if (room.size == OnexOne || room.size == OnexTwo)
        {
            room.setRoomRight(neighbor);
        }
        else if (room.size == TwoxOne)
        {
            if (neighbor.locations.Contains(room.getBottom() + Vector2.right))
            {
                room.setRoomRightBottom(neighbor);
            }
            if (neighbor.locations.Contains(room.getTop() + Vector2.right))
            {
                room.setRoomRightTop(neighbor);
            }
        }
        else if (room.size == TwoxTwo)
        {
            if (neighbor.locations.Contains(room.getBottomRight() + Vector2.right))
            {
                room.setRoomRightBottom(neighbor);
            }
            if (neighbor.locations.Contains(room.getTopRight() + Vector2.right))
            {
                room.setRoomRightTop(neighbor);
            }
        }
        else
        {
            if (neighbor.locations.Contains(room.getBottomRight() + Vector2.right))
            {
                room.setRoomRightBottom(neighbor);
            }
            if (neighbor.locations.Contains(room.getRight() + Vector2.right))
            {
                room.setRoomRight(neighbor);
            }
            if (neighbor.locations.Contains(room.getTopRight() + Vector2.right))
            {
                room.setRoomRightTop(neighbor);
            }
        }
    }

    private void setAsTopNeighborTo(Room neighbor, Room room)
    {
        if (room.size == OnexOne || room.size == TwoxOne)
        {
            room.setRoomTop(neighbor);
        }
        else if (room.size == OnexTwo)
        {
            if (neighbor.locations.Contains(room.getLeft() + Vector2.up))
            {
                room.setRoomTopLeft(neighbor);
            }
            if (neighbor.locations.Contains(room.getRight() + Vector2.up))
            {
                room.setRoomTopRight(neighbor);
            }
        }
        else if (room.size == TwoxTwo)
        {
            if (neighbor.locations.Contains(room.getTopLeft() + Vector2.up))
            {
                room.setRoomTopLeft(neighbor);
            }
            if (neighbor.locations.Contains(room.getTopRight() + Vector2.up))
            {
                room.setRoomTopRight(neighbor);
            }
        }
        else
        {
            if (neighbor.locations.Contains(room.getTopLeft() + Vector2.up))
            {
                room.setRoomTopLeft(neighbor);
            }
            if (neighbor.locations.Contains(room.getTop() + Vector2.up))
            {
                room.setRoomTop(neighbor);
            }
            if (neighbor.locations.Contains(room.getTopRight() + Vector2.up))
            {
                room.setRoomTopRight(neighbor);
            }
        }
    }

    private Room openRoomsContains(Vector2 vector)
    {
        for (int i = 0; i < openRooms.Count; i++)
        {
            if (openRooms[i].locations.Contains(vector))
            {
                return openRooms[i];
            }
        }
        return errorRoom;
    }

    //Removes rooms that do not have an opening neighboring position from the openRooms list
    private void removeNotOpenRooms(Room room)
    {
        if (room.size == OnexOne)
        {
            Room tempBottom = room.getRoomBottom();
            Room tempLeft = room.getRoomLeft();
            Room tempRight = room.getRoomRight();
            Room tempTop = room.getRoomTop();

            if (openRooms.Contains(tempBottom) && getNumNeighbors(tempBottom) >= tempBottom.maxNeighbors)
            {
                openRooms.Remove(tempBottom);
            }
            if (openRooms.Contains(tempLeft) && getNumNeighbors(tempLeft) >= tempLeft.maxNeighbors)
            {
                openRooms.Remove(tempLeft);
            }
            if (openRooms.Contains(tempRight) && getNumNeighbors(tempRight) >= tempRight.maxNeighbors)
            {
                openRooms.Remove(tempRight);
            }
            if (openRooms.Contains(tempTop) && getNumNeighbors(tempTop) >= tempTop.maxNeighbors)
            {
                openRooms.Remove(tempTop);
            }
        }
        else if (room.size == OnexTwo)
        {
            Room tempBottomLeft = room.getRoomBottomLeft();
            Room tempBottomRight = room.getRoomBottomRight();
            Room tempLeft = room.getRoomLeft();
            Room tempRight = room.getRoomRight();
            Room tempTopLeft = room.getRoomTopLeft();
            Room tempTopRight = room.getRoomTopRight();

            if (openRooms.Contains(tempBottomLeft) && getNumNeighbors(tempBottomLeft) >= tempBottomLeft.maxNeighbors)
            {
                openRooms.Remove(tempBottomLeft);
            }
            if (openRooms.Contains(tempBottomRight) && getNumNeighbors(tempBottomRight) >= tempBottomRight.maxNeighbors)
            {
                openRooms.Remove(tempBottomRight);
            }
            if (openRooms.Contains(tempLeft) && getNumNeighbors(tempLeft) >= tempLeft.maxNeighbors)
            {
                openRooms.Remove(tempLeft);
            }
            if (openRooms.Contains(tempRight) && getNumNeighbors(tempRight) >= tempRight.maxNeighbors)
            {
                openRooms.Remove(tempRight);
            }
            if (openRooms.Contains(tempTopLeft) && getNumNeighbors(tempTopLeft) >= tempTopLeft.maxNeighbors)
            {
                openRooms.Remove(tempTopLeft);
            }
            if (openRooms.Contains(tempTopRight) && getNumNeighbors(tempTopRight) >= tempTopRight.maxNeighbors)
            {
                openRooms.Remove(tempTopRight);
            }
        }
        else if (room.size == TwoxOne)
        {
            Room tempBottom = room.getRoomBottom();
            Room tempLeftBottom = room.getRoomLeftBottom();
            Room tempLeftTop = room.getRoomLeftTop();
            Room tempRightBottom = room.getRoomRightBottom();
            Room tempRightTop = room.getRoomRightTop();
            Room tempTop = room.getRoomTop();

            if (openRooms.Contains(tempBottom) && getNumNeighbors(tempBottom) >= tempBottom.maxNeighbors)
            {
                openRooms.Remove(tempBottom);
            }
            if (openRooms.Contains(tempLeftBottom) && getNumNeighbors(tempLeftBottom) >= tempLeftBottom.maxNeighbors)
            {
                openRooms.Remove(tempLeftBottom);
            }
            if (openRooms.Contains(tempLeftTop) && getNumNeighbors(tempLeftTop) >= tempLeftTop.maxNeighbors)
            {
                openRooms.Remove(tempLeftTop);
            }
            if (openRooms.Contains(tempRightBottom) && getNumNeighbors(tempRightBottom) >= tempRightBottom.maxNeighbors)
            {
                openRooms.Remove(tempRightBottom);
            }
            if (openRooms.Contains(tempRightTop) && getNumNeighbors(tempRightTop) >= tempRightTop.maxNeighbors)
            {
                openRooms.Remove(tempRightTop);
            }
            if (openRooms.Contains(tempTop) && getNumNeighbors(tempTop) >= tempTop.maxNeighbors)
            {
                openRooms.Remove(tempTop);
            }
        }
        else if (room.size == TwoxTwo)
        {
            Room tempBottomLeft = room.getRoomBottomLeft();
            Room tempBottomRight = room.getRoomBottomRight();
            Room tempLeftBottom = room.getRoomLeftBottom();
            Room tempLeftTop = room.getRoomLeftTop();
            Room tempRightBottom = room.getRoomRightBottom();
            Room tempRightTop = room.getRoomRightTop();
            Room tempTopLeft = room.getRoomTopLeft();
            Room tempTopRight = room.getRoomTopRight();

            if (openRooms.Contains(tempBottomLeft) && getNumNeighbors(tempBottomLeft) >= tempBottomLeft.maxNeighbors)
            {
                openRooms.Remove(tempBottomLeft);
            }
            if (openRooms.Contains(tempBottomRight) && getNumNeighbors(tempBottomRight) >= tempBottomRight.maxNeighbors)
            {
                openRooms.Remove(tempBottomRight);
            }
            if (openRooms.Contains(tempLeftBottom) && getNumNeighbors(tempLeftBottom) >= tempLeftBottom.maxNeighbors)
            {
                openRooms.Remove(tempLeftBottom);
            }
            if (openRooms.Contains(tempLeftTop) && getNumNeighbors(tempLeftTop) >= tempLeftTop.maxNeighbors)
            {
                openRooms.Remove(tempLeftTop);
            }
            if (openRooms.Contains(tempRightBottom) && getNumNeighbors(tempRightBottom) >= tempRightBottom.maxNeighbors)
            {
                openRooms.Remove(tempRightBottom);
            }
            if (openRooms.Contains(tempRightTop) && getNumNeighbors(tempRightTop) >= tempRightTop.maxNeighbors)
            {
                openRooms.Remove(tempRightTop);
            }
            if (openRooms.Contains(tempTopLeft) && getNumNeighbors(tempTopLeft) >= tempTopLeft.maxNeighbors)
            {
                openRooms.Remove(tempTopLeft);
            }
            if (openRooms.Contains(tempTopRight) && getNumNeighbors(tempTopRight) >= tempTopRight.maxNeighbors)
            {
                openRooms.Remove(tempTopRight);
            }
        }
        else
        {
            Room tempBottomLeft = room.getRoomBottomLeft();
            Room tempBottom = room.getRoomBottom();
            Room tempBottomRight = room.getRoomBottomRight();
            Room tempLeftBottom = room.getRoomLeftBottom();
            Room tempLeft = room.getRoomLeft();
            Room tempLeftTop = room.getRoomLeftTop();
            Room tempRightBottom = room.getRoomRightBottom();
            Room tempRight = room.getRoomRight();
            Room tempRightTop = room.getRoomRightTop();
            Room tempTopLeft = room.getRoomTopLeft();
            Room tempTop = room.getRoomTop();
            Room tempTopRight = room.getRoomTopRight();

            if (openRooms.Contains(tempBottomLeft) && getNumNeighbors(tempBottomLeft) >= tempBottomLeft.maxNeighbors)
            {
                openRooms.Remove(tempBottomLeft);
            }
            if (openRooms.Contains(tempBottom) && getNumNeighbors(tempBottom) >= tempBottom.maxNeighbors)
            {
                openRooms.Remove(tempBottom);
            }
            if (openRooms.Contains(tempBottomRight) && getNumNeighbors(tempBottomRight) >= tempBottomRight.maxNeighbors)
            {
                openRooms.Remove(tempBottomRight);
            }
            if (openRooms.Contains(tempLeftBottom) && getNumNeighbors(tempLeftBottom) >= tempLeftBottom.maxNeighbors)
            {
                openRooms.Remove(tempLeftBottom);
            }
            if (openRooms.Contains(tempLeft) && getNumNeighbors(tempLeft) >= tempLeft.maxNeighbors)
            {
                openRooms.Remove(tempLeft);
            }
            if (openRooms.Contains(tempLeftTop) && getNumNeighbors(tempLeftTop) >= tempLeftTop.maxNeighbors)
            {
                openRooms.Remove(tempLeftTop);
            }
            if (openRooms.Contains(tempRightBottom) && getNumNeighbors(tempRightBottom) >= tempRightBottom.maxNeighbors)
            {
                openRooms.Remove(tempRightBottom);
            }
            if (openRooms.Contains(tempRight) && getNumNeighbors(tempRight) >= tempRight.maxNeighbors)
            {
                openRooms.Remove(tempRight);
            }
            if (openRooms.Contains(tempRightTop) && getNumNeighbors(tempRightTop) >= tempRightTop.maxNeighbors)
            {
                openRooms.Remove(tempRightTop);
            }
            if (openRooms.Contains(tempTopLeft) && getNumNeighbors(tempTopLeft) >= tempTopLeft.maxNeighbors)
            {
                openRooms.Remove(tempTopLeft);
            }
            if (openRooms.Contains(tempTop) && getNumNeighbors(tempTop) >= tempTop.maxNeighbors)
            {
                openRooms.Remove(tempTop);
            }
            if (openRooms.Contains(tempTopRight) && getNumNeighbors(tempTopRight) >= tempTopRight.maxNeighbors)
            {
                openRooms.Remove(tempTopRight);
            }
        }

        openRooms = openRooms.Distinct().ToList();
    }

    //Removes rooms that do not have at most one neighboring position from the singleNeighborRooms list
    private void removeNotSingleNeighborRooms(Room room)
    {
        if (room.size == OnexOne)
        {
            Room tempBottom = room.getRoomBottom();
            Room tempLeft = room.getRoomLeft();
            Room tempRight = room.getRoomRight();
            Room tempTop = room.getRoomTop();

            if (singleNeighborRooms.Contains(tempBottom) && getNumUniqueNeighbors(tempBottom) > 1)
            {
                singleNeighborRooms.Remove(tempBottom);
            }
            if (singleNeighborRooms.Contains(tempLeft) && getNumUniqueNeighbors(tempLeft) > 1)
            {
                singleNeighborRooms.Remove(tempLeft);
            }
            if (singleNeighborRooms.Contains(tempRight) && getNumUniqueNeighbors(tempRight) > 1)
            {
                singleNeighborRooms.Remove(tempRight);
            }
            if (singleNeighborRooms.Contains(tempTop) && getNumUniqueNeighbors(tempTop) > 1)
            {
                singleNeighborRooms.Remove(tempTop);
            }
        }
        else if (room.size == OnexTwo)
        {
            Room tempBottomLeft = room.getRoomBottomLeft();
            Room tempBottomRight = room.getRoomBottomRight();
            Room tempLeft = room.getRoomLeft();
            Room tempRight = room.getRoomRight();
            Room tempTopLeft = room.getRoomTopLeft();
            Room tempTopRight = room.getRoomTopRight();

            if (singleNeighborRooms.Contains(tempBottomLeft) && getNumUniqueNeighbors(tempBottomLeft) > 1)
            {
                singleNeighborRooms.Remove(tempBottomLeft);
            }
            if (singleNeighborRooms.Contains(tempBottomRight) && getNumUniqueNeighbors(tempBottomRight) > 1)
            {
                singleNeighborRooms.Remove(tempBottomRight);
            }
            if (singleNeighborRooms.Contains(tempLeft) && getNumUniqueNeighbors(tempLeft) > 1)
            {
                singleNeighborRooms.Remove(tempLeft);
            }
            if (singleNeighborRooms.Contains(tempRight) && getNumUniqueNeighbors(tempRight) > 1)
            {
                singleNeighborRooms.Remove(tempRight);
            }
            if (singleNeighborRooms.Contains(tempTopLeft) && getNumUniqueNeighbors(tempTopLeft) > 1)
            {
                singleNeighborRooms.Remove(tempTopLeft);
            }
            if (singleNeighborRooms.Contains(tempTopRight) && getNumUniqueNeighbors(tempTopRight) > 1)
            {
                singleNeighborRooms.Remove(tempTopRight);
            }
        }
        else if (room.size == TwoxOne)
        {
            Room tempBottom = room.getRoomBottom();
            Room tempLeftBottom = room.getRoomLeftBottom();
            Room tempLeftTop = room.getRoomLeftTop();
            Room tempRightBottom = room.getRoomRightBottom();
            Room tempRightTop = room.getRoomRightTop();
            Room tempTop = room.getRoomTop();

            if (singleNeighborRooms.Contains(tempBottom) && getNumUniqueNeighbors(tempBottom) > 1)
            {
                singleNeighborRooms.Remove(tempBottom);
            }
            if (singleNeighborRooms.Contains(tempLeftBottom) && getNumUniqueNeighbors(tempLeftBottom) > 1)
            {
                singleNeighborRooms.Remove(tempLeftBottom);
            }
            if (singleNeighborRooms.Contains(tempLeftTop) && getNumUniqueNeighbors(tempLeftTop) > 1)
            {
                singleNeighborRooms.Remove(tempLeftTop);
            }
            if (singleNeighborRooms.Contains(tempRightBottom) && getNumUniqueNeighbors(tempRightBottom) > 1)
            {
                singleNeighborRooms.Remove(tempRightBottom);
            }
            if (singleNeighborRooms.Contains(tempRightTop) && getNumUniqueNeighbors(tempRightTop) > 1)
            {
                singleNeighborRooms.Remove(tempRightTop);
            }
            if (singleNeighborRooms.Contains(tempTop) && getNumUniqueNeighbors(tempTop) > 1)
            {
                singleNeighborRooms.Remove(tempTop);
            }
        }
        else if (room.size == TwoxTwo)
        {
            Room tempBottomLeft = room.getRoomBottomLeft();
            Room tempBottomRight = room.getRoomBottomRight();
            Room tempLeftBottom = room.getRoomLeftBottom();
            Room tempLeftTop = room.getRoomLeftTop();
            Room tempRightBottom = room.getRoomRightBottom();
            Room tempRightTop = room.getRoomRightTop();
            Room tempTopLeft = room.getRoomTopLeft();
            Room tempTopRight = room.getRoomTopRight();

            if (singleNeighborRooms.Contains(tempBottomLeft) && getNumUniqueNeighbors(tempBottomLeft) > 1)
            {
                singleNeighborRooms.Remove(tempBottomLeft);
            }
            if (singleNeighborRooms.Contains(tempBottomRight) && getNumUniqueNeighbors(tempBottomRight) > 1)
            {
                singleNeighborRooms.Remove(tempBottomRight);
            }
            if (singleNeighborRooms.Contains(tempLeftBottom) && getNumUniqueNeighbors(tempLeftBottom) > 1)
            {
                singleNeighborRooms.Remove(tempLeftBottom);
            }
            if (singleNeighborRooms.Contains(tempLeftTop) && getNumUniqueNeighbors(tempLeftTop) > 1)
            {
                singleNeighborRooms.Remove(tempLeftTop);
            }
            if (singleNeighborRooms.Contains(tempRightBottom) && getNumUniqueNeighbors(tempRightBottom) > 1)
            {
                singleNeighborRooms.Remove(tempRightBottom);
            }
            if (singleNeighborRooms.Contains(tempRightTop) && getNumUniqueNeighbors(tempRightTop) > 1)
            {
                singleNeighborRooms.Remove(tempRightTop);
            }
            if (singleNeighborRooms.Contains(tempTopLeft) && getNumUniqueNeighbors(tempTopLeft) > 1)
            {
                singleNeighborRooms.Remove(tempTopLeft);
            }
            if (singleNeighborRooms.Contains(tempTopRight) && getNumUniqueNeighbors(tempTopRight) > 1)
            {
                singleNeighborRooms.Remove(tempTopRight);
            }
        }
        else
        {
            Room tempBottomLeft = room.getRoomBottomLeft();
            Room tempBottom = room.getRoomBottom();
            Room tempBottomRight = room.getRoomBottomRight();
            Room tempLeftBottom = room.getRoomLeftBottom();
            Room tempLeft = room.getRoomLeft();
            Room tempLeftTop = room.getRoomLeftTop();
            Room tempRightBottom = room.getRoomRightBottom();
            Room tempRight = room.getRoomRight();
            Room tempRightTop = room.getRoomRightTop();
            Room tempTopLeft = room.getRoomTopLeft();
            Room tempTop = room.getRoomTop();
            Room tempTopRight = room.getRoomTopRight();

            if (singleNeighborRooms.Contains(tempBottomLeft) && getNumUniqueNeighbors(tempBottomLeft) > 1)
            {
                singleNeighborRooms.Remove(tempBottomLeft);
            }
            if (singleNeighborRooms.Contains(tempBottom) && getNumUniqueNeighbors(tempBottom) > 1)
            {
                singleNeighborRooms.Remove(tempBottom);
            }
            if (singleNeighborRooms.Contains(tempBottomRight) && getNumUniqueNeighbors(tempBottomRight) > 1)
            {
                singleNeighborRooms.Remove(tempBottomRight);
            }
            if (singleNeighborRooms.Contains(tempLeftBottom) && getNumUniqueNeighbors(tempLeftBottom) > 1)
            {
                singleNeighborRooms.Remove(tempLeftBottom);
            }
            if (singleNeighborRooms.Contains(tempLeft) && getNumUniqueNeighbors(tempLeft) > 1)
            {
                singleNeighborRooms.Remove(tempLeft);
            }
            if (singleNeighborRooms.Contains(tempLeftTop) && getNumUniqueNeighbors(tempLeftTop) > 1)
            {
                singleNeighborRooms.Remove(tempLeftTop);
            }
            if (singleNeighborRooms.Contains(tempRightBottom) && getNumUniqueNeighbors(tempRightBottom) > 1)
            {
                singleNeighborRooms.Remove(tempRightBottom);
            }
            if (singleNeighborRooms.Contains(tempRight) && getNumUniqueNeighbors(tempRight) > 1)
            {
                singleNeighborRooms.Remove(tempRight);
            }
            if (singleNeighborRooms.Contains(tempRightTop) && getNumUniqueNeighbors(tempRightTop) > 1)
            {
                singleNeighborRooms.Remove(tempRightTop);
            }
            if (singleNeighborRooms.Contains(tempTopLeft) && getNumUniqueNeighbors(tempTopLeft) > 1)
            {
                singleNeighborRooms.Remove(tempTopLeft);
            }
            if (singleNeighborRooms.Contains(tempTop) && getNumUniqueNeighbors(tempTop) > 1)
            {
                singleNeighborRooms.Remove(tempTop);
            }
            if (singleNeighborRooms.Contains(tempTopRight) && getNumUniqueNeighbors(tempTopRight) > 1)
            {
                singleNeighborRooms.Remove(tempTopRight);
            }
        }

        singleNeighborRooms = singleNeighborRooms.Distinct().ToList();
    }

    //Gets a random position that's adjacent to a random room
    private Vector2 getRandomRoomPosition(Vector2 newRoomSize)
    {
        if (openRooms.Count == 0)
        {
            throw new System.Exception("There are no open rooms!");
        }

        Vector2 randomPos;
        bool validRandomPos;
        int index;
        int newRoomIndex;

        do
        {
            //Pick a random room that's already in the grid that doesn't have four neighbors
            index = Mathf.Clamp(Mathf.RoundToInt(Random.value * (openRooms.Count)), 0, openRooms.Count - 1);
            index = Mathf.Clamp(index, 0, openRooms.Count);

            Room randomRoom = openRooms[index];
            int x = (int)openRooms[index].topLeftInnerLocation.x;
            int y = (int)openRooms[index].topLeftInnerLocation.y;
            List<Vector2> openNeighboringPositions = getOpenNeighboringPositions(newRoomSize, randomRoom);

            if (openNeighboringPositions.Count == 0)
            {
                return errorVector;
            }

            newRoomIndex = Mathf.Clamp(Mathf.RoundToInt(Random.value * (openNeighboringPositions.Count)), 0, openNeighboringPositions.Count - 1);
            newRoomIndex = Mathf.Clamp(newRoomIndex, 0, openNeighboringPositions.Count);

            randomPos = openNeighboringPositions[newRoomIndex];
            Room newRoom = new Room(randomPos);

            //If this new location does not meet location requirements
            if (takenPosContainsAny(newRoom.locations)
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

    private void addCycles()
    {
        int rayLength = numOfRoomsInitial / 8;
        int minLength = 2;

        //Debug.Log("SingleNeighborRooms: " + singleNeighborRooms.Count);
        List<Room> initialSingleNeighborRooms = singleNeighborRooms.ToList();
        foreach (Room singleNeighborRoom in initialSingleNeighborRooms)
        {
            if (getNumUniqueNeighbors(singleNeighborRoom) > 1)
            {
                continue;
            }

            List<Room> twoNeighborRoomList = new List<Room>();
            twoNeighborRoomList.Add(singleNeighborRoom);

            Room previousRoom;
            Room currentRoom = singleNeighborRoom;
            List<Room> current = singleNeighborRoom.getNeighboringRooms();
            bool end = false;

            while (!end)
            {
                previousRoom = currentRoom;
                currentRoom = current[0];
                current.Clear();

                current = currentRoom.getNeighboringRooms();
                current.Remove(previousRoom);

                if (current.Count == 1 && !twoNeighborRoomList.Contains(currentRoom))
                {
                    twoNeighborRoomList.Add(currentRoom);
                }
                else
                {
                    end = true;
                }
            }

            if (twoNeighborRoomList.Count < minLength)
            {
                continue;
            }

            bool added = false;

            for (int i = 0; i < twoNeighborRoomList.Count; i++)
            {
                Room twoNeighborRoom = twoNeighborRoomList[i];
                List<Room> roomsToAdd = new List<Room>();

                if (twoNeighborRoom.size == OnexOne)
                {
                    bool goBottomDown = !hasBottomNeighbor(twoNeighborRoom);
                    bool goLeftLeft = !hasLeftNeighbor(twoNeighborRoom);
                    bool goRightRight = !hasRightNeighbor(twoNeighborRoom);
                    bool goTopUp = !hasTopNeighbor(twoNeighborRoom);

                    if (goBottomDown && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getBottomNeighborPosition(twoNeighborRoom) + (r * Vector2.down);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getBottomNeighborPosition(twoNeighborRoom) + (j * Vector2.down)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }
                    
                    if (goLeftLeft && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getLeftNeighborPosition(twoNeighborRoom) + (r * Vector2.left);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getLeftNeighborPosition(twoNeighborRoom) + (j * Vector2.left)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goRightRight && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getRightNeighborPosition(twoNeighborRoom) + (r * Vector2.right);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getRightNeighborPosition(twoNeighborRoom) + (j * Vector2.right)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goTopUp && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getTopNeighborPosition(twoNeighborRoom) + (r * Vector2.up);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getTopNeighborPosition(twoNeighborRoom) + (j * Vector2.up)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (twoNeighborRoom.size == OnexTwo)
                {
                    bool goBottomLeftDown = !hasBottomLeftNeighbor(twoNeighborRoom);
                    bool goBottomRightDown = !hasBottomRightNeighbor(twoNeighborRoom);
                    bool goLeftLeft = !hasLeftNeighbor(twoNeighborRoom);
                    bool goRightRight = !hasRightNeighbor(twoNeighborRoom);
                    bool goTopLeftUp = !hasTopLeftNeighbor(twoNeighborRoom);
                    bool goTopRightUp = !hasTopRightNeighbor(twoNeighborRoom);

                    if (goBottomLeftDown && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getBottomLeftNeighborPosition(twoNeighborRoom) + (r * Vector2.down);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getBottomLeftNeighborPosition(twoNeighborRoom) + (j * Vector2.down)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goBottomRightDown && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getBottomRightNeighborPosition(twoNeighborRoom) + (r * Vector2.down);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getBottomRightNeighborPosition(twoNeighborRoom) + (j * Vector2.down)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goLeftLeft && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getLeftNeighborPosition(twoNeighborRoom) + (r * Vector2.left);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getLeftNeighborPosition(twoNeighborRoom) + (j * Vector2.left)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goRightRight && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getRightNeighborPosition(twoNeighborRoom) + (r * Vector2.right);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getRightNeighborPosition(twoNeighborRoom) + (j * Vector2.right)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goTopLeftUp && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getTopLeftNeighborPosition(twoNeighborRoom) + (r * Vector2.up);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getTopLeftNeighborPosition(twoNeighborRoom) + (j * Vector2.up)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goTopRightUp && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getTopRightNeighborPosition(twoNeighborRoom) + (r * Vector2.up);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getTopRightNeighborPosition(twoNeighborRoom) + (j * Vector2.up)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (twoNeighborRoom.size == TwoxOne)
                {
                    bool goBottomDown = !hasBottomNeighbor(twoNeighborRoom);
                    bool goLeftBottomLeft = !hasLeftBottomNeighbor(twoNeighborRoom);
                    bool goLeftTopLeft = !hasLeftTopNeighbor(twoNeighborRoom);
                    bool goRightBottomRight = !hasRightBottomNeighbor(twoNeighborRoom);
                    bool goRightTopRight = !hasRightTopNeighbor(twoNeighborRoom);
                    bool goTopUp = !hasTopNeighbor(twoNeighborRoom);

                    if (goBottomDown && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getBottomNeighborPosition(twoNeighborRoom) + (r * Vector2.down);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getBottomNeighborPosition(twoNeighborRoom) + (j * Vector2.down)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goLeftBottomLeft && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getLeftBottomNeighborPosition(twoNeighborRoom) + (r * Vector2.left);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getLeftBottomNeighborPosition(twoNeighborRoom) + (j * Vector2.left)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goLeftTopLeft && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getLeftTopNeighborPosition(twoNeighborRoom) + (r * Vector2.left);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getLeftTopNeighborPosition(twoNeighborRoom) + (j * Vector2.left)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goRightBottomRight && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getRightBottomNeighborPosition(twoNeighborRoom) + (r * Vector2.right);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getRightBottomNeighborPosition(twoNeighborRoom) + (j * Vector2.right)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goRightTopRight && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getRightTopNeighborPosition(twoNeighborRoom) + (r * Vector2.right);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getRightTopNeighborPosition(twoNeighborRoom) + (j * Vector2.right)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goTopUp && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getTopNeighborPosition(twoNeighborRoom) + (r * Vector2.up);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getTopNeighborPosition(twoNeighborRoom) + (j * Vector2.up)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (twoNeighborRoom.size == TwoxTwo)
                {
                    bool goBottomLeftDown = !hasBottomLeftNeighbor(twoNeighborRoom);
                    bool goBottomRightDown = !hasBottomRightNeighbor(twoNeighborRoom);
                    bool goLeftBottomLeft = !hasLeftBottomNeighbor(twoNeighborRoom);
                    bool goLeftTopLeft = !hasLeftTopNeighbor(twoNeighborRoom);
                    bool goRightBottomRight = !hasRightBottomNeighbor(twoNeighborRoom);
                    bool goRightTopRight = !hasRightTopNeighbor(twoNeighborRoom);
                    bool goTopLeftUp = !hasTopLeftNeighbor(twoNeighborRoom);
                    bool goTopRightUp = !hasTopRightNeighbor(twoNeighborRoom);

                    if (goBottomLeftDown && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getBottomLeftNeighborPosition(twoNeighborRoom) + (r * Vector2.down);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getBottomLeftNeighborPosition(twoNeighborRoom) + (j * Vector2.down)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goBottomRightDown && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getBottomRightNeighborPosition(twoNeighborRoom) + (r * Vector2.down);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getBottomRightNeighborPosition(twoNeighborRoom) + (j * Vector2.down)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goLeftBottomLeft && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getLeftBottomNeighborPosition(twoNeighborRoom) + (r * Vector2.left);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getLeftBottomNeighborPosition(twoNeighborRoom) + (j * Vector2.left)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goLeftTopLeft && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getLeftTopNeighborPosition(twoNeighborRoom) + (r * Vector2.left);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getLeftTopNeighborPosition(twoNeighborRoom) + (j * Vector2.left)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goRightBottomRight && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getRightBottomNeighborPosition(twoNeighborRoom) + (r * Vector2.right);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getRightBottomNeighborPosition(twoNeighborRoom) + (j * Vector2.right)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goRightTopRight && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getRightTopNeighborPosition(twoNeighborRoom) + (r * Vector2.right);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getRightTopNeighborPosition(twoNeighborRoom) + (j * Vector2.right)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goTopLeftUp && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getTopLeftNeighborPosition(twoNeighborRoom) + (r * Vector2.up);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getTopLeftNeighborPosition(twoNeighborRoom) + (j * Vector2.up)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goTopRightUp && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getTopRightNeighborPosition(twoNeighborRoom) + (r * Vector2.up);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getTopRightNeighborPosition(twoNeighborRoom) + (j * Vector2.up)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    bool goBottomLeftDown = !hasBottomLeftNeighbor(twoNeighborRoom);
                    bool goBottomDown = !hasBottomNeighbor(twoNeighborRoom);
                    bool goBottomRightDown = !hasBottomRightNeighbor(twoNeighborRoom);
                    bool goLeftBottomLeft = !hasLeftBottomNeighbor(twoNeighborRoom);
                    bool goLeftLeft = !hasLeftNeighbor(twoNeighborRoom);
                    bool goLeftTopLeft = !hasLeftTopNeighbor(twoNeighborRoom);
                    bool goRightBottomRight = !hasRightBottomNeighbor(twoNeighborRoom);
                    bool goRightRight = !hasRightNeighbor(twoNeighborRoom);
                    bool goRightTopRight = !hasRightTopNeighbor(twoNeighborRoom);
                    bool goTopLeftUp = !hasTopLeftNeighbor(twoNeighborRoom);
                    bool goTopUp = !hasTopNeighbor(twoNeighborRoom);
                    bool goTopRightUp = !hasTopRightNeighbor(twoNeighborRoom);

                    if (goBottomLeftDown && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getBottomLeftNeighborPosition(twoNeighborRoom) + (r * Vector2.down);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getBottomLeftNeighborPosition(twoNeighborRoom) + (j * Vector2.down)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goBottomDown && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getBottomNeighborPosition(twoNeighborRoom) + (r * Vector2.down);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getBottomNeighborPosition(twoNeighborRoom) + (j * Vector2.down)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goBottomRightDown && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getBottomRightNeighborPosition(twoNeighborRoom) + (r * Vector2.down);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getBottomRightNeighborPosition(twoNeighborRoom) + (j * Vector2.down)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goLeftBottomLeft && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getLeftBottomNeighborPosition(twoNeighborRoom) + (r * Vector2.left);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getLeftBottomNeighborPosition(twoNeighborRoom) + (j * Vector2.left)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goLeftLeft && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getLeftNeighborPosition(twoNeighborRoom) + (r * Vector2.left);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getLeftNeighborPosition(twoNeighborRoom) + (j * Vector2.left)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goLeftTopLeft && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getLeftTopNeighborPosition(twoNeighborRoom) + (r * Vector2.left);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getLeftTopNeighborPosition(twoNeighborRoom) + (j * Vector2.left)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goRightBottomRight && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getRightBottomNeighborPosition(twoNeighborRoom) + (r * Vector2.right);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getRightBottomNeighborPosition(twoNeighborRoom) + (j * Vector2.right)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goRightRight && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getRightNeighborPosition(twoNeighborRoom) + (r * Vector2.right);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getRightNeighborPosition(twoNeighborRoom) + (j * Vector2.right)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goRightTopRight && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getRightTopNeighborPosition(twoNeighborRoom) + (r * Vector2.right);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getRightTopNeighborPosition(twoNeighborRoom) + (j * Vector2.right)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goTopLeftUp && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getTopLeftNeighborPosition(twoNeighborRoom) + (r * Vector2.up);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getTopLeftNeighborPosition(twoNeighborRoom) + (j * Vector2.up)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goTopUp && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getTopNeighborPosition(twoNeighborRoom) + (r * Vector2.up);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getTopNeighborPosition(twoNeighborRoom) + (j * Vector2.up)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (goTopRightUp && !added)
                    {
                        for (int r = 0; r <= rayLength; r++)
                        {
                            Vector2 tempLocation = getTopRightNeighborPosition(twoNeighborRoom) + (r * Vector2.up);

                            if (takenPos.Contains(tempLocation))
                            {
                                break;
                            }

                            Room tempRoom = new Room(tempLocation);

                            if (getNumUniqueNeighbors(tempRoom) >= 1)
                            {
                                List<Room> neighbors = getNeighbors(tempRoom);
                                foreach (Room neighbor in twoNeighborRoomList)
                                {
                                    if (neighbors.Contains(neighbor))
                                    {
                                        neighbors.Remove(neighbor);
                                    }
                                }

                                if (neighbors.Count >= 1)
                                {
                                    for (int j = 0; j <= r; j++)
                                    {
                                        roomsToAdd.Add(new Room(getTopRightNeighborPosition(twoNeighborRoom) + (j * Vector2.up)));
                                    }
                                    added = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                // Actually add the rooms to form a cycle
                foreach (Room addedRoom in roomsToAdd)
                {
                    Debug.Log("*CYCLE* Actually adding: " + addedRoom.center);
                    rooms.Add(addedRoom);

                    addLocationsToTakenPos(addedRoom);
                    setNeighboringRooms(addedRoom);
                    setRoomDoors(addedRoom);

                    if (getNumNeighbors(addedRoom) < addedRoom.maxNeighbors)
                    {
                        openRooms.Add(addedRoom);
                    }
                    if (getNumUniqueNeighbors(addedRoom) <= 1)
                    {
                        singleNeighborRooms.Add(addedRoom);
                    }

                    removeNotOpenRooms(addedRoom);
                    removeNotSingleNeighborRooms(addedRoom);
                }

                foreach (Room addedRoom in roomsToAdd)
                {
                    if (Random.value <= cycleBranchProb)
                    {
                        int iterations = 0;
                        Vector2 tempSize;
                        Vector2 tempLoc;

                        do
                        {
                            tempSize = getRoomSize();
                            tempLoc = getRandomCycleBranchRoomPosition(roomsToAdd, tempSize);
                            iterations++;
                        }
                        while (tempLoc == errorVector && iterations < roomsToAdd.Count * 2);

                        if (tempLoc == errorVector)
                        {
                            continue;
                        }

                        Room cycleBranchRoom = new Room(tempLoc, tempSize);
                        Debug.Log("*BRANCH* Actually adding: " + cycleBranchRoom.center);
                        rooms.Add(cycleBranchRoom);

                        addLocationsToTakenPos(cycleBranchRoom);
                        setNeighboringRooms(cycleBranchRoom);
                        setRoomDoors(cycleBranchRoom);

                        if (getNumNeighbors(cycleBranchRoom) < cycleBranchRoom.maxNeighbors)
                        {
                            openRooms.Add(cycleBranchRoom);
                        }
                        if (getNumUniqueNeighbors(cycleBranchRoom) <= 1)
                        {
                            singleNeighborRooms.Add(cycleBranchRoom);
                        }

                        removeNotOpenRooms(cycleBranchRoom);
                        removeNotSingleNeighborRooms(cycleBranchRoom);
                    }
                }
            }
        }
    }

    private List<Vector2> getOpenNeighboringPositions(Vector2 newRoomSize, Room randomRoom)
    {
        List<Vector2> openNeighboringPositions = new List<Vector2>();
        List<Vector2> tempNewRoomLocationsMM = new List<Vector2>();
        List<Vector2> tempNewRoomLocationsM = new List<Vector2>();
        List<Vector2> tempNewRoomLocationsZ = new List<Vector2>();
        List<Vector2> tempNewRoomLocationsP = new List<Vector2>();
        List<Vector2> tempNewRoomLocationsPP = new List<Vector2>();

        List<Vector2> offsets = new List<Vector2>();

        if (newRoomSize == OnexOne)
        {

            if (randomRoom.size == OnexOne)
            {
                if (!takenPos.Contains(getBottomNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getBottomNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getLeftNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getLeftNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getRightNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getRightNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getTopNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getTopNeighborPosition(randomRoom));
                }

                return openNeighboringPositions;
            }
            else if (randomRoom.size == OnexTwo)
            {
                if (!takenPos.Contains(getBottomLeftNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getBottomLeftNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getBottomRightNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getBottomRightNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getLeftNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getLeftNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getRightNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getRightNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getTopLeftNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getTopLeftNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getTopRightNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getTopRightNeighborPosition(randomRoom));
                }

                return openNeighboringPositions;
            }
            else if (randomRoom.size == TwoxOne)
            {
                if (!takenPos.Contains(getBottomNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getBottomNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getLeftBottomNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getLeftBottomNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getLeftTopNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getLeftTopNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getRightBottomNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getRightBottomNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getRightTopNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getRightTopNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getTopNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getTopNeighborPosition(randomRoom));
                }

                return openNeighboringPositions;
            }
            else if (randomRoom.size == TwoxTwo)
            {
                if (!takenPos.Contains(getBottomLeftNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getBottomLeftNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getBottomRightNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getBottomRightNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getLeftBottomNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getLeftBottomNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getLeftTopNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getLeftTopNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getRightBottomNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getRightBottomNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getRightTopNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getRightTopNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getTopLeftNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getTopLeftNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getTopRightNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getTopRightNeighborPosition(randomRoom));
                }

                return openNeighboringPositions;
            }
            else
            {
                if (!takenPos.Contains(getBottomLeftNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getBottomLeftNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getBottomNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getBottomNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getBottomRightNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getBottomRightNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getLeftBottomNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getLeftBottomNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getLeftNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getLeftNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getLeftTopNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getLeftTopNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getRightBottomNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getRightBottomNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getRightNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getRightNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getRightTopNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getRightTopNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getTopLeftNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getTopLeftNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getTopNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getTopNeighborPosition(randomRoom));
                }
                if (!takenPos.Contains(getTopRightNeighborPosition(randomRoom)))
                {
                    openNeighboringPositions.Add(getTopRightNeighborPosition(randomRoom));
                }

                return openNeighboringPositions;
            }
        }
        else if (newRoomSize == OnexTwo)
        {
            offsets.Add(new Vector2(-0.5f, 0f));
            offsets.Add(new Vector2(0.5f, 0f));

            if (randomRoom.size == OnexOne)
            {
                Vector2 bottomPosition = getBottomNeighborPosition(randomRoom) + new Vector2(-0.5f, 0f);
                Vector2 leftPosition = getLeftNeighborPosition(randomRoom) + new Vector2(-0.5f, 0f);
                Vector2 rightPosition = getRightNeighborPosition(randomRoom) + new Vector2(0.5f, 0f);
                Vector2 topPosition = getTopNeighborPosition(randomRoom) + new Vector2(-0.5f, 0f);

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(bottomPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(bottomPosition + offsets[i] + Vector2.right);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(bottomPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.right);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(leftPosition + offsets[i]);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(leftPosition);
                }

                tempNewRoomLocationsZ.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(rightPosition + offsets[i]);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(rightPosition);
                }

                tempNewRoomLocationsZ.Clear();
                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(topPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(topPosition + offsets[i] + Vector2.right);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(topPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.right);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                return openNeighboringPositions;
            }
            else if (randomRoom.size == OnexTwo)
            {
                Vector2 bottomPosition = getBottomLeftNeighborPosition(randomRoom) + new Vector2(-0.5f, 0f);
                Vector2 leftPosition = getLeftNeighborPosition(randomRoom) + new Vector2(-0.5f, 0f);
                Vector2 rightPosition = getRightNeighborPosition(randomRoom) + new Vector2(0.5f, 0f);
                Vector2 topPosition = getTopLeftNeighborPosition(randomRoom) + new Vector2(-0.5f, 0f);

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(bottomPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(bottomPosition + offsets[i] + Vector2.right);
                    tempNewRoomLocationsPP.Add(bottomPosition + offsets[i] + (2 * Vector2.right));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(bottomPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.right);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(bottomPosition + (2 * Vector2.right));
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(leftPosition + offsets[i]);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(leftPosition);
                }

                tempNewRoomLocationsZ.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(rightPosition + offsets[i]);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(rightPosition);
                }

                tempNewRoomLocationsZ.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(topPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(topPosition + offsets[i] + Vector2.right);
                    tempNewRoomLocationsPP.Add(topPosition + offsets[i] + (2 * Vector2.right));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(topPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.right);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(topPosition + (2 * Vector2.right));
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                return openNeighboringPositions;
            }
            else if (randomRoom.size == TwoxOne)
            {
                Vector2 bottomPosition = getBottomNeighborPosition(randomRoom) + new Vector2(-0.5f, 0f);
                Vector2 leftPosition = getLeftBottomNeighborPosition(randomRoom) + new Vector2(-0.5f, 0f);
                Vector2 rightPosition = getRightBottomNeighborPosition(randomRoom) + new Vector2(0.5f, 0f);
                Vector2 topPosition = getTopNeighborPosition(randomRoom) + new Vector2(-0.5f, 0f);

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(bottomPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(bottomPosition + offsets[i] + Vector2.right);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(bottomPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.right);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(leftPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(leftPosition + offsets[i] + Vector2.up);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(leftPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.up);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(rightPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(rightPosition + offsets[i] + Vector2.up);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(rightPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.up);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(topPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(topPosition + offsets[i] + Vector2.right);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(topPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.right);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                return openNeighboringPositions;
            }
            else if (randomRoom.size == TwoxTwo)
            {
                Vector2 bottomPosition = getBottomLeftNeighborPosition(randomRoom) + new Vector2(-0.5f, 0f);
                Vector2 leftPosition = getLeftBottomNeighborPosition(randomRoom) + new Vector2(-0.5f, 0f);
                Vector2 rightPosition = getRightBottomNeighborPosition(randomRoom) + new Vector2(0.5f, 0f);
                Vector2 topPosition = getTopLeftNeighborPosition(randomRoom) + new Vector2(-0.5f, 0f);

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(bottomPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(bottomPosition + offsets[i] + Vector2.right);
                    tempNewRoomLocationsPP.Add(bottomPosition + offsets[i] + (2 * Vector2.right));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(bottomPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.right);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(bottomPosition + (2 * Vector2.right));
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(leftPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(leftPosition + offsets[i] + Vector2.up);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(leftPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.up);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(rightPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(rightPosition + offsets[i] + Vector2.up);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(rightPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.up);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(topPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(topPosition + offsets[i] + Vector2.right);
                    tempNewRoomLocationsPP.Add(topPosition + offsets[i] + (2 * Vector2.right));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(topPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.right);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(topPosition + (2 * Vector2.right));
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                return openNeighboringPositions;
            }
            else
            {
                Vector2 bottomPosition = getBottomNeighborPosition(randomRoom) + new Vector2(-0.5f, 0f);
                Vector2 leftPosition = getLeftNeighborPosition(randomRoom) + new Vector2(-0.5f, 0f);
                Vector2 rightPosition = getRightNeighborPosition(randomRoom) + new Vector2(0.5f, 0f);
                Vector2 topPosition = getTopNeighborPosition(randomRoom) + new Vector2(-0.5f, 0f);

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(bottomPosition + offsets[i] + Vector2.left);
                    tempNewRoomLocationsZ.Add(bottomPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(bottomPosition + offsets[i] + Vector2.right);
                    tempNewRoomLocationsPP.Add(bottomPosition + offsets[i] + (2 * Vector2.right));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.left);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(bottomPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.right);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(bottomPosition + (2 * Vector2.right));
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(leftPosition + offsets[i] + Vector2.down);
                    tempNewRoomLocationsZ.Add(leftPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(leftPosition + offsets[i] + Vector2.up);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.down);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(leftPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.up);
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(rightPosition + offsets[i] + Vector2.down);
                    tempNewRoomLocationsZ.Add(rightPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(rightPosition + offsets[i] + Vector2.up);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.down);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(rightPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.up);
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(topPosition + offsets[i] + Vector2.left);
                    tempNewRoomLocationsZ.Add(topPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(topPosition + offsets[i] + Vector2.right);
                    tempNewRoomLocationsPP.Add(topPosition + offsets[i] + (2 * Vector2.right));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.left);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(topPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.right);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(topPosition + (2 * Vector2.right));
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                return openNeighboringPositions;
            }
        }
        else if (newRoomSize == TwoxOne)
        {
            offsets.Add(new Vector2(0f, -0.5f));
            offsets.Add(new Vector2(0f, 0.5f));

            if (randomRoom.size == OnexOne)
            {
                Vector2 bottomPosition = getBottomNeighborPosition(randomRoom) + new Vector2(0f, -0.5f);
                Vector2 leftPosition = getLeftNeighborPosition(randomRoom) + new Vector2(0f, -0.5f);
                Vector2 rightPosition = getRightNeighborPosition(randomRoom) + new Vector2(0f, -0.5f);
                Vector2 topPosition = getTopNeighborPosition(randomRoom) + new Vector2(0f, 0.5f);

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(bottomPosition + offsets[i]);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(bottomPosition);
                }

                tempNewRoomLocationsZ.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(leftPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(leftPosition + offsets[i] + Vector2.up);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(leftPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.up);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(rightPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(rightPosition + offsets[i] + Vector2.up);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(rightPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.up);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(topPosition + offsets[i]);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(topPosition);
                }

                tempNewRoomLocationsZ.Clear();

                return openNeighboringPositions;
            }
            else if (randomRoom.size == OnexTwo)
            {
                Vector2 bottomPosition = getBottomLeftNeighborPosition(randomRoom) + new Vector2(0f, -0.5f);
                Vector2 leftPosition = getLeftNeighborPosition(randomRoom) + new Vector2(0f, -0.5f);
                Vector2 rightPosition = getRightNeighborPosition(randomRoom) + new Vector2(0f, -0.5f);
                Vector2 topPosition = getTopLeftNeighborPosition(randomRoom) + new Vector2(0f, 0.5f);

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(bottomPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(bottomPosition + offsets[i] + Vector2.right);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(bottomPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.right);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(leftPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(leftPosition + offsets[i] + Vector2.up);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(leftPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.up);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(rightPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(rightPosition + offsets[i] + Vector2.up);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(rightPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.up);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(topPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(topPosition + offsets[i] + Vector2.right);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(topPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.right);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                return openNeighboringPositions;
            }
            else if (randomRoom.size == TwoxOne)
            {
                Vector2 bottomPosition = getBottomNeighborPosition(randomRoom) + new Vector2(0f, -0.5f);
                Vector2 leftPosition = getLeftBottomNeighborPosition(randomRoom) + new Vector2(0f, -0.5f);
                Vector2 rightPosition = getRightBottomNeighborPosition(randomRoom) + new Vector2(0f, -0.5f);
                Vector2 topPosition = getTopNeighborPosition(randomRoom) + new Vector2(0f, 0.5f);

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(bottomPosition + offsets[i]);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(bottomPosition);
                }

                tempNewRoomLocationsZ.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(leftPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(leftPosition + offsets[i] + Vector2.up);
                    tempNewRoomLocationsPP.Add(leftPosition + offsets[i] + (2 * Vector2.up));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(leftPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.up);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(leftPosition + (2 * Vector2.up));
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(rightPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(rightPosition + offsets[i] + Vector2.up);
                    tempNewRoomLocationsPP.Add(rightPosition + offsets[i] + (2 * Vector2.up));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(rightPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.up);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(rightPosition + (2 * Vector2.up));
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(topPosition + offsets[i]);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(topPosition);
                }

                tempNewRoomLocationsZ.Clear();

                return openNeighboringPositions;
            }
            else if (randomRoom.size == TwoxTwo)
            {
                Vector2 bottomPosition = getBottomLeftNeighborPosition(randomRoom) + new Vector2(0f, -0.5f);
                Vector2 leftPosition = getLeftBottomNeighborPosition(randomRoom) + new Vector2(0f, -0.5f);
                Vector2 rightPosition = getRightBottomNeighborPosition(randomRoom) + new Vector2(0f, -0.5f);
                Vector2 topPosition = getTopLeftNeighborPosition(randomRoom) + new Vector2(0f, 0.5f);

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(bottomPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(bottomPosition + offsets[i] + Vector2.right);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(bottomPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.right);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(leftPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(leftPosition + offsets[i] + Vector2.up);
                    tempNewRoomLocationsPP.Add(leftPosition + offsets[i] + (2 * Vector2.up));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(leftPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.up);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(leftPosition + (2 * Vector2.up));
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(rightPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(rightPosition + offsets[i] + Vector2.up);
                    tempNewRoomLocationsPP.Add(rightPosition + offsets[i] + (2 * Vector2.up));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(rightPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.up);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(rightPosition + (2 * Vector2.up));
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(topPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(topPosition + offsets[i] + Vector2.right);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(topPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.right);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                return openNeighboringPositions;
            }
            else
            {
                Vector2 bottomPosition = getBottomNeighborPosition(randomRoom) + new Vector2(0f, -0.5f);
                Vector2 leftPosition = getLeftNeighborPosition(randomRoom) + new Vector2(0f, -0.5f);
                Vector2 rightPosition = getRightNeighborPosition(randomRoom) + new Vector2(0f, -0.5f);
                Vector2 topPosition = getTopNeighborPosition(randomRoom) + new Vector2(0f, 0.5f);

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(bottomPosition + offsets[i] + Vector2.left);
                    tempNewRoomLocationsZ.Add(bottomPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(bottomPosition + offsets[i] + Vector2.right);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.left);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(bottomPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.right);
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(leftPosition + offsets[i] + Vector2.down);
                    tempNewRoomLocationsZ.Add(leftPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(leftPosition + offsets[i] + Vector2.up);
                    tempNewRoomLocationsPP.Add(leftPosition + offsets[i] + (2 * Vector2.up));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.down);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(leftPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.up);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(leftPosition + (2 * Vector2.up));
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(rightPosition + offsets[i] + Vector2.down);
                    tempNewRoomLocationsZ.Add(rightPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(rightPosition + offsets[i] + Vector2.up);
                    tempNewRoomLocationsPP.Add(rightPosition + offsets[i] + (2 * Vector2.up));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.down);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(rightPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.up);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(rightPosition + (2 * Vector2.up));
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(topPosition + offsets[i] + Vector2.left);
                    tempNewRoomLocationsZ.Add(topPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(topPosition + offsets[i] + Vector2.right);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.left);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(topPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.right);
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                return openNeighboringPositions;
            }
        }
        else if (newRoomSize == TwoxTwo)
        {
            offsets.Add(new Vector2(-0.5f, -0.5f));
            offsets.Add(new Vector2(0.5f, -0.5f));
            offsets.Add(new Vector2(-0.5f, 0.5f));
            offsets.Add(new Vector2(0.5f, 0.5f));

            if (randomRoom.size == OnexOne)
            {
                Vector2 bottomPosition = getBottomNeighborPosition(randomRoom) + new Vector2(-0.5f, -0.5f);
                Vector2 leftPosition = getLeftNeighborPosition(randomRoom) + new Vector2(-0.5f, -0.5f);
                Vector2 rightPosition = getRightNeighborPosition(randomRoom) + new Vector2(0.5f, -0.5f);
                Vector2 topPosition = getTopNeighborPosition(randomRoom) + new Vector2(-0.5f, 0.5f);

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(bottomPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(bottomPosition + offsets[i] + Vector2.right);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(bottomPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.right);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(leftPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(leftPosition + offsets[i] + Vector2.up);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(leftPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.up);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(rightPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(rightPosition + offsets[i] + Vector2.up);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(rightPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.up);
                }
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(topPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(topPosition + offsets[i] + Vector2.right);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(topPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.right);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                return openNeighboringPositions;
            }
            else if (randomRoom.size == OnexTwo)
            {
                Vector2 bottomPosition = getBottomLeftNeighborPosition(randomRoom) + new Vector2(-0.5f, -0.5f); 
                Vector2 leftPosition = getLeftNeighborPosition(randomRoom) + new Vector2(-0.5f, -0.5f);
                Vector2 rightPosition = getRightNeighborPosition(randomRoom) + new Vector2(0.5f, -0.5f);
                Vector2 topPosition = getTopLeftNeighborPosition(randomRoom) + new Vector2(-0.5f, 0.5f);

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(bottomPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(bottomPosition + offsets[i] + Vector2.right);
                    tempNewRoomLocationsPP.Add(bottomPosition + offsets[i] + (2 * Vector2.right));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(bottomPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.right);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(bottomPosition + (2 * Vector2.right));
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(leftPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(leftPosition + offsets[i] + Vector2.up);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(leftPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.up);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(rightPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(rightPosition + offsets[i] + Vector2.up);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(rightPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.up);
                }
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(topPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(topPosition + offsets[i] + Vector2.right);
                    tempNewRoomLocationsPP.Add(topPosition + offsets[i] + (2 * Vector2.right));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(topPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.right);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(topPosition + (2 * Vector2.right));
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                return openNeighboringPositions;
            }
            else if (randomRoom.size == TwoxOne)
            {
                Vector2 bottomPosition = getBottomNeighborPosition(randomRoom) + new Vector2(-0.5f, -0.5f); 
                Vector2 leftPosition = getLeftBottomNeighborPosition(randomRoom) + new Vector2(-0.5f, -0.5f);
                Vector2 rightPosition = getRightBottomNeighborPosition(randomRoom) + new Vector2(0.5f, -0.5f);
                Vector2 topPosition = getTopNeighborPosition(randomRoom) + new Vector2(-0.5f, 0.5f);

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(bottomPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(bottomPosition + offsets[i] + Vector2.right);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(bottomPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.right);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(leftPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(leftPosition + offsets[i] + Vector2.up);
                    tempNewRoomLocationsPP.Add(leftPosition + offsets[i] + (2 * Vector2.up));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(leftPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.up);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(leftPosition + (2 * Vector2.up));
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(rightPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(rightPosition + offsets[i] + Vector2.up);
                    tempNewRoomLocationsPP.Add(rightPosition + offsets[i] + (2 * Vector2.up));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(rightPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.up);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(rightPosition + (2 * Vector2.up));
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(topPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(topPosition + offsets[i] + Vector2.right);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(topPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.right);
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                return openNeighboringPositions;
            }
            else if (randomRoom.size == TwoxTwo)
            {
                Vector2 bottomPosition = getBottomLeftNeighborPosition(randomRoom) + new Vector2(-0.5f, -0.5f);
                Vector2 leftPosition = getLeftBottomNeighborPosition(randomRoom) + new Vector2(-0.5f, -0.5f);
                Vector2 rightPosition = getRightBottomNeighborPosition(randomRoom) + new Vector2(0.5f, -0.5f);
                Vector2 topPosition = getTopLeftNeighborPosition(randomRoom) + new Vector2(-0.5f, 0.5f);

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(bottomPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(bottomPosition + offsets[i] + Vector2.right);
                    tempNewRoomLocationsPP.Add(bottomPosition + offsets[i] + (2 * Vector2.right));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(bottomPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.right);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(bottomPosition + (2 * Vector2.right));
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(leftPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(leftPosition + offsets[i] + Vector2.up);
                    tempNewRoomLocationsPP.Add(leftPosition + offsets[i] + (2 * Vector2.up));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(leftPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.up);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(leftPosition + (2 * Vector2.up));
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(rightPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(rightPosition + offsets[i] + Vector2.up);
                    tempNewRoomLocationsPP.Add(rightPosition + offsets[i] + (2 * Vector2.up));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(rightPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.up);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(rightPosition + (2 * Vector2.up));
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsZ.Add(topPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(topPosition + offsets[i] + Vector2.right);
                    tempNewRoomLocationsPP.Add(topPosition + offsets[i] + (2 * Vector2.right));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(topPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.right);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(topPosition + (2 * Vector2.right));
                }

                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                return openNeighboringPositions;
            }
            else
            {
                Vector2 bottomPosition = getBottomNeighborPosition(randomRoom) + new Vector2(-0.5f, -0.5f);
                Vector2 leftPosition = getLeftNeighborPosition(randomRoom) + new Vector2(-0.5f, -0.5f);
                Vector2 rightPosition = getRightNeighborPosition(randomRoom) + new Vector2(0.5f, -0.5f);
                Vector2 topPosition = getTopNeighborPosition(randomRoom) + new Vector2(-0.5f, 0.5f);

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(bottomPosition + offsets[i] + Vector2.left);
                    tempNewRoomLocationsZ.Add(bottomPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(bottomPosition + offsets[i] + Vector2.right);
                    tempNewRoomLocationsPP.Add(bottomPosition + offsets[i] + (2 * Vector2.right));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.left);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(bottomPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.right);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(bottomPosition + (2 * Vector2.right));
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(leftPosition + offsets[i] + Vector2.down);
                    tempNewRoomLocationsZ.Add(leftPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(leftPosition + offsets[i] + Vector2.up);
                    tempNewRoomLocationsPP.Add(leftPosition + offsets[i] + (2 * Vector2.up));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.down);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(leftPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.up);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(leftPosition + (2 * Vector2.up));
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(rightPosition + offsets[i] + Vector2.down);
                    tempNewRoomLocationsZ.Add(rightPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(rightPosition + offsets[i] + Vector2.up);
                    tempNewRoomLocationsPP.Add(rightPosition + offsets[i] + (2 * Vector2.up));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.down);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(rightPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.up);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(rightPosition + (2 * Vector2.up));
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(topPosition + offsets[i] + Vector2.left);
                    tempNewRoomLocationsZ.Add(topPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(topPosition + offsets[i] + Vector2.right);
                    tempNewRoomLocationsPP.Add(topPosition + offsets[i] + (2 * Vector2.right));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.left);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(topPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.right);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(topPosition + (2 * Vector2.right));
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                return openNeighboringPositions;
            }
        }
        else
        {
            offsets.Add(new Vector2(-1f, -1f));
            offsets.Add(new Vector2(0f, -1f));
            offsets.Add(new Vector2(1f, -1f));
            offsets.Add(new Vector2(-1f, 0f));
            offsets.Add(new Vector2(0f, 0f));
            offsets.Add(new Vector2(1f, 0f));
            offsets.Add(new Vector2(-1f, 1f));
            offsets.Add(new Vector2(0f, 1f));
            offsets.Add(new Vector2(1f, 1f));

            if (randomRoom.size == OnexOne)
            {
                Vector2 bottomPosition = getBottomNeighborPosition(randomRoom) + Vector2.down;
                Vector2 leftPosition = getLeftNeighborPosition(randomRoom) + Vector2.left;
                Vector2 rightPosition = getRightNeighborPosition(randomRoom) + Vector2.right;
                Vector2 topPosition = getTopNeighborPosition(randomRoom) + Vector2.up;

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(bottomPosition + offsets[i] + Vector2.left);
                    tempNewRoomLocationsZ.Add(bottomPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(bottomPosition + offsets[i] + Vector2.right);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.left);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(bottomPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.right);
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(leftPosition + offsets[i] + Vector2.down);
                    tempNewRoomLocationsZ.Add(leftPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(leftPosition + offsets[i] + Vector2.up);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.down);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(leftPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.up);
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(rightPosition + offsets[i] + Vector2.down);
                    tempNewRoomLocationsZ.Add(rightPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(rightPosition + offsets[i] + Vector2.up);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.down);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(rightPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.up);
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(topPosition + offsets[i] + Vector2.left);
                    tempNewRoomLocationsZ.Add(topPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(topPosition + offsets[i] + Vector2.right);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.left);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(topPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.right);
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                return openNeighboringPositions;
            }
            else if (randomRoom.size == OnexTwo)
            {
                Vector2 bottomPosition = getBottomLeftNeighborPosition(randomRoom) + Vector2.down;
                Vector2 leftPosition = getLeftNeighborPosition(randomRoom) + Vector2.left;
                Vector2 rightPosition = getRightNeighborPosition(randomRoom) + Vector2.right;
                Vector2 topPosition = getTopLeftNeighborPosition(randomRoom) + Vector2.up;

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(bottomPosition + offsets[i] + Vector2.left);
                    tempNewRoomLocationsZ.Add(bottomPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(bottomPosition + offsets[i] + Vector2.right);
                    tempNewRoomLocationsPP.Add(bottomPosition + offsets[i] + (2 * Vector2.right));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.left);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(bottomPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.right);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(bottomPosition + (2 * Vector2.right));
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(leftPosition + offsets[i] + Vector2.down);
                    tempNewRoomLocationsZ.Add(leftPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(leftPosition + offsets[i] + Vector2.up);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.down);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(leftPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.up);
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(rightPosition + offsets[i] + Vector2.down);
                    tempNewRoomLocationsZ.Add(rightPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(rightPosition + offsets[i] + Vector2.up);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.down);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(rightPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.up);
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(topPosition + offsets[i] + Vector2.left);
                    tempNewRoomLocationsZ.Add(topPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(topPosition + offsets[i] + Vector2.right);
                    tempNewRoomLocationsPP.Add(topPosition + offsets[i] + (2 * Vector2.right));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.left);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(topPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.right);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(topPosition + (2 * Vector2.right));
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                return openNeighboringPositions;
            }
            else if (randomRoom.size == TwoxOne)
            {
                Vector2 bottomPosition = getBottomNeighborPosition(randomRoom) + Vector2.down;
                Vector2 leftPosition = getLeftBottomNeighborPosition(randomRoom) + Vector2.left;
                Vector2 rightPosition = getRightBottomNeighborPosition(randomRoom) + Vector2.right;
                Vector2 topPosition = getTopNeighborPosition(randomRoom) + Vector2.up;

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(bottomPosition + offsets[i] + Vector2.left);
                    tempNewRoomLocationsZ.Add(bottomPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(bottomPosition + offsets[i] + Vector2.right);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.left);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(bottomPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.right);
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(leftPosition + offsets[i] + Vector2.down);
                    tempNewRoomLocationsZ.Add(leftPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(leftPosition + offsets[i] + Vector2.up);
                    tempNewRoomLocationsPP.Add(leftPosition + offsets[i] + (2 * Vector2.up));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.down);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(leftPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.up);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(leftPosition + (2 * Vector2.up));
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(rightPosition + offsets[i] + Vector2.down);
                    tempNewRoomLocationsZ.Add(rightPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(rightPosition + offsets[i] + Vector2.up);
                    tempNewRoomLocationsPP.Add(rightPosition + offsets[i] + (2 * Vector2.up));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.down);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(rightPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.up);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(rightPosition + (2 * Vector2.up));
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(topPosition + offsets[i] + Vector2.left);
                    tempNewRoomLocationsZ.Add(topPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(topPosition + offsets[i] + Vector2.right);
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.left);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(topPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.right);
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();

                return openNeighboringPositions;
            }
            else if (randomRoom.size == TwoxTwo)
            {
                Vector2 bottomPosition = getBottomLeftNeighborPosition(randomRoom) + Vector2.down;
                Vector2 leftPosition = getLeftBottomNeighborPosition(randomRoom) + Vector2.left;
                Vector2 rightPosition = getRightBottomNeighborPosition(randomRoom) + Vector2.right;
                Vector2 topPosition = getTopLeftNeighborPosition(randomRoom) + Vector2.up;

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(bottomPosition + offsets[i] + Vector2.left);
                    tempNewRoomLocationsZ.Add(bottomPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(bottomPosition + offsets[i] + Vector2.right);
                    tempNewRoomLocationsPP.Add(bottomPosition + offsets[i] + (2 * Vector2.right));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.left);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(bottomPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.right);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(bottomPosition + (2 * Vector2.right));
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(leftPosition + offsets[i] + Vector2.down);
                    tempNewRoomLocationsZ.Add(leftPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(leftPosition + offsets[i] + Vector2.up);
                    tempNewRoomLocationsPP.Add(leftPosition + offsets[i] + (2 * Vector2.up));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.down);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(leftPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.up);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(leftPosition + (2 * Vector2.up));
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(rightPosition + offsets[i] + Vector2.down);
                    tempNewRoomLocationsZ.Add(rightPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(rightPosition + offsets[i] + Vector2.up);
                    tempNewRoomLocationsPP.Add(rightPosition + offsets[i] + (2 * Vector2.up));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.down);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(rightPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.up);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(rightPosition + (2 * Vector2.up));
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsM.Add(topPosition + offsets[i] + Vector2.left);
                    tempNewRoomLocationsZ.Add(topPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(topPosition + offsets[i] + Vector2.right);
                    tempNewRoomLocationsPP.Add(topPosition + offsets[i] + (2 * Vector2.right));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.left);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(topPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.right);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(topPosition + (2 * Vector2.right));
                }

                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                return openNeighboringPositions;
            }
            else
            {
                Vector2 bottomPosition = getBottomNeighborPosition(randomRoom) + Vector2.down;
                Vector2 leftPosition = getLeftNeighborPosition(randomRoom) + Vector2.left;
                Vector2 rightPosition = getRightNeighborPosition(randomRoom) + Vector2.right;
                Vector2 topPosition = getTopNeighborPosition(randomRoom) + Vector2.up;

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsMM.Add(bottomPosition + offsets[i] + (2 * Vector2.left));
                    tempNewRoomLocationsM.Add(bottomPosition + offsets[i] + Vector2.left);
                    tempNewRoomLocationsZ.Add(bottomPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(bottomPosition + offsets[i] + Vector2.right);
                    tempNewRoomLocationsPP.Add(bottomPosition + offsets[i] + (2 * Vector2.right));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsMM))
                {
                    openNeighboringPositions.Add(bottomPosition + (2 * Vector2.left));
                }
                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(bottomPosition +  Vector2.left);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(bottomPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(bottomPosition + Vector2.right);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(bottomPosition + (2 * Vector2.right));
                }

                tempNewRoomLocationsMM.Clear();
                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsMM.Add(leftPosition + offsets[i] + (2 * Vector2.down));
                    tempNewRoomLocationsM.Add(leftPosition + offsets[i] + Vector2.down);
                    tempNewRoomLocationsZ.Add(leftPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(leftPosition + offsets[i] + Vector2.up);
                    tempNewRoomLocationsPP.Add(leftPosition + offsets[i] + (2 * Vector2.up));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsMM))
                {
                    openNeighboringPositions.Add(leftPosition + (2 * Vector2.down));
                }
                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.down);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(leftPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(leftPosition + Vector2.up);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(leftPosition + (2 * Vector2.up));
                }

                tempNewRoomLocationsMM.Clear();
                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsMM.Add(rightPosition + offsets[i] + (2 * Vector2.down));
                    tempNewRoomLocationsM.Add(rightPosition + offsets[i] + Vector2.down);
                    tempNewRoomLocationsZ.Add(rightPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(rightPosition + offsets[i] + Vector2.up);
                    tempNewRoomLocationsPP.Add(rightPosition + offsets[i] + (2 * Vector2.up));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsMM))
                {
                    openNeighboringPositions.Add(rightPosition + (2 * Vector2.down));
                }
                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.down);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(rightPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(rightPosition + Vector2.up);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(rightPosition + (2 * Vector2.up));
                }

                tempNewRoomLocationsMM.Clear();
                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                for (int i = 0; i < offsets.Count; i++)
                {
                    tempNewRoomLocationsMM.Add(topPosition + offsets[i] + (2 * Vector2.left));
                    tempNewRoomLocationsM.Add(topPosition + offsets[i] + Vector2.left);
                    tempNewRoomLocationsZ.Add(topPosition + offsets[i]);
                    tempNewRoomLocationsP.Add(topPosition + offsets[i] + Vector2.right);
                    tempNewRoomLocationsPP.Add(topPosition + offsets[i] + (2 * Vector2.right));
                }

                if (!takenPosContainsAny(tempNewRoomLocationsMM))
                {
                    openNeighboringPositions.Add(topPosition + (2 * Vector2.left));
                }
                if (!takenPosContainsAny(tempNewRoomLocationsM))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.left);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsZ))
                {
                    openNeighboringPositions.Add(topPosition);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsP))
                {
                    openNeighboringPositions.Add(topPosition + Vector2.right);
                }
                if (!takenPosContainsAny(tempNewRoomLocationsPP))
                {
                    openNeighboringPositions.Add(topPosition + (2 * Vector2.right));
                }

                tempNewRoomLocationsMM.Clear();
                tempNewRoomLocationsM.Clear();
                tempNewRoomLocationsZ.Clear();
                tempNewRoomLocationsP.Clear();
                tempNewRoomLocationsPP.Clear();

                return openNeighboringPositions;
            }
        }
    }

    private bool takenPosContainsAny(List<Vector2> vectors)
    {
        for (int i = 0; i < vectors.Count; i++)
        {
            if (takenPos.Contains(vectors[i]))
            {
                return true;
            }
        }
        return false;
    }

    private Room findRoomAtLocation(Vector2 location)
    {
        foreach (Room room in rooms)
        {
            if (room.locations.Contains(location))
            {
                return room;
            }
        }

        return errorRoom;
    }

    //Gets a random position that's adjacent to only one random room (branching)
    private Vector2 getRandomBranchRoomPosition(Vector2 newRoomSize)
    {
        if (openRooms.Count == 0)
        {
            throw new System.Exception("There are no open rooms!");
        }

        if (singleNeighborRooms.Count == 0)
        {
            return errorVector;
        }

        Vector2 randomPos = errorVector;
        bool validRandomPos = true;
        int index;
        int newRoomIndex;
        int iterations = 0;

        do
        {
            //Pick a random room that's already in the grid that doesn't have four neighbors
            index = Mathf.Clamp(Mathf.RoundToInt(Random.value * (singleNeighborRooms.Count)), 0, singleNeighborRooms.Count - 1);
            index = Mathf.Clamp(index, 0, singleNeighborRooms.Count);

            Room randomRoom = singleNeighborRooms[index];
            int x = (int)singleNeighborRooms[index].topLeftInnerLocation.x;
            int y = (int)singleNeighborRooms[index].topLeftInnerLocation.y;
            List<Vector2> openNeighboringPositions = getOpenNeighboringPositions(newRoomSize, randomRoom);
            List<Vector2> singleOpenNeighboringPositions = getSingleOpenNeighboringPositions(openNeighboringPositions, newRoomSize);

            if (singleOpenNeighboringPositions.Count == 0)
            {
                if (iterations < 100)
                {
                    validRandomPos = false;
                    iterations++;
                }
                else
                {
                    return errorVector;
                }
            }
            else
            {
                newRoomIndex = Mathf.Clamp(Mathf.RoundToInt(Random.value * (singleOpenNeighboringPositions.Count)), 0, singleOpenNeighboringPositions.Count - 1);
                newRoomIndex = Mathf.Clamp(newRoomIndex, 0, singleOpenNeighboringPositions.Count);

                randomPos = singleOpenNeighboringPositions[newRoomIndex];
                Room newRoom = new Room(randomPos);

                //If this new location does not meet location requirements
                if (takenPosContainsAny(newRoom.locations)
                    || x >= areaSizeX
                    || x < 0
                    || y >= areaSizeY
                    || y < 0)
                {
                    iterations++;
                    validRandomPos = false;
                }
                else
                {
                    validRandomPos = true;
                }
            }
        }
        while (!validRandomPos);

        return randomPos;
    }

    //Gets a random position that's adjacent to only one random room (branching)
    private Vector2 getRandomCycleBranchRoomPosition(List<Room> cycleRooms, Vector2 newRoomSize)
    {
        if (openRooms.Count == 0)
        {
            throw new System.Exception("There are no open rooms!");
        }

        if (cycleRooms.Count == 0)
        {
            return errorVector;
        }

        Vector2 randomPos = errorVector;
        bool validRandomPos = true;
        int index;
        int newRoomIndex;
        int iterations = 0;

        do
        {
            //Pick a random room that's already in the grid that doesn't have four neighbors
            index = Mathf.Clamp(Mathf.RoundToInt(Random.value * (cycleRooms.Count)), 0, cycleRooms.Count - 1);
            index = Mathf.Clamp(index, 0, cycleRooms.Count);

            Room randomRoom = cycleRooms[index];
            int x = (int) cycleRooms[index].topLeftInnerLocation.x;
            int y = (int) cycleRooms[index].topLeftInnerLocation.y;
            //Here
            List<Vector2> openNeighboringPositions = getOpenNeighboringPositions(newRoomSize, randomRoom);

            if (openNeighboringPositions.Count == 0)
            {
                if (iterations < cycleRooms.Count * 2)
                {
                    validRandomPos = false;
                    iterations++;
                }
                else
                {
                    return errorVector;
                }
            }
            else
            {
                newRoomIndex = Mathf.Clamp(Mathf.RoundToInt(Random.value * (openNeighboringPositions.Count)), 0, openNeighboringPositions.Count - 1);
                newRoomIndex = Mathf.Clamp(newRoomIndex, 0, openNeighboringPositions.Count);

                randomPos = openNeighboringPositions[newRoomIndex];
                Room newRoom = new Room(randomPos);

                //If this new location does not meet location requirements
                if (takenPosContainsAny(newRoom.locations)
                    || x >= areaSizeX
                    || x < 0
                    || y >= areaSizeY
                    || y < 0)
                {
                    iterations++;
                    validRandomPos = false;
                }
                else
                {
                    validRandomPos = true;
                }
            }
        }
        while (!validRandomPos);

        return randomPos;
    }

    private List<Vector2> getSingleOpenNeighboringPositions(List<Vector2> openNeighboringPositions, Vector2 newRoomSize)
    {
        List<Vector2> tempSingleOpenNeighboringPositions = new List<Vector2>();
        if (newRoomSize == OnexOne)
        {
            for (int i = 0; i < openNeighboringPositions.Count; i++)
            {
                Room temp = new Room(openNeighboringPositions[i], newRoomSize);
                int neighbors = getNumUniqueNeighbors(temp);
                if (neighbors <= 1)
                {
                    tempSingleOpenNeighboringPositions.Add(openNeighboringPositions[i]);
                }
            }

            return tempSingleOpenNeighboringPositions;
        }
        else if (newRoomSize == OnexTwo)
        {
            for (int i = 0; i < openNeighboringPositions.Count; i++)
            {
                Room temp = new Room(openNeighboringPositions[i], newRoomSize);
                int neighbors = getNumUniqueNeighbors(temp);
                if (neighbors <= 1)
                {
                    tempSingleOpenNeighboringPositions.Add(openNeighboringPositions[i]);
                }
            }
            return tempSingleOpenNeighboringPositions;
        }
        else if (newRoomSize == TwoxOne)
        {
            for (int i = 0; i < openNeighboringPositions.Count; i++)
            {
                Room temp = new Room(openNeighboringPositions[i], newRoomSize);
                int neighbors = getNumUniqueNeighbors(temp);
                if (neighbors <= 1)
                {
                    tempSingleOpenNeighboringPositions.Add(openNeighboringPositions[i]);
                }
            }
            return tempSingleOpenNeighboringPositions;
        }
        else if (newRoomSize == TwoxTwo)
        {
            for (int i = 0; i < openNeighboringPositions.Count; i++)
            {
                Room temp = new Room(openNeighboringPositions[i], newRoomSize);
                int neighbors = getNumUniqueNeighbors(temp);
                if (neighbors <= 1)
                {
                    tempSingleOpenNeighboringPositions.Add(openNeighboringPositions[i]);
                }
            }
            return tempSingleOpenNeighboringPositions;
        }
        else
        {
            for (int i = 0; i < openNeighboringPositions.Count; i++)
            {
                Room temp = new Room(openNeighboringPositions[i], newRoomSize);
                int neighbors = getNumUniqueNeighbors(temp);
                if (neighbors <= 1)
                {
                    tempSingleOpenNeighboringPositions.Add(openNeighboringPositions[i]);
                }
            }
            return tempSingleOpenNeighboringPositions;
        }
    }

    private void setRoomDoors(Room room)
    {
        if (room.size == OnexOne)
        {
            room.setDoorBottom(!hasBottomNeighbor(room));
            room.setDoorLeft(!hasLeftNeighbor(room));
            room.setDoorRight(!hasRightNeighbor(room));
            room.setDoorTop(!hasTopNeighbor(room));
        }
        else if (room.size == OnexTwo)
        {
            bool doorBottomLeft = !hasBottomLeftNeighbor(room);
            bool doorBottomRight = !hasBottomRightNeighbor(room);
            bool doorTopLeft = !hasTopLeftNeighbor(room);
            bool doorTopRight = !hasTopRightNeighbor(room);

            if (!doorBottomLeft && !doorBottomRight && room.getRoomBottomLeft() == room.getRoomBottomRight())
            {
                Room bottomLeftRoom = room.getRoomBottomLeft();
                if (bottomLeftRoom.size == OnexTwo || bottomLeftRoom.size == TwoxTwo)
                {
                    if (bottomLeftRoom.getDoorTopLeft() != bottomLeftRoom.getDoorTopRight())
                    {
                        room.setDoorBottomLeft(bottomLeftRoom.getDoorTopLeft());
                        room.setDoorBottomRight(bottomLeftRoom.getDoorTopRight());
                    }
                    else
                    {
                        float random = Random.value;
                        if (random < 0.5f)
                        {
                            room.setDoorBottomLeft(false);
                            room.setDoorBottomRight(true);
                        }
                        else
                        {
                            room.setDoorBottomLeft(true);
                            room.setDoorBottomRight(false);
                        }
                    }
                }
                else if (bottomLeftRoom.size == ThreexThree)
                {
                    Room topLeftRoomOfBottomLeftRoom = bottomLeftRoom.getRoomTopLeft();
                    Room topRoomOfBottomLeftRoom = bottomLeftRoom.getRoomTop();
                    Room topRightRoomOfBottomLeftRoom = bottomLeftRoom.getRoomTopRight();

                    if (topLeftRoomOfBottomLeftRoom == topRoomOfBottomLeftRoom)
                    {
                        if (bottomLeftRoom.getDoorTopLeft() != bottomLeftRoom.getDoorTop())
                        {
                            room.setDoorBottomLeft(bottomLeftRoom.getDoorTopLeft());
                            room.setDoorBottomRight(bottomLeftRoom.getDoorTop());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorBottomLeft(false);
                                room.setDoorBottomRight(true);
                            }
                            else
                            {
                                room.setDoorBottomLeft(true);
                                room.setDoorBottomRight(false);
                            }
                        }
                    }
                    else if (topRoomOfBottomLeftRoom == topRightRoomOfBottomLeftRoom)
                    {
                        if (bottomLeftRoom.getDoorTop() != bottomLeftRoom.getDoorTopRight())
                        {
                            room.setDoorBottomLeft(bottomLeftRoom.getDoorTop());
                            room.setDoorBottomRight(bottomLeftRoom.getDoorTopRight());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorBottomLeft(false);
                                room.setDoorBottomRight(true);
                            }
                            else
                            {
                                room.setDoorBottomLeft(true);
                                room.setDoorBottomRight(false);
                            }
                        }
                    }
                }
            }
            else
            {
                room.setDoorBottomLeft(doorBottomLeft);
                room.setDoorBottomRight(doorBottomRight);
            }


            if (!doorTopLeft && !doorTopRight && room.getRoomTopLeft() == room.getRoomTopRight())
            {
                Room topLeftRoom = room.getRoomTopLeft();
                if (topLeftRoom.size == OnexTwo || topLeftRoom.size == TwoxTwo)
                {
                    if (topLeftRoom.getDoorBottomLeft() != topLeftRoom.getDoorBottomRight())
                    {
                        room.setDoorTopLeft(topLeftRoom.getDoorBottomLeft());
                        room.setDoorTopRight(topLeftRoom.getDoorBottomRight());
                    }
                    else
                    {
                        float random = Random.value;
                        if (random < 0.5f)
                        {
                            room.setDoorTopLeft(false);
                            room.setDoorTopRight(true);
                        }
                        else
                        {
                            room.setDoorTopLeft(true);
                            room.setDoorTopRight(false);
                        }
                    }
                }
                else if (topLeftRoom.size == ThreexThree)
                {
                    Room bottomLeftRoomOfTopLeftRoom = topLeftRoom.getRoomBottomLeft();
                    Room bottomRoomOfTopLeftRoom = topLeftRoom.getRoomBottom();
                    Room bottomRightRoomOfTopLeftRoom = topLeftRoom.getRoomBottomRight();

                    if (bottomLeftRoomOfTopLeftRoom == bottomRoomOfTopLeftRoom)
                    {
                        if (topLeftRoom.getDoorBottomLeft() != topLeftRoom.getDoorBottom())
                        {
                            room.setDoorTopLeft(topLeftRoom.getDoorBottomLeft());
                            room.setDoorTopRight(topLeftRoom.getDoorBottom());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorTopLeft(false);
                                room.setDoorTopRight(true);
                            }
                            else
                            {
                                room.setDoorTopLeft(true);
                                room.setDoorTopRight(false);
                            }
                        }
                    }
                    else if (bottomRoomOfTopLeftRoom == bottomRightRoomOfTopLeftRoom)
                    {
                        if (topLeftRoom.getDoorBottom() != topLeftRoom.getDoorBottomRight())
                        {
                            room.setDoorTopLeft(topLeftRoom.getDoorBottom());
                            room.setDoorTopRight(topLeftRoom.getDoorBottomRight());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorTopLeft(false);
                                room.setDoorTopRight(true);
                            }
                            else
                            {
                                room.setDoorTopLeft(true);
                                room.setDoorTopRight(false);
                            }
                        }
                    }
                }
            }
            else
            {
                room.setDoorTopLeft(doorTopLeft);
                room.setDoorTopRight(doorTopRight);
            }

            room.setDoorLeft(!hasLeftNeighbor(room));
            room.setDoorRight(!hasRightNeighbor(room));
        }
        else if (room.size == TwoxOne)
        {
            bool doorLeftBottom = !hasLeftBottomNeighbor(room);
            bool doorLeftTop = !hasLeftTopNeighbor(room);
            bool doorRightBottom = !hasRightBottomNeighbor(room);
            bool doorRightTop = !hasRightTopNeighbor(room);

            if (!doorLeftBottom && !doorLeftTop && room.getRoomLeftBottom() == room.getRoomLeftTop())
            {
                Room leftBottomRoom = room.getRoomLeftBottom();
                if (leftBottomRoom.size == TwoxOne || leftBottomRoom.size == TwoxTwo)
                {
                    if (leftBottomRoom.getDoorRightBottom() != leftBottomRoom.getDoorRightTop())
                    {
                        room.setDoorLeftBottom(leftBottomRoom.getDoorRightBottom());
                        room.setDoorLeftTop(leftBottomRoom.getDoorRightTop());
                    }
                    else
                    {
                        float random = Random.value;
                        if (random < 0.5f)
                        {
                            room.setDoorLeftBottom(false);
                            room.setDoorLeftTop(true);
                        }
                        else
                        {
                            room.setDoorLeftBottom(true);
                            room.setDoorLeftTop(false);
                        }
                    }
                }
                else if (leftBottomRoom.size == ThreexThree)
                {
                    Room rightBottomRoomOfLeftBottomRoom = leftBottomRoom.getRoomRightBottom();
                    Room rightRoomOfLeftBottomRoom = leftBottomRoom.getRoomRight();
                    Room rightTopRoomOfLeftBottomRoom = leftBottomRoom.getRoomRightTop();

                    if (rightBottomRoomOfLeftBottomRoom == rightRoomOfLeftBottomRoom)
                    {
                        if (leftBottomRoom.getDoorRightBottom() != leftBottomRoom.getDoorRight())
                        {
                            room.setDoorLeftBottom(leftBottomRoom.getDoorRightBottom());
                            room.setDoorLeftTop(leftBottomRoom.getDoorRight());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorLeftBottom(false);
                                room.setDoorLeftTop(true);
                            }
                            else
                            {
                                room.setDoorLeftBottom(true);
                                room.setDoorLeftTop(false);
                            }
                        }
                    }
                    else if (rightRoomOfLeftBottomRoom == rightTopRoomOfLeftBottomRoom)
                    {
                        if (leftBottomRoom.getDoorRight() != leftBottomRoom.getDoorRightTop())
                        {
                            room.setDoorLeftBottom(leftBottomRoom.getDoorRight());
                            room.setDoorLeftTop(leftBottomRoom.getDoorRightTop());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorLeftBottom(false);
                                room.setDoorLeftTop(true);
                            }
                            else
                            {
                                room.setDoorLeftBottom(true);
                                room.setDoorLeftTop(false);
                            }
                        }
                    }
                }
            }
            else
            {
                room.setDoorLeftBottom(doorLeftBottom);
                room.setDoorLeftTop(doorLeftTop);
            }

            if (!doorRightBottom && !doorRightTop && room.getRoomRightBottom() == room.getRoomRightTop())
            {
                Room rightBottomRoom = room.getRoomRightBottom();
                if (rightBottomRoom.size == TwoxOne || rightBottomRoom.size == TwoxTwo)
                {
                    if (rightBottomRoom.getDoorLeftBottom() != rightBottomRoom.getDoorLeftTop())
                    {
                        room.setDoorRightBottom(rightBottomRoom.getDoorLeftBottom());
                        room.setDoorRightTop(rightBottomRoom.getDoorLeftTop());
                    }
                    else
                    {
                        float random = Random.value;
                        if (random < 0.5f)
                        {
                            room.setDoorRightBottom(false);
                            room.setDoorRightTop(true);
                        }
                        else
                        {
                            room.setDoorRightBottom(true);
                            room.setDoorRightTop(false);
                        }
                    }
                }
                else if (rightBottomRoom.size == ThreexThree)
                {
                    Room leftBottomRoomOfRightBottomRoom = rightBottomRoom.getRoomLeftBottom();
                    Room leftRoomOfRightBottomRoom = rightBottomRoom.getRoomLeft();
                    Room leftTopRoomOfRightBottomRoom = rightBottomRoom.getRoomLeftTop();

                    if (leftBottomRoomOfRightBottomRoom == leftRoomOfRightBottomRoom)
                    {
                        if (rightBottomRoom.getDoorLeftBottom() != rightBottomRoom.getDoorLeft())
                        {
                            room.setDoorRightBottom(rightBottomRoom.getDoorLeftBottom());
                            room.setDoorRightTop(rightBottomRoom.getDoorLeft());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorRightBottom(false);
                                room.setDoorRightTop(true);
                            }
                            else
                            {
                                room.setDoorRightBottom(true);
                                room.setDoorRightTop(false);
                            }
                        }
                    }
                    else if (leftRoomOfRightBottomRoom == leftTopRoomOfRightBottomRoom)
                    {
                        if (rightBottomRoom.getDoorLeft() != rightBottomRoom.getDoorLeftTop())
                        {
                            room.setDoorRightBottom(rightBottomRoom.getDoorLeft());
                            room.setDoorRightTop(rightBottomRoom.getDoorLeftTop());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorRightBottom(false);
                                room.setDoorRightTop(true);
                            }
                            else
                            {
                                room.setDoorRightBottom(true);
                                room.setDoorRightTop(false);
                            }
                        }
                    }
                }
            }
            else
            {
                room.setDoorRightBottom(doorRightBottom);
                room.setDoorRightTop(doorRightTop);
            }

            room.setDoorBottom(!hasBottomNeighbor(room));
            room.setDoorTop(!hasTopNeighbor(room));
        }
        else if (room.size == TwoxTwo)
        {
            bool doorBottomLeft = !hasBottomLeftNeighbor(room);
            bool doorBottomRight = !hasBottomRightNeighbor(room);
            bool doorLeftBottom = !hasLeftBottomNeighbor(room);
            bool doorLeftTop = !hasLeftTopNeighbor(room);
            bool doorRightBottom = !hasRightBottomNeighbor(room);
            bool doorRightTop = !hasRightTopNeighbor(room);
            bool doorTopLeft = !hasTopLeftNeighbor(room);
            bool doorTopRight = !hasTopRightNeighbor(room);

            if (!doorBottomLeft && !doorBottomRight && room.getRoomBottomLeft() == room.getRoomBottomRight())
            {
                Room bottomLeftRoom = room.getRoomBottomLeft();
                if (bottomLeftRoom.size == OnexTwo || bottomLeftRoom.size == TwoxTwo)
                {
                    if (bottomLeftRoom.getDoorTopLeft() != bottomLeftRoom.getDoorTopRight())
                    {
                        room.setDoorBottomLeft(bottomLeftRoom.getDoorTopLeft());
                        room.setDoorBottomRight(bottomLeftRoom.getDoorTopRight());
                    }
                    else
                    {
                        float random = Random.value;
                        if (random < 0.5f)
                        {
                            room.setDoorBottomLeft(false);
                            room.setDoorBottomRight(true);
                        }
                        else
                        {
                            room.setDoorBottomLeft(true);
                            room.setDoorBottomRight(false);
                        }
                    }
                }
                else if (bottomLeftRoom.size == ThreexThree)
                {
                    Room topLeftRoomOfBottomLeftRoom = bottomLeftRoom.getRoomTopLeft();
                    Room topRoomOfBottomLeftRoom = bottomLeftRoom.getRoomTop();
                    Room topRightRoomOfBottomLeftRoom = bottomLeftRoom.getRoomTopRight();

                    if (topLeftRoomOfBottomLeftRoom == topRoomOfBottomLeftRoom)
                    {
                        if (bottomLeftRoom.getDoorTopLeft() != bottomLeftRoom.getDoorTop())
                        {
                            room.setDoorBottomLeft(bottomLeftRoom.getDoorTopLeft());
                            room.setDoorBottomRight(bottomLeftRoom.getDoorTop());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorBottomLeft(false);
                                room.setDoorBottomRight(true);
                            }
                            else
                            {
                                room.setDoorBottomLeft(true);
                                room.setDoorBottomRight(false);
                            }
                        }
                    }
                    else if (topRoomOfBottomLeftRoom == topRightRoomOfBottomLeftRoom)
                    {
                        if (bottomLeftRoom.getDoorTop() != bottomLeftRoom.getDoorTopRight())
                        {
                            room.setDoorBottomLeft(bottomLeftRoom.getDoorTop());
                            room.setDoorBottomRight(bottomLeftRoom.getDoorTopRight());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorBottomLeft(false);
                                room.setDoorBottomRight(true);
                            }
                            else
                            {
                                room.setDoorBottomLeft(true);
                                room.setDoorBottomRight(false);
                            }
                        }
                    }
                }
            }
            else
            {
                room.setDoorBottomLeft(doorBottomLeft);
                room.setDoorBottomRight(doorBottomRight);
            }

            if (!doorLeftBottom && !doorLeftTop && room.getRoomLeftBottom() == room.getRoomLeftTop())
            {
                Room leftBottomRoom = room.getRoomLeftBottom();
                if (leftBottomRoom.size == TwoxOne || leftBottomRoom.size == TwoxTwo)
                {
                    if (leftBottomRoom.getDoorRightBottom() != leftBottomRoom.getDoorRightTop())
                    {
                        room.setDoorLeftBottom(leftBottomRoom.getDoorRightBottom());
                        room.setDoorLeftTop(leftBottomRoom.getDoorRightTop());
                    }
                    else
                    {
                        float random = Random.value;
                        if (random < 0.5f)
                        {
                            room.setDoorLeftBottom(false);
                            room.setDoorLeftTop(true);
                        }
                        else
                        {
                            room.setDoorLeftBottom(true);
                            room.setDoorLeftTop(false);
                        }
                    }
                }
                else if (leftBottomRoom.size == ThreexThree)
                {
                    Room rightBottomRoomOfLeftBottomRoom = leftBottomRoom.getRoomRightBottom();
                    Room rightRoomOfLeftBottomRoom = leftBottomRoom.getRoomRight();
                    Room rightTopRoomOfLeftBottomRoom = leftBottomRoom.getRoomRightTop();

                    if (rightBottomRoomOfLeftBottomRoom == rightRoomOfLeftBottomRoom)
                    {
                        if (leftBottomRoom.getDoorRightBottom() != leftBottomRoom.getDoorRight())
                        {
                            room.setDoorLeftBottom(leftBottomRoom.getDoorRightBottom());
                            room.setDoorLeftTop(leftBottomRoom.getDoorRight());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorLeftBottom(false);
                                room.setDoorLeftTop(true);
                            }
                            else
                            {
                                room.setDoorLeftBottom(true);
                                room.setDoorLeftTop(false);
                            }
                        }
                    }
                    else if (rightRoomOfLeftBottomRoom == rightTopRoomOfLeftBottomRoom)
                    {
                        if (leftBottomRoom.getDoorRight() != leftBottomRoom.getDoorRightTop())
                        {
                            room.setDoorLeftBottom(leftBottomRoom.getDoorRight());
                            room.setDoorLeftTop(leftBottomRoom.getDoorRightTop());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorLeftBottom(false);
                                room.setDoorLeftTop(true);
                            }
                            else
                            {
                                room.setDoorLeftBottom(true);
                                room.setDoorLeftTop(false);
                            }
                        }
                    }
                }
            }
            else
            {
                room.setDoorLeftBottom(doorLeftBottom);
                room.setDoorLeftTop(doorLeftTop);
            }

            if (!doorRightBottom && !doorRightTop && room.getRoomRightBottom() == room.getRoomRightTop())
            {
                Room rightBottomRoom = room.getRoomRightBottom();
                if (rightBottomRoom.size == TwoxOne || rightBottomRoom.size == TwoxTwo)
                {
                    if (rightBottomRoom.getDoorLeftBottom() != rightBottomRoom.getDoorLeftTop())
                    {
                        room.setDoorRightBottom(rightBottomRoom.getDoorLeftBottom());
                        room.setDoorRightTop(rightBottomRoom.getDoorLeftTop());
                    }
                    else
                    {
                        float random = Random.value;
                        if (random < 0.5f)
                        {
                            room.setDoorRightBottom(false);
                            room.setDoorRightTop(true);
                        }
                        else
                        {
                            room.setDoorRightBottom(true);
                            room.setDoorRightTop(false);
                        }
                    }
                }
                else if (rightBottomRoom.size == ThreexThree)
                {
                    Room leftBottomRoomOfRightBottomRoom = rightBottomRoom.getRoomLeftBottom();
                    Room leftRoomOfRightBottomRoom = rightBottomRoom.getRoomLeft();
                    Room leftTopRoomOfRightBottomRoom = rightBottomRoom.getRoomLeftTop();

                    if (leftBottomRoomOfRightBottomRoom == leftRoomOfRightBottomRoom)
                    {
                        if (rightBottomRoom.getDoorLeftBottom() != rightBottomRoom.getDoorLeft())
                        {
                            room.setDoorRightBottom(rightBottomRoom.getDoorLeftBottom());
                            room.setDoorRightTop(rightBottomRoom.getDoorLeft());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorRightBottom(false);
                                room.setDoorRightTop(true);
                            }
                            else
                            {
                                room.setDoorRightBottom(true);
                                room.setDoorRightTop(false);
                            }
                        }
                    }
                    else if (leftRoomOfRightBottomRoom == leftTopRoomOfRightBottomRoom)
                    {
                        if (rightBottomRoom.getDoorLeft() != rightBottomRoom.getDoorLeftTop())
                        {
                            room.setDoorRightBottom(rightBottomRoom.getDoorLeft());
                            room.setDoorRightTop(rightBottomRoom.getDoorLeftTop());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorRightBottom(false);
                                room.setDoorRightTop(true);
                            }
                            else
                            {
                                room.setDoorRightBottom(true);
                                room.setDoorRightTop(false);
                            }
                        }
                    }
                }
            }
            else
            {
                room.setDoorRightBottom(doorRightBottom);
                room.setDoorRightTop(doorRightTop);
            }

            if (!doorTopLeft && !doorTopRight && room.getRoomTopLeft() == room.getRoomTopRight())
            {
                Room topLeftRoom = room.getRoomTopLeft();
                if (topLeftRoom.size == OnexTwo || topLeftRoom.size == TwoxTwo)
                {
                    if (topLeftRoom.getDoorBottomLeft() != topLeftRoom.getDoorBottomRight())
                    {
                        room.setDoorTopLeft(topLeftRoom.getDoorBottomLeft());
                        room.setDoorTopRight(topLeftRoom.getDoorBottomRight());
                    }
                    else
                    {
                        float random = Random.value;
                        if (random < 0.5f)
                        {
                            room.setDoorTopLeft(false);
                            room.setDoorTopRight(true);
                        }
                        else
                        {
                            room.setDoorTopLeft(true);
                            room.setDoorTopRight(false);
                        }
                    }
                }
                else if (topLeftRoom.size == ThreexThree)
                {
                    Room bottomLeftRoomOfTopLeftRoom = topLeftRoom.getRoomBottomLeft();
                    Room bottomRoomOfTopLeftRoom = topLeftRoom.getRoomBottom();
                    Room bottomRightRoomOfTopLeftRoom = topLeftRoom.getRoomBottomRight();

                    if (bottomLeftRoomOfTopLeftRoom == bottomRoomOfTopLeftRoom)
                    {
                        if (topLeftRoom.getDoorBottomLeft() != topLeftRoom.getDoorBottom())
                        {
                            room.setDoorTopLeft(topLeftRoom.getDoorBottomLeft());
                            room.setDoorTopRight(topLeftRoom.getDoorBottom());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorTopLeft(false);
                                room.setDoorTopRight(true);
                            }
                            else
                            {
                                room.setDoorTopLeft(true);
                                room.setDoorTopRight(false);
                            }
                        }
                    }
                    else if (bottomRoomOfTopLeftRoom == bottomRightRoomOfTopLeftRoom)
                    {
                        if (topLeftRoom.getDoorBottom() != topLeftRoom.getDoorBottomRight())
                        {
                            room.setDoorTopLeft(topLeftRoom.getDoorBottom());
                            room.setDoorTopRight(topLeftRoom.getDoorBottomRight());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorTopLeft(false);
                                room.setDoorTopRight(true);
                            }
                            else
                            {
                                room.setDoorTopLeft(true);
                                room.setDoorTopRight(false);
                            }
                        }
                    }
                }
            }
            else
            {
                room.setDoorTopLeft(doorTopLeft);
                room.setDoorTopRight(doorTopRight);
            }
        }
        else
        {
            bool doorBottomLeft = !hasBottomLeftNeighbor(room);
            bool doorBottom = !hasBottomNeighbor(room);
            bool doorBottomRight = !hasBottomRightNeighbor(room);
            bool doorLeftBottom = !hasLeftBottomNeighbor(room);
            bool doorLeft = !hasLeftNeighbor(room);
            bool doorLeftTop = !hasLeftTopNeighbor(room);
            bool doorRightBottom = !hasRightBottomNeighbor(room);
            bool doorRight = !hasRightNeighbor(room);
            bool doorRightTop = !hasRightTopNeighbor(room);
            bool doorTopLeft = !hasTopLeftNeighbor(room);
            bool doorTop = !hasTopNeighbor(room);
            bool doorTopRight = !hasTopRightNeighbor(room);

        if (!doorBottomLeft && !doorBottom && !doorBottomRight
                && room.getRoomBottomLeft() == room.getRoomBottom() 
                && room.getRoomBottom() == room.getRoomBottomRight())
            {
                room.setDoorBottomLeft(true);
                room.setDoorBottom(false);
                room.setDoorBottomRight(true);
            }
            else if (!doorBottomLeft && !doorBottom && room.getRoomBottomLeft() == room.getRoomBottom())
            {
                Room bottomLeftRoom = room.getRoomBottomLeft();

                if (bottomLeftRoom.size == OnexTwo || bottomLeftRoom.size == TwoxTwo)
                {
                    if (bottomLeftRoom.getDoorTopLeft() != bottomLeftRoom.getDoorTopRight())
                    {
                        room.setDoorBottomLeft(bottomLeftRoom.getDoorTopLeft());
                        room.setDoorBottom(bottomLeftRoom.getDoorTopRight());
                    }
                    else
                    {
                        float random = Random.value;
                        if (random < 0.5f)
                        {
                            room.setDoorBottomLeft(false);
                            room.setDoorBottom(true);
                        }
                        else
                        {
                            room.setDoorBottomLeft(true);
                            room.setDoorBottom(false);
                        }
                    }
                }
                else if (bottomLeftRoom.size == ThreexThree)
                {
                    Room topRoomOfBottomLeftRoom = bottomLeftRoom.getRoomTop();
                    Room topRightRoomOfBottomLeftRoom = bottomLeftRoom.getRoomTopRight();

                    if (topRoomOfBottomLeftRoom == topRightRoomOfBottomLeftRoom)
                    {
                        if (bottomLeftRoom.getDoorTop() != bottomLeftRoom.getDoorTopRight())
                        {
                            room.setDoorBottomLeft(bottomLeftRoom.getDoorTop());
                            room.setDoorBottom(bottomLeftRoom.getDoorTopRight());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorBottomLeft(false);
                                room.setDoorBottom(true);
                            }
                            else
                            {
                                room.setDoorBottomLeft(true);
                                room.setDoorBottom(false);
                            }
                        }
                    }
                }

                room.setDoorBottomRight(doorBottomRight);
            }
            else if (!doorBottom && !doorBottomRight && room.getRoomBottom() == room.getRoomBottomRight())
            {
                Room bottomRoom = room.getRoomBottom();

                if (bottomRoom.size == OnexTwo || bottomRoom.size == TwoxTwo)
                {
                    if (bottomRoom.getDoorTopLeft() != bottomRoom.getDoorTopRight())
                    {
                        room.setDoorBottom(bottomRoom.getDoorTopLeft());
                        room.setDoorBottomRight(bottomRoom.getDoorTopRight());
                    }
                    else
                    {
                        float random = Random.value;
                        if (random < 0.5f)
                        {
                            room.setDoorBottom(false);
                            room.setDoorBottomRight(true);
                        }
                        else
                        {
                            room.setDoorBottom(true);
                            room.setDoorBottomRight(false);
                        }
                    }
                }
                else if (bottomRoom.size == ThreexThree)
                {
                    Room topLeftRoomOfBottomRoom = bottomRoom.getRoomTopLeft();
                    Room topRoomOfBottomRoom = bottomRoom.getRoomTop();

                    if (topLeftRoomOfBottomRoom == topRoomOfBottomRoom)
                    {
                        if (bottomRoom.getDoorTopLeft() != bottomRoom.getDoorTop())
                        {
                            room.setDoorBottom(bottomRoom.getDoorTopLeft());
                            room.setDoorBottomRight(bottomRoom.getDoorTop());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorBottom(false);
                                room.setDoorBottomRight(true);
                            }
                            else
                            {
                                room.setDoorBottom(true);
                                room.setDoorBottomRight(false);
                            }
                        }
                    }
                }

                room.setDoorBottomLeft(doorBottomLeft);
            }
            else
            {
                room.setDoorBottomLeft(doorBottomLeft);
                room.setDoorBottom(doorBottom);
                room.setDoorBottomRight(doorBottomRight);
            }

            if (!doorLeftBottom && !doorLeft && !doorLeftTop
                && room.getRoomLeftBottom() == room.getRoomLeft()
                && room.getRoomLeft() == room.getRoomLeftTop())
            {
                room.setDoorLeftBottom(true);
                room.setDoorLeft(false);
                room.setDoorLeftTop(true);
            }
            else if (!doorLeftBottom && !doorLeft && room.getRoomLeftBottom() == room.getRoomLeft())
            {
                Room leftBottomRoom = room.getRoomLeftBottom();

                if (leftBottomRoom.size == TwoxOne || leftBottomRoom.size == TwoxTwo)
                {
                    if (leftBottomRoom.getDoorRightBottom() != leftBottomRoom.getDoorRightTop())
                    {
                        room.setDoorLeftBottom(leftBottomRoom.getDoorRightBottom());
                        room.setDoorLeft(leftBottomRoom.getDoorRightTop());
                    }
                    else
                    {
                        float random = Random.value;
                        if (random < 0.5f)
                        {
                            room.setDoorLeftBottom(false);
                            room.setDoorLeft(true);
                        }
                        else
                        {
                            room.setDoorLeftBottom(true);
                            room.setDoorLeft(false);
                        }
                    }
                }
                else if (leftBottomRoom.size == ThreexThree)
                {
                    Room rightRoomOfLeftBottomRoom = leftBottomRoom.getRoomRight();
                    Room rightTopRoomOfLeftBottomRoom = leftBottomRoom.getRoomRightTop();

                    if (rightRoomOfLeftBottomRoom == rightTopRoomOfLeftBottomRoom)
                    {
                        if (leftBottomRoom.getDoorRight() != leftBottomRoom.getDoorRightTop())
                        {
                            room.setDoorLeftBottom(leftBottomRoom.getDoorRight());
                            room.setDoorLeft(leftBottomRoom.getDoorRightTop());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorLeftBottom(false);
                                room.setDoorLeft(true);
                            }
                            else
                            {
                                room.setDoorLeftBottom(true);
                                room.setDoorLeft(false);
                            }
                        }
                    }
                }

                room.setDoorLeftTop(doorLeftTop);
            }
            else if (!doorLeft && !doorLeftTop && room.getRoomLeft() == room.getRoomLeftTop())
            {
                Room leftRoom = room.getRoomLeft();

                if (leftRoom.size == TwoxOne || leftRoom.size == TwoxTwo)
                {
                    if (leftRoom.getDoorRightBottom() != leftRoom.getDoorRightTop())
                    {
                        room.setDoorLeft(leftRoom.getDoorRightBottom());
                        room.setDoorLeftTop(leftRoom.getDoorRightTop());
                    }
                    else
                    {
                        float random = Random.value;
                        if (random < 0.5f)
                        {
                            room.setDoorLeft(false);
                            room.setDoorLeftTop(true);
                        }
                        else
                        {
                            room.setDoorLeft(true);
                            room.setDoorLeftTop(false);
                        }
                    }
                }
                else if (leftRoom.size == ThreexThree)
                {
                    Room rightBottomRoomOfLeftRoom = leftRoom.getRoomRightBottom();
                    Room rightRoomOfLeftRoom = leftRoom.getRoomRight();

                    if (rightBottomRoomOfLeftRoom == rightRoomOfLeftRoom)
                    {
                        if (leftRoom.getDoorRightBottom() != leftRoom.getDoorRight())
                        {
                            room.setDoorLeft(leftRoom.getDoorRightBottom());
                            room.setDoorLeftTop(leftRoom.getDoorRight());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorLeft(false);
                                room.setDoorLeftTop(true);
                            }
                            else
                            {
                                room.setDoorLeft(true);
                                room.setDoorLeftTop(false);
                            }
                        }
                    }
                }

                room.setDoorLeftBottom(doorLeftBottom);
            }
            else
            {
                room.setDoorLeftBottom(doorLeftBottom);
                room.setDoorLeft(doorLeft);
                room.setDoorLeftTop(doorLeftTop);
            }

            if (!doorRightBottom && !doorRight && !doorRightTop
                && room.getRoomRightBottom() == room.getRoomRight()
                && room.getRoomRight() == room.getRoomRightTop())
            {
                room.setDoorRightBottom(true);
                room.setDoorRight(false);
                room.setDoorRightTop(true);
            }
            else if (!doorRightBottom && !doorRight && room.getRoomRightBottom() == room.getRoomRight())
            {
                Room rightBottomRoom = room.getRoomRightBottom();

                if (rightBottomRoom.size == TwoxOne || rightBottomRoom.size == TwoxTwo)
                {
                    if (rightBottomRoom.getDoorLeftBottom() != rightBottomRoom.getDoorLeftTop())
                    {
                        room.setDoorRightBottom(rightBottomRoom.getDoorLeftBottom());
                        room.setDoorRight(rightBottomRoom.getDoorLeftTop());
                    }
                    else
                    {
                        float random = Random.value;
                        if (random < 0.5f)
                        {
                            room.setDoorRightBottom(false);
                            room.setDoorRight(true);
                        }
                        else
                        {
                            room.setDoorRightBottom(true);
                            room.setDoorRight(false);
                        }
                    }
                }
                else if (rightBottomRoom.size == ThreexThree)
                {
                    Room leftRoomOfRightBottomRoom = rightBottomRoom.getRoomLeft();
                    Room leftTopRoomOfRightBottomRoom = rightBottomRoom.getRoomLeftTop();

                    if (leftRoomOfRightBottomRoom == leftTopRoomOfRightBottomRoom)
                    {
                        if (rightBottomRoom.getDoorLeft() != rightBottomRoom.getDoorLeftTop())
                        {
                            room.setDoorRightBottom(rightBottomRoom.getDoorLeft());
                            room.setDoorRight(rightBottomRoom.getDoorLeftTop());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorRightBottom(false);
                                room.setDoorRight(true);
                            }
                            else
                            {
                                room.setDoorRightBottom(true);
                                room.setDoorRight(false);
                            }
                        }
                    }
                }

                room.setDoorRightTop(doorRightTop);
            }
            else if (!doorRight && !doorRightTop && room.getRoomRight() == room.getRoomRightTop())
            {
                Room rightRoom = room.getRoomRight();

                if (rightRoom.size == TwoxOne || rightRoom.size == TwoxTwo)
                {
                    if (rightRoom.getDoorLeftBottom() != rightRoom.getDoorLeftTop())
                    {
                        room.setDoorRight(rightRoom.getDoorLeftBottom());
                        room.setDoorRightTop(rightRoom.getDoorLeftTop());
                    }
                    else
                    {
                        float random = Random.value;
                        if (random < 0.5f)
                        {
                            room.setDoorRight(false);
                            room.setDoorRightTop(true);
                        }
                        else
                        {
                            room.setDoorRight(true);
                            room.setDoorRightTop(false);
                        }
                    }
                }
                else if (rightRoom.size == ThreexThree)
                {
                    Room leftBottomRoomOfRightRoom = rightRoom.getRoomLeftBottom();
                    Room leftRoomOfRightRoom = rightRoom.getRoomLeft();

                    if (leftBottomRoomOfRightRoom == leftRoomOfRightRoom)
                    {
                        if (rightRoom.getDoorLeftBottom() != rightRoom.getDoorLeft())
                        {
                            room.setDoorRight(rightRoom.getDoorLeftBottom());
                            room.setDoorRightTop(rightRoom.getDoorLeft());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorRight(false);
                                room.setDoorRightTop(true);
                            }
                            else
                            {
                                room.setDoorRight(true);
                                room.setDoorRightTop(false);
                            }
                        }
                    }
                }

                room.setDoorRightBottom(doorRightBottom);
            }
            else
            {
                room.setDoorRightBottom(doorRightBottom);
                room.setDoorRight(doorRight);
                room.setDoorRightTop(doorRightTop);
            }

            if (!doorTopLeft && !doorTop && !doorTopRight
                && room.getRoomTopLeft() == room.getRoomTop()
                && room.getRoomTop() == room.getRoomTopRight())
            {
                room.setDoorTopLeft(true);
                room.setDoorTop(false);
                room.setDoorTopRight(true);
            }
            else if (!doorTopLeft && !doorTop && room.getRoomTopLeft() == room.getRoomTop())
            {
                Room topLeftRoom = room.getRoomTopLeft();

                if (topLeftRoom.size == OnexTwo || topLeftRoom.size == TwoxTwo)
                {
                    if (topLeftRoom.getDoorBottomLeft() != topLeftRoom.getDoorBottomRight())
                    {
                        room.setDoorTopLeft(topLeftRoom.getDoorBottomLeft());
                        room.setDoorTop(topLeftRoom.getDoorBottomRight());
                    }
                    else
                    {
                        float random = Random.value;
                        if (random < 0.5f)
                        {
                            room.setDoorTopLeft(false);
                            room.setDoorTop(true);
                        }
                        else
                        {
                            room.setDoorTopLeft(true);
                            room.setDoorTop(false);
                        }
                    }
                }
                else if (topLeftRoom.size == ThreexThree)
                {
                    Room bottomRoomOfTopLeftRoom = topLeftRoom.getRoomBottom();
                    Room bottomRightRoomOfTopLeftRoom = topLeftRoom.getRoomBottomRight();

                    if (bottomRoomOfTopLeftRoom == bottomRightRoomOfTopLeftRoom)
                    {
                        if (topLeftRoom.getDoorBottom() != topLeftRoom.getDoorBottomRight())
                        {
                            room.setDoorTopLeft(topLeftRoom.getDoorBottom());
                            room.setDoorTop(topLeftRoom.getDoorBottomRight());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorTopLeft(false);
                                room.setDoorTop(true);
                            }
                            else
                            {
                                room.setDoorTopLeft(true);
                                room.setDoorTop(false);
                            }
                        }
                    }
                }

                room.setDoorTopRight(doorTopRight);
            }
            else if (!doorTop && !doorTopRight && room.getRoomTop() == room.getRoomTopRight())
            {
                Room topRoom = room.getRoomTop();

                if (topRoom.size == OnexTwo || topRoom.size == TwoxTwo)
                {
                    if (topRoom.getDoorBottomLeft() != topRoom.getDoorBottomRight())
                    {
                        room.setDoorTop(topRoom.getDoorBottomLeft());
                        room.setDoorTopRight(topRoom.getDoorBottomRight());
                    }
                    else
                    {
                        float random = Random.value;
                        if (random < 0.5f)
                        {
                            room.setDoorTop(false);
                            room.setDoorTopRight(true);
                        }
                        else
                        {
                            room.setDoorTop(true);
                            room.setDoorTopRight(false);
                        }
                    }
                }
                else if (topRoom.size == ThreexThree)
                {
                    Room bottomLeftRoomOfTopRoom = topRoom.getRoomBottomLeft();
                    Room bottomRoomOfTopRoom = topRoom.getRoomBottom();

                    if (bottomLeftRoomOfTopRoom == bottomRoomOfTopRoom)
                    {
                        if (topRoom.getDoorBottomLeft() != topRoom.getDoorBottom())
                        {
                            room.setDoorTop(topRoom.getDoorBottomLeft());
                            room.setDoorTopRight(topRoom.getDoorBottom());
                        }
                        else
                        {
                            float random = Random.value;
                            if (random < 0.5f)
                            {
                                room.setDoorTop(false);
                                room.setDoorTopRight(true);
                            }
                            else
                            {
                                room.setDoorTop(true);
                                room.setDoorTopRight(false);
                            }
                        }
                    }
                }

                room.setDoorTopLeft(doorTopLeft);
            }
            else
            {
                room.setDoorTopLeft(doorTopLeft);
                room.setDoorTop(doorTop);
                room.setDoorTopRight(doorTopRight);
            }
        }
    }

    private int getNumUniqueNeighbors(Room room)
    {
        int numRooms = 0;

        if (room.size == OnexOne)
        {
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
            if (hasTopNeighbor(room))
            {
                numRooms++;
            }
        }
        else if (room.size == OnexTwo)
        {
            bool bottomLeft = hasBottomLeftNeighbor(room);
            bool bottomRight = hasBottomRightNeighbor(room);
            bool topLeft = hasTopLeftNeighbor(room);
            bool topRight = hasTopRightNeighbor(room);

            if (bottomLeft && bottomRight && room.getRoomBottomLeft() == room.getRoomBottomRight())
            {
                numRooms++;
            }
            else
            {
                if (bottomLeft)
                {
                    numRooms++;
                }
                if (bottomRight)
                {
                    numRooms++;
                }
            }

            if (topLeft && topRight && room.getRoomTopLeft() == room.getRoomTopRight())
            {
                numRooms++;
            }
            else
            {
                if (topLeft)
                {
                    numRooms++;
                }
                if (topRight)
                {
                    numRooms++;
                }
            }

            if (hasLeftNeighbor(room))
            {
                numRooms++;
            }
            if (hasRightNeighbor(room))
            {
                numRooms++;
            }
        }
        else if (room.size == TwoxOne)
        {
            bool leftBottom = hasLeftBottomNeighbor(room);
            bool leftTop = hasLeftTopNeighbor(room);
            bool rightBottom = hasRightBottomNeighbor(room);
            bool rightTop = hasRightTopNeighbor(room);

            if (leftBottom && leftTop && room.getRoomLeftBottom() == room.getRoomLeftTop())
            {
                numRooms++;
            }
            else
            {
                if (leftBottom)
                {
                    numRooms++;
                }
                if (leftTop)
                {
                    numRooms++;
                }
            }
            if (rightBottom && rightTop && room.getRoomRightBottom() == room.getRoomRightTop())
            {
                numRooms++;
            }
            else
            {
                if (rightBottom)
                {
                    numRooms++;
                }
                if (rightTop)
                {
                    numRooms++;
                }
            }

            if (hasBottomNeighbor(room))
            {
                numRooms++;
            }
            if (hasTopNeighbor(room))
            {
                numRooms++;
            }
        }
        else if (room.size == TwoxTwo)
        {
            bool bottomLeft = hasBottomLeftNeighbor(room);
            bool bottomRight = hasBottomRightNeighbor(room);
            bool leftBottom = hasLeftBottomNeighbor(room);
            bool leftTop = hasLeftTopNeighbor(room);
            bool rightBottom = hasRightBottomNeighbor(room);
            bool rightTop = hasRightTopNeighbor(room);
            bool topLeft = hasTopLeftNeighbor(room);
            bool topRight = hasTopRightNeighbor(room);

            if (bottomLeft && bottomRight && room.getRoomBottomLeft() == room.getRoomBottomRight())
            {
                numRooms++;
            }
            else
            {
                if (bottomLeft)
                {
                    numRooms++;
                }
                if (bottomRight)
                {
                    numRooms++;
                }
            }

            if (leftBottom && leftTop && room.getRoomLeftBottom() == room.getRoomLeftTop())
            {
                numRooms++;
            }
            else
            {
                if (leftBottom)
                {
                    numRooms++;
                }
                if (leftTop)
                {
                    numRooms++;
                }
            }
            if (rightBottom && rightTop && room.getRoomRightBottom() == room.getRoomRightTop())
            {
                numRooms++;
            }
            else
            {
                if (rightBottom)
                {
                    numRooms++;
                }
                if (rightTop)
                {
                    numRooms++;
                }
            }

            if (topLeft && topRight && room.getRoomTopLeft() == room.getRoomTopRight())
            {
                numRooms++;
            }
            else
            {
                if (topLeft)
                {
                    numRooms++;
                }
                if (topRight)
                {
                    numRooms++;
                }
            }
        }
        else
        {
            bool bottomLeft = hasBottomLeftNeighbor(room);
            bool bottom = hasBottomNeighbor(room);
            bool bottomRight = hasBottomRightNeighbor(room);
            bool leftBottom = hasLeftBottomNeighbor(room);
            bool left = hasLeftNeighbor(room);
            bool leftTop = hasLeftTopNeighbor(room);
            bool rightBottom = hasRightBottomNeighbor(room);
            bool right = hasRightNeighbor(room);
            bool rightTop = hasRightTopNeighbor(room);
            bool topLeft = hasTopLeftNeighbor(room);
            bool top = hasTopNeighbor(room);
            bool topRight = hasTopRightNeighbor(room);

            if (bottomLeft && bottom && bottomRight
                    && room.getRoomBottomLeft() == room.getRoomBottom()
                    && room.getRoomBottom() == room.getRoomBottomRight())
            {
                numRooms++;
            }
            else if (bottomLeft && bottom && room.getRoomBottomLeft() == room.getRoomBottom())
            {
                numRooms++;
                if (bottomRight)
                {
                    numRooms++;
                }
            }
            else if (bottom && bottomRight && room.getRoomBottom() == room.getRoomBottomRight())
            {
                numRooms++;
                if (bottomLeft)
                {
                    numRooms++;
                }
            }
            else
            {
                if (bottomLeft)
                {
                    numRooms++;
                }
                if (bottom)
                {
                    numRooms++;
                }
                if (bottomRight)
                {
                    numRooms++;
                }
            }

            if (leftBottom && left && leftTop
            && room.getRoomLeftBottom() == room.getRoomLeft()
            && room.getRoomLeft() == room.getRoomLeftTop())
            {
                numRooms++;
            }
            else if (leftBottom && left && room.getRoomLeftBottom() == room.getRoomLeft())
            {
                numRooms++;
                if (leftTop)
                {
                    numRooms++;
                }
            }
            else if (left && leftTop && room.getRoomLeft() == room.getRoomLeftTop())
            {
                numRooms++;
                if (leftBottom)
                {
                    numRooms++;
                }
            }
            else
            {
                if (leftBottom)
                {
                    numRooms++;
                }
                if (left)
                {
                    numRooms++;
                }
                if (leftTop)
                {
                    numRooms++;
                }
            }

            if (rightBottom && right && rightTop
            && room.getRoomRightBottom() == room.getRoomRight()
            && room.getRoomRight() == room.getRoomRightTop())
            {
                numRooms++;
            }
            else if (rightBottom && right && room.getRoomRightBottom() == room.getRoomRight())
            {
                numRooms++;
                if (rightTop)
                {
                    numRooms++;
                }
            }
            else if (right && rightTop && room.getRoomRight() == room.getRoomRightTop())
            {
                numRooms++;
                if (rightBottom)
                {
                    numRooms++;
                }
            }
            else
            {
                if (rightBottom)
                {
                    numRooms++;
                }
                if (right)
                {
                    numRooms++;
                }
                if (rightTop)
                {
                    numRooms++;
                }
            }

            if (topLeft && top && topRight
                    && room.getRoomTopLeft() == room.getRoomTop()
                    && room.getRoomTop() == room.getRoomTopRight())
            {
                numRooms++;
            }
            else if (topLeft && top && room.getRoomTopLeft() == room.getRoomTop())
            {
                numRooms++;
                if (topRight)
                {
                    numRooms++;
                }
            }
            else if (top && topRight && room.getRoomTop() == room.getRoomTopRight())
            {
                numRooms++;
                if (topLeft)
                {
                    numRooms++;
                }
            }
            else
            {
                if (topLeft)
                {
                    numRooms++;
                }
                if (top)
                {
                    numRooms++;
                }
                if (topRight)
                {
                    numRooms++;
                }
            }
        }

        return numRooms;
    }

    //Gets the number of neighboring rooms surrounding a given room
    private int getNumNeighbors(Room room)
    {
        int numRooms = 0;

        if (room.size == OnexOne)
        {
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
            if (hasTopNeighbor(room))
            {
                numRooms++;
            }
        }
        else if (room.size == OnexTwo)
        {
            if (hasBottomLeftNeighbor(room))
            {
                numRooms++;
            }
            if (hasBottomRightNeighbor(room))
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
            if (hasTopLeftNeighbor(room))
            {
                numRooms++;
            }
            if (hasTopRightNeighbor(room))
            {
                numRooms++;
            }
        }
        else if (room.size == TwoxOne)
        {
            if (hasBottomNeighbor(room))
            {
                numRooms++;
            }
            if (hasLeftBottomNeighbor(room))
            {
                numRooms++;
            }
            if (hasLeftTopNeighbor(room))
            {
                numRooms++;
            }
            if (hasRightBottomNeighbor(room))
            {
                numRooms++;
            }
            if (hasRightTopNeighbor(room))
            {
                numRooms++;
            }
            if (hasTopNeighbor(room))
            {
                numRooms++;
            }
        }
        else if (room.size == TwoxTwo)
        {
            if (hasBottomLeftNeighbor(room))
            {
                numRooms++;
            }
            if (hasBottomRightNeighbor(room))
            {
                numRooms++;
            }
            if (hasLeftBottomNeighbor(room))
            {
                numRooms++;
            }
            if (hasLeftTopNeighbor(room))
            {
                numRooms++;
            }
            if (hasRightBottomNeighbor(room))
            {
                numRooms++;
            }
            if (hasRightTopNeighbor(room))
            {
                numRooms++;
            }
            if (hasTopLeftNeighbor(room))
            {
                numRooms++;
            }
            if (hasTopRightNeighbor(room))
            {
                numRooms++;
            }
        }
        else
        {
            if (hasBottomLeftNeighbor(room))
            {
                numRooms++;
            }
            if (hasBottomNeighbor(room))
            {
                numRooms++;
            }
            if (hasBottomRightNeighbor(room))
            {
                numRooms++;
            }
            if (hasLeftBottomNeighbor(room))
            {
                numRooms++;
            }
            if (hasLeftNeighbor(room))
            {
                numRooms++;
            }
            if (hasLeftTopNeighbor(room))
            {
                numRooms++;
            }
            if (hasRightBottomNeighbor(room))
            {
                numRooms++;
            }
            if (hasRightNeighbor(room))
            {
                numRooms++;
            }
            if (hasRightTopNeighbor(room))
            {
                numRooms++;
            }
            if (hasTopLeftNeighbor(room))
            {
                numRooms++;
            }
            if (hasTopNeighbor(room))
            {
                numRooms++;
            }
            if (hasTopRightNeighbor(room))
            {
                numRooms++;
            }
        }
        return numRooms;
    }

    // Only use this for temporary rooms. Otherwise, use room.getNeighboringRooms();
    private List<Room> getNeighbors(Room room)
    {
        List<Room> neighbors = new List<Room>();

        if (room.size == OnexOne)
        {
            if (hasBottomNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getBottomNeighborPosition(room)));
            }
            if (hasLeftNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getLeftNeighborPosition(room)));
            }
            if (hasRightNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getRightNeighborPosition(room)));
            }
            if (hasTopNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getTopNeighborPosition(room)));
            }
        }
        else if (room.size == OnexTwo)
        {
            if (hasBottomLeftNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getBottomLeftNeighborPosition(room)));
            }
            if (hasBottomRightNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getBottomRightNeighborPosition(room)));
            }
            if (hasLeftNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getLeftNeighborPosition(room)));
            }
            if (hasRightNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getRightNeighborPosition(room)));
            }
            if (hasTopLeftNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getTopLeftNeighborPosition(room)));
            }
            if (hasTopRightNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getTopRightNeighborPosition(room)));
            }
        }
        else if (room.size == TwoxOne)
        {
            if (hasBottomNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getBottomNeighborPosition(room)));
            }
            if (hasLeftBottomNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getLeftBottomNeighborPosition(room)));
            }
            if (hasLeftTopNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getLeftTopNeighborPosition(room)));
            }
            if (hasRightBottomNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getRightBottomNeighborPosition(room)));
            }
            if (hasRightTopNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getRightTopNeighborPosition(room)));
            }
            if (hasTopNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getTopNeighborPosition(room)));
            }
        }
        else if (room.size == TwoxTwo)
        {
            if (hasBottomLeftNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getBottomLeftNeighborPosition(room)));
            }
            if (hasBottomRightNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getBottomRightNeighborPosition(room)));
            }
            if (hasLeftBottomNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getLeftBottomNeighborPosition(room)));
            }
            if (hasLeftTopNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getLeftTopNeighborPosition(room)));
            }
            if (hasRightBottomNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getRightBottomNeighborPosition(room)));
            }
            if (hasRightTopNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getRightTopNeighborPosition(room)));
            }
            if (hasTopLeftNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getTopLeftNeighborPosition(room)));
            }
            if (hasTopRightNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getTopRightNeighborPosition(room)));
            }
        }
        else
        {
            if (hasBottomLeftNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getBottomLeftNeighborPosition(room)));
            }
            if (hasBottomNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getBottomNeighborPosition(room)));
            }
            if (hasBottomRightNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getBottomRightNeighborPosition(room)));
            }
            if (hasLeftBottomNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getLeftBottomNeighborPosition(room)));
            }
            if (hasLeftNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getLeftNeighborPosition(room)));
            }
            if (hasLeftTopNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getLeftTopNeighborPosition(room)));
            }
            if (hasRightBottomNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getRightBottomNeighborPosition(room)));
            }
            if (hasRightNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getRightNeighborPosition(room)));
            }
            if (hasRightTopNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getRightTopNeighborPosition(room)));
            }
            if (hasTopLeftNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getTopLeftNeighborPosition(room)));
            }
            if (hasTopNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getTopNeighborPosition(room)));
            }
            if (hasTopRightNeighbor(room))
            {
                neighbors.Add(findRoomAtLocation(getTopRightNeighborPosition(room)));
            }
        }

        return neighbors;
    }

    private Vector2 getBottomLeftNeighborPosition(Room room)
    {
        if (room.size != OnexTwo && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use getBottomNeighborPosition(Room) instead!");
        }

        if (room.size == OnexTwo)
        {
            return room.getLeft() + Vector2.down;
        }

        return room.getBottomLeft() + Vector2.down;
    }

    private Vector2 getBottomNeighborPosition(Room room)
    {
        if (room.size != OnexOne && room.size != TwoxOne && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use getBottomLeft/RightNeighborPosition(Room) instead!");
        }

        if (room.size == OnexOne)
        {
            return room.getMiddle() + Vector2.down;
        }

        return room.getBottom() + Vector2.down;
    }

    private Vector2 getBottomRightNeighborPosition(Room room)
    {
        if (room.size != OnexTwo && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use getBottomNeighborPosition(Room) instead!");
        }

        if (room.size == OnexTwo)
        {
            return room.getRight() + Vector2.down;
        }

        return room.getBottomRight() + Vector2.down;
    }

    private Vector2 getLeftBottomNeighborPosition(Room room)
    {
        if (room.size != TwoxOne && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use getLeftNeighborPosition(Room) instead!");
        }

        if (room.size == TwoxOne)
        {
            return room.getBottom() + Vector2.left;
        }

        return room.getBottomLeft() + Vector2.left;
    }

    private Vector2 getLeftNeighborPosition(Room room)
    {
        if (room.size != OnexOne && room.size != OnexTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use getLeftBottom/TopNeighborPosition(Room) instead!");
        }

        if (room.size == OnexOne)
        {
            return room.getMiddle() + Vector2.left;
        }

        return room.getLeft() + Vector2.left;
    }

    private Vector2 getLeftTopNeighborPosition(Room room)
    {
        if (room.size != TwoxOne && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use getLeftNeighborPosition(Room) instead!");
        }

        return room.topLeftInnerLocation + Vector2.left;
    }

    private Vector2 getRightBottomNeighborPosition(Room room)
    {
        if (room.size != TwoxOne && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use getRightNeighborPosition(Room) instead!");
        }

        if (room.size == TwoxOne)
        {
            return room.getBottom() + Vector2.right;
        }

        return room.getBottomRight() + Vector2.right;
    }

    private Vector2 getRightNeighborPosition(Room room)
    {
        if (room.size != OnexOne && room.size != OnexTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use getRightBottom/TopNeighborPosition(Room) instead!");
        }

        if (room.size == OnexOne)
        {
            return room.getMiddle() + Vector2.right;
        }

        return room.getRight() + Vector2.right;
    }

    private Vector2 getRightTopNeighborPosition(Room room)
    {
        if (room.size != TwoxOne && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use getRightNeighborPosition(Room) instead!");
        }

        if (room.size == TwoxOne)
        {
            return room.getTop() + Vector2.right;
        }

        return room.getTopRight() + Vector2.right;
    }

    private Vector2 getTopLeftNeighborPosition(Room room)
    {
        if (room.size != OnexTwo && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use getTopNeighborPosition(Room) instead!");
        }

        return room.topLeftInnerLocation + Vector2.up;
    }

    private Vector2 getTopNeighborPosition(Room room)
    {
        if (room.size != OnexOne && room.size != TwoxOne && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use getTopLeft/RightNeighborPosition(Room) instead!");
        }

        if (room.size == OnexOne)
        {
            return room.getMiddle() + Vector2.up;
        }

        return room.getTop() + Vector2.up;
    }

    private Vector2 getTopRightNeighborPosition(Room room)
    {
        if (room.size != OnexTwo && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use getTopNeighborPosition(Room) instead!");
        }

        if (room.size == OnexTwo)
        {
            return room.getRight() + Vector2.up;
        }

        return room.getTopRight() + Vector2.up;
    }

    private bool hasBottomLeftNeighbor(Room room)
    {
        if (room.size != OnexTwo && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasBottomNeighbor(Room) instead!");
        }

        if (room.size == OnexTwo)
        {
            return takenPos.Contains(room.getLeft() + Vector2.down);
        }

        return takenPos.Contains(room.getBottomLeft() + Vector2.down);
    }

    private bool hasBottomNeighbor(Room room)
    {
        if (room.size != OnexOne && room.size != TwoxOne && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasBottomLeft/RightNeighbor(Room) instead!");
        }

        if (room.size == OnexOne)
        {
            return takenPos.Contains(room.getMiddle() + Vector2.down);
        }

        return takenPos.Contains(room.getBottom() + Vector2.down);
    }

    private bool hasBottomRightNeighbor(Room room)
    {
        if (room.size != OnexTwo && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasBottomNeighbor(Room) instead!");
        }

        if (room.size == OnexTwo)
        {
            return takenPos.Contains(room.getRight() + Vector2.down);
        }

        return takenPos.Contains(room.getBottomRight() + Vector2.down);

    }

    private bool hasLeftBottomNeighbor(Room room)
    {
        if (room.size != TwoxOne && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasLeftNeighbor(Room) instead!");
        }

        if (room.size == TwoxOne)
        {
            return takenPos.Contains(room.getBottom() + Vector2.left);
        }

        return takenPos.Contains(room.getBottomLeft() + Vector2.left);
    }

    private bool hasLeftNeighbor(Room room)
    {
        if (room.size != OnexOne && room.size != OnexTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasLeftTop/BottomNeighbor(Room) instead!");
        }

        if (room.size == OnexOne)
        {
            return takenPos.Contains(room.getMiddle() + Vector2.left);
        }

        return takenPos.Contains(room.getLeft() + Vector2.left);
    }

    private bool hasLeftTopNeighbor(Room room)
    {
        if (room.size != TwoxOne && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasLeftNeighbor(Room) instead!");
        }

        return takenPos.Contains(room.topLeftInnerLocation + Vector2.left);
    }

    private bool hasRightBottomNeighbor(Room room)
    {
        if (room.size != TwoxOne && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasRightNeighbor(Room) instead!");
        }

        if (room.size == TwoxOne)
        {
            return takenPos.Contains(room.getBottom() + Vector2.right);
        }

        return takenPos.Contains(room.getBottomRight() + Vector2.right);
    }

    private bool hasRightNeighbor(Room room)
    {
        if (room.size != OnexOne && room.size != OnexTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasRightTop/BottomNeighbor(Room) instead!");
        }

        if (room.size == OnexOne)
        {
            return takenPos.Contains(room.getMiddle() + Vector2.right);
        }

        return takenPos.Contains(room.getRight() + Vector2.right);
    }

    private bool hasRightTopNeighbor(Room room)
    {
        if (room.size != TwoxOne && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasRightNeighbor(Room) instead!");
        }

        if (room.size == TwoxOne)
        {
            return takenPos.Contains(room.getTop() + Vector2.right);
        }

        return takenPos.Contains(room.getTopRight() + Vector2.right);
    }

    private bool hasTopLeftNeighbor(Room room)
    {
        if (room.size != OnexTwo && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasTopNeighbor(Room) instead!");
        }

        return takenPos.Contains(room.topLeftInnerLocation + Vector2.up);
    }

    private bool hasTopNeighbor(Room room)
    {
        if (room.size != OnexOne && room.size != TwoxOne && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasTopLeft/RightNeighbor(Room) instead!");
        }

        if (room.size == OnexOne)
        {
            return takenPos.Contains(room.getMiddle() + Vector2.up);
        }

        return takenPos.Contains(room.getTop() + Vector2.up);
    }

    private bool hasTopRightNeighbor(Room room)
    {
        if (room.size != OnexTwo && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasTopNeighbor(Room) instead!");
        }

        if (room.size == OnexTwo)
        {
            return takenPos.Contains(room.getRight() + Vector2.up);
        }

        return takenPos.Contains(room.getTopRight() + Vector2.up);
    }

    private void BuildPrimitives()
    {
        int gridSize = 10;

        for (int i = 0; i < rooms.Count; i++)
        {
            float offsetX = rooms[i].center.x * gridSize;
            float offsetZ = rooms[i].center.y * gridSize;
            int doorCount = getNumNeighbors(rooms[i]);

            if (rooms[i].size == OnexOne)
            {
                GameObject rm = Instantiate(OnexOneRoom, new Vector3(offsetX, 0, offsetZ), Quaternion.identity);
                rooms[i].roomRef = rm;

                if (rooms[i].getDoorBottom())
                {
                    GameObject bottomDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    bottomDoor.transform.parent = rm.transform;
                    bottomDoor.transform.localPosition = new Vector3(0f, 1f, -12f);
                }
                if (rooms[i].getDoorLeft())
                {
                    GameObject leftDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    leftDoor.transform.parent = rm.transform;
                    leftDoor.transform.localPosition = new Vector3(-12f, 1f, 0f);
                    leftDoor.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
                }
                if (rooms[i].getDoorRight())
                {
                    GameObject rightDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    rightDoor.transform.parent = rm.transform;
                    rightDoor.transform.localPosition = new Vector3(12f, 1f, 0f);
                    rightDoor.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
                }
                if (rooms[i].getDoorTop())
                {
                    GameObject topDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    topDoor.transform.parent = rm.transform;
                    topDoor.transform.localPosition = new Vector3(0f, 1f, 12f);
                }

                rm.transform.parent = map;
                FillNavBaker(rm);
            }
            else if (rooms[i].size == OnexTwo)
            {
                GameObject rm = Instantiate(OnexTwoRoom, new Vector3(offsetX, 0, offsetZ), Quaternion.identity);
                rooms[i].roomRef = rm;

                bool doorBottomLeft = !hasBottomLeftNeighbor(rooms[i]);
                bool doorBottomRight = !hasBottomRightNeighbor(rooms[i]);
                bool doorTopLeft = !hasTopLeftNeighbor(rooms[i]);
                bool doorTopRight = !hasTopRightNeighbor(rooms[i]);

                if (rooms[i].getDoorBottomLeft())
                {
                    GameObject bottomLeftDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    bottomLeftDoor.transform.parent = rm.transform;
                    bottomLeftDoor.transform.localPosition = new Vector3(-12.5f, 1f, -12f);
                }
                if (rooms[i].getDoorBottomRight())
                {
                    GameObject bottomRightDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    bottomRightDoor.transform.parent = rm.transform;
                    bottomRightDoor.transform.localPosition = new Vector3(12.5f, 1f, -12f);
                }
                if (rooms[i].getDoorLeft())
                {
                    GameObject leftDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    leftDoor.transform.parent = rm.transform;
                    leftDoor.transform.localPosition = new Vector3(-24.5f, 1f, 0f);
                    leftDoor.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
                }
                if (rooms[i].getDoorRight())
                {
                    GameObject rightDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    rightDoor.transform.parent = rm.transform;
                    rightDoor.transform.localPosition = new Vector3(24.5f, 1f, 0f);
                    rightDoor.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
                }
                if (rooms[i].getDoorTopLeft())
                {
                    GameObject topLeftDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    topLeftDoor.transform.parent = rm.transform;
                    topLeftDoor.transform.localPosition = new Vector3(-12.5f, 1f, 12f);
                }
                if (rooms[i].getDoorTopRight())
                {
                    GameObject topRightDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    topRightDoor.transform.parent = rm.transform;
                    topRightDoor.transform.localPosition = new Vector3(12.5f, 1f, 12f);
                }

                rm.transform.parent = map;
                FillNavBaker(rm);
            }
            else if (rooms[i].size == TwoxOne)
            {
                GameObject rm = Instantiate(TwoxOneRoom, new Vector3(offsetX, 0, offsetZ), Quaternion.identity);
                rooms[i].roomRef = rm;

                if (rooms[i].getDoorBottom())
                {
                    GameObject bottomDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    bottomDoor.transform.parent = rm.transform;
                    bottomDoor.transform.localPosition = new Vector3(0f, 1f, -24.5f);
                }
                if (rooms[i].getDoorLeftBottom())
                {
                    GameObject leftBottomDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    leftBottomDoor.transform.parent = rm.transform;
                    leftBottomDoor.transform.localPosition = new Vector3(-12f, 1f, -12.5f);
                    leftBottomDoor.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
                }
                if (rooms[i].getDoorLeftTop())
                {
                    GameObject leftTopDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    leftTopDoor.transform.parent = rm.transform;
                    leftTopDoor.transform.localPosition = new Vector3(-12f, 1f, 12.5f);
                    leftTopDoor.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
                }
                if (rooms[i].getDoorRightBottom())
                {
                    GameObject rightBottomDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    rightBottomDoor.transform.parent = rm.transform;
                    rightBottomDoor.transform.localPosition = new Vector3(12f, 1f, -12.5f);
                    rightBottomDoor.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
                }
                if (rooms[i].getDoorRightTop())
                {
                    GameObject rightTopDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    rightTopDoor.transform.parent = rm.transform;
                    rightTopDoor.transform.localPosition = new Vector3(12f, 1f, 12.5f);
                    rightTopDoor.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
                }
                if (rooms[i].getDoorTop())
                {
                    GameObject topDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    topDoor.transform.parent = rm.transform;
                    topDoor.transform.localPosition = new Vector3(0f, 1f, 24.5f);
                }

                rm.transform.parent = map;
                FillNavBaker(rm);
            }
            else if (rooms[i].size == TwoxTwo)
            {
                GameObject rm = Instantiate(TwoxTwoRoom, new Vector3(offsetX, 0, offsetZ), Quaternion.identity);
                rooms[i].roomRef = rm;

                if (rooms[i].getDoorBottomLeft())
                {
                    GameObject bottomLeftDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    bottomLeftDoor.transform.parent = rm.transform;
                    bottomLeftDoor.transform.localPosition = new Vector3(-12.5f, 1f, -24.5f);
                }
                if (rooms[i].getDoorBottomRight())
                {
                    GameObject bottomRightDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    bottomRightDoor.transform.parent = rm.transform;
                    bottomRightDoor.transform.localPosition = new Vector3(12.5f, 1f, -24.5f);
                }
                if (rooms[i].getDoorLeftBottom())
                {
                    GameObject leftBottomDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    leftBottomDoor.transform.parent = rm.transform;
                    leftBottomDoor.transform.localPosition = new Vector3(-24.5f, 1f, -12.5f);
                    leftBottomDoor.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
                }
                if (rooms[i].getDoorLeftTop())
                {
                    GameObject leftTopDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    leftTopDoor.transform.parent = rm.transform;
                    leftTopDoor.transform.localPosition = new Vector3(-24.5f, 1f, 12.5f);
                    leftTopDoor.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
                }
                if (rooms[i].getDoorRightBottom())
                {
                    GameObject rightBottomDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    rightBottomDoor.transform.parent = rm.transform;
                    rightBottomDoor.transform.localPosition = new Vector3(24.5f, 1f, -12.5f);
                    rightBottomDoor.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
                }
                if (rooms[i].getDoorRightTop())
                {
                    GameObject rightTopDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    rightTopDoor.transform.parent = rm.transform;
                    rightTopDoor.transform.localPosition = new Vector3(24.5f, 1f, 12.5f);
                    rightTopDoor.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
                }
                if (rooms[i].getDoorTopLeft())
                {
                    GameObject topLeftDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    topLeftDoor.transform.parent = rm.transform;
                    topLeftDoor.transform.localPosition = new Vector3(-12.5f, 1f, 24.5f);
                }
                if (rooms[i].getDoorTopRight())
                {
                    GameObject topRightDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    topRightDoor.transform.parent = rm.transform;
                    topRightDoor.transform.localPosition = new Vector3(12.5f, 1f, 24.5f);
                }

                rm.transform.parent = map;
                FillNavBaker(rm);
            }
            else
            {
                GameObject rm = Instantiate(ThreexThreeRoom, new Vector3(offsetX, 0, offsetZ), Quaternion.identity);
                rooms[i].roomRef = rm;

                if (rooms[i].getDoorBottomLeft())
                {
                    GameObject bottomLeftDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    bottomLeftDoor.transform.parent = rm.transform;
                    bottomLeftDoor.transform.localPosition = new Vector3(-25f, 1f, -37f);
                }
                if (rooms[i].getDoorBottom())
                {
                    GameObject bottomDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    bottomDoor.transform.parent = rm.transform;
                    bottomDoor.transform.localPosition = new Vector3(0f, 1f, -37f);
                }
                if (rooms[i].getDoorBottomRight())
                {
                    GameObject bottomRightDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    bottomRightDoor.transform.parent = rm.transform;
                    bottomRightDoor.transform.localPosition = new Vector3(25f, 1f, -37f);
                }
                if (rooms[i].getDoorLeftBottom())
                {
                    GameObject leftBottomDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    leftBottomDoor.transform.parent = rm.transform;
                    leftBottomDoor.transform.localPosition = new Vector3(-37f, 1f, -25f);
                    leftBottomDoor.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
                }
                if (rooms[i].getDoorLeft())
                {
                    GameObject leftDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    leftDoor.transform.parent = rm.transform;
                    leftDoor.transform.localPosition = new Vector3(-37f, 1f, 0f);
                    leftDoor.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
                }
                if (rooms[i].getDoorLeftTop())
                {
                    GameObject leftTopDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    leftTopDoor.transform.parent = rm.transform;
                    leftTopDoor.transform.localPosition = new Vector3(-37f, 1f, 25f);
                    leftTopDoor.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
                }
                if (rooms[i].getDoorRightBottom())
                {
                    GameObject rightBottomDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    rightBottomDoor.transform.parent = rm.transform;
                    rightBottomDoor.transform.localPosition = new Vector3(37f, 1f, -25f);
                    rightBottomDoor.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
                }
                if (rooms[i].getDoorRight())
                {
                    GameObject rightDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    rightDoor.transform.parent = rm.transform;
                    rightDoor.transform.localPosition = new Vector3(37f, 1f, 0f);
                    rightDoor.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
                }
                if (rooms[i].getDoorRightTop())
                {
                    GameObject rightTopDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    rightTopDoor.transform.parent = rm.transform;
                    rightTopDoor.transform.localPosition = new Vector3(37f, 1f, 25f);
                    rightTopDoor.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
                }
                if (rooms[i].getDoorTopLeft())
                {
                    GameObject topLeftDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    topLeftDoor.transform.parent = rm.transform;
                    topLeftDoor.transform.localPosition = new Vector3(-25f, 1f, 37f);
                }
                if (rooms[i].getDoorTop())
                {
                    GameObject topDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    topDoor.transform.parent = rm.transform;
                    topDoor.transform.localPosition = new Vector3(0f, 1f, 37f);
                }
                if (rooms[i].getDoorTopRight())
                {
                    GameObject topRightDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    topRightDoor.transform.parent = rm.transform;
                    topRightDoor.transform.localPosition = new Vector3(25f, 1f, 37f);
                }

                rm.transform.parent = map;
                FillNavBaker(rm);
            }
        }

        map.transform.position = Vector3.zero;
        SetCharToMap();
    }

    private void FillNavBaker(GameObject rm)
    {
        baker.surfaces.Add(rm.GetComponent<NavMeshSurface>());
    }

    private void SetCharToMap()
    {
        int rand = Random.Range(0, numOfRoomsInitial - 1);
        Room spawnRoom = rooms[rand];
        spawnRoom.type = "spawn";
        character.transform.position = spawnRoom.getRandomPosition();
    }

    public List<Room> getAllRooms()
    {
        return rooms;
    }

    private void SpawnEnemies()
    {
        spawner.spawnAllEnemies();
    }
}
