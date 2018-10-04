using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

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
    private NavigationBaker baker;

    public GameObject tempEnemy;

    public int areaSizeX = 5; //Size of the grid on the x axis
    public int areaSizeY = 5; //Size of the grid on the y axis
    [SerializeField] int numOfRooms = 20; //Number of rooms to add to the grid

    [SerializeField] float startBranchProb = 1.0f; //Branch probability when the first rooms are being created
    [SerializeField] float endBranchProb = 0.01f; //Branch probability when the last rooms are being created
    private float branchProb; //Actual branch probability that gets decreased/increased over the course of adding all of the rooms
    private float changeInProb; //The difference between startBranchProb and endBranchProb
    private bool decreasing; //Whether the branchProb is decreasing or increasing

    //List of all rooms
    private List<Room> rooms = new List<Room>();
    //List of all rooms that have at least one open neighboring position
    //TODO: Sort by num of neighbors
    private List<Room> openRooms = new List<Room>();
    //List of all rooms that have at most one neighboring position
    private List<Room> singleNeighborRooms = new List<Room>();

    //List of all occupied locations in the area
    private List<Vector2> takenPos = new List<Vector2>();

    //Useful Vectors
    private Vector2 OnexOne = new Vector2(1f, 1f);
    private Vector2 OnexTwo = new Vector2(1f, 2f);
    private Vector2 TwoxOne = new Vector2(2f, 1f);
    private Vector2 TwoxTwo = new Vector2(2f, 2f);
    private Vector2 ThreexThree = new Vector2(3f, 3f);

    //Placeholder vector in case a randomBranchPosition can't be found in a timely manner
    private Vector2 errorVector = new Vector2(3.14f, 3.14f);
    private Room errorRoom;

    //Initilization
    void Start()
    {
        errorRoom = new Room(errorVector);

        //If there are more rooms than can fit in the grid
        if (numOfRooms >= (areaSizeX * areaSizeY))
        {
            numOfRooms = Mathf.RoundToInt(areaSizeX * areaSizeY);
        }

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

        CreateRooms();
        //CreateManualRooms();
        BuildPrimitives();
    }

    private void CreateManualRooms()
    {
        Room startRoom = new Room(new Vector2(areaSizeX / 2, areaSizeY / 2), 0);
        rooms.Insert(0, startRoom);
        takenPos.Insert(0, startRoom.location);
        openRooms.Insert(0, startRoom);
        singleNeighborRooms.Insert(0, startRoom);

        Room newRoom = new Room(startRoom.location + Vector2.up);
        rooms.Insert(0, newRoom);

        addLocationsToTakenPos(newRoom);
        setRoomDoors(newRoom);
        setNeighboringRooms(newRoom);

        if (getNumNeighbors(newRoom) < newRoom.maxNeighbors)
        {
            openRooms.Insert(0, newRoom);
        }
        if (getNumNeighbors(newRoom) <= 1)
        {
            singleNeighborRooms.Insert(0, newRoom);
        }

        removeNotOpenRooms(newRoom);
        removeNotSingleNeighborRooms(newRoom);
    }

    //Populates the "rooms" array with rooms
    private void CreateRooms()
    {
        //Add starter room in middle
        //TODO: Different type for starter room (Change 0 to another number)
        Room startRoom = new Room(new Vector2(areaSizeX / 2, areaSizeY / 2), 0);
        rooms.Insert(0, startRoom);
        takenPos.Insert(0, startRoom.location);
        openRooms.Insert(0, startRoom);
        singleNeighborRooms.Insert(0, startRoom);

        //Room startRoom = new Room(new Vector2(areaSizeX / 2, areaSizeY / 2), ThreexThree, 0);
        //rooms.Insert(0, startRoom);
        //addLocationsToTakenPos(startRoom);
        //openRooms.Insert(0, startRoom);
        //singleNeighborRooms.Insert(0, startRoom);

        //Add each room to the grid
        for (int i = 0; i < numOfRooms - 1; i++)
        {
            //Determine type and size of new Room (somehow)
            int tempType = 0;
            Vector2 tempSize = OnexOne;

            //Get temp position of new room
            Vector2 tempLoc = getRandomPosition();
            Room tempRoom = new Room(tempLoc, tempSize, tempType);

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
                Room tempBranchRoom = new Room(tempBranchLoc, tempSize, tempType);

                //If it isn't the error vector, a branch position was found and it will be the position of the new room
                if (tempBranchLoc != errorVector)
                {
                    tempLoc = tempBranchLoc;
                    tempNeighbors = getNumNeighbors(tempBranchRoom);
                }
            }

            //Actually insert the room
            Room newRoom = new Room(tempLoc, tempSize, tempType);
            rooms.Insert(0, newRoom);

            addLocationsToTakenPos(newRoom);
            setRoomDoors(newRoom);
            setNeighboringRooms(newRoom);

            if (tempNeighbors < newRoom.maxNeighbors)
            {
                openRooms.Insert(0, newRoom);
            }
            if (tempNeighbors <= 1)
            {
                singleNeighborRooms.Insert(0, newRoom);
            }

            removeNotOpenRooms(newRoom);
            removeNotSingleNeighborRooms(newRoom);

            //**Test with manual additions
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
            takenPos.Insert(0, room.locations[i]);
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
            else
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
            else
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
            else if (neighbor.locations.Contains(room.getBottom() + Vector2.down))
            {
                room.setRoomBottom(neighbor);
            }
            else
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
            else
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
            else
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
            else if (neighbor.locations.Contains(room.getLeft() + Vector2.left))
            {
                room.setRoomLeft(neighbor);
            }
            else
            {
                room.setRoomTopLeft(neighbor);
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
            else
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
            else
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
            else if (neighbor.locations.Contains(room.getRight() + Vector2.right))
            {
                room.setRoomRight(neighbor);
            }
            else
            {
                room.setRoomTopRight(neighbor);
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
            else
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
            else
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
            else if (neighbor.locations.Contains(room.getTop() + Vector2.up))
            {
                room.setRoomTop(neighbor);
            }
            else
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

            if (singleNeighborRooms.Contains(tempBottom) && getNumNeighbors(tempBottom) > 1)
            {
                singleNeighborRooms.Remove(tempBottom);
            }
            if (singleNeighborRooms.Contains(tempLeft) && getNumNeighbors(tempLeft) > 1)
            {
                singleNeighborRooms.Remove(tempLeft);
            }
            if (singleNeighborRooms.Contains(tempRight) && getNumNeighbors(tempRight) > 1)
            {
                singleNeighborRooms.Remove(tempRight);
            }
            if (singleNeighborRooms.Contains(tempTop) && getNumNeighbors(tempTop) > 1)
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

            if (singleNeighborRooms.Contains(tempBottomLeft) && getNumNeighbors(tempBottomLeft) > 1)
            {
                singleNeighborRooms.Remove(tempBottomLeft);
            }
            if (singleNeighborRooms.Contains(tempBottomRight) && getNumNeighbors(tempBottomRight) > 1)
            {
                singleNeighborRooms.Remove(tempBottomRight);
            }
            if (singleNeighborRooms.Contains(tempLeft) && getNumNeighbors(tempLeft) > 1)
            {
                singleNeighborRooms.Remove(tempLeft);
            }
            if (singleNeighborRooms.Contains(tempRight) && getNumNeighbors(tempRight) > 1)
            {
                singleNeighborRooms.Remove(tempRight);
            }
            if (singleNeighborRooms.Contains(tempTopLeft) && getNumNeighbors(tempTopLeft) > 1)
            {
                singleNeighborRooms.Remove(tempTopLeft);
            }
            if (singleNeighborRooms.Contains(tempTopRight) && getNumNeighbors(tempTopRight) > 1)
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

            if (singleNeighborRooms.Contains(tempBottom) && getNumNeighbors(tempBottom) > 1)
            {
                singleNeighborRooms.Remove(tempBottom);
            }
            if (singleNeighborRooms.Contains(tempLeftBottom) && getNumNeighbors(tempLeftBottom) > 1)
            {
                singleNeighborRooms.Remove(tempLeftBottom);
            }
            if (singleNeighborRooms.Contains(tempLeftTop) && getNumNeighbors(tempLeftTop) > 1)
            {
                singleNeighborRooms.Remove(tempLeftTop);
            }
            if (singleNeighborRooms.Contains(tempRightBottom) && getNumNeighbors(tempRightBottom) > 1)
            {
                singleNeighborRooms.Remove(tempRightBottom);
            }
            if (singleNeighborRooms.Contains(tempRightTop) && getNumNeighbors(tempRightTop) > 1)
            {
                singleNeighborRooms.Remove(tempRightTop);
            }
            if (singleNeighborRooms.Contains(tempTop) && getNumNeighbors(tempTop) > 1)
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

            if (singleNeighborRooms.Contains(tempBottomLeft) && getNumNeighbors(tempBottomLeft) > 1)
            {
                singleNeighborRooms.Remove(tempBottomLeft);
            }
            if (singleNeighborRooms.Contains(tempBottomRight) && getNumNeighbors(tempBottomRight) > 1)
            {
                singleNeighborRooms.Remove(tempBottomRight);
            }
            if (singleNeighborRooms.Contains(tempLeftBottom) && getNumNeighbors(tempLeftBottom) > 1)
            {
                singleNeighborRooms.Remove(tempLeftBottom);
            }
            if (singleNeighborRooms.Contains(tempLeftTop) && getNumNeighbors(tempLeftTop) > 1)
            {
                singleNeighborRooms.Remove(tempLeftTop);
            }
            if (singleNeighborRooms.Contains(tempRightBottom) && getNumNeighbors(tempRightBottom) > 1)
            {
                singleNeighborRooms.Remove(tempRightBottom);
            }
            if (singleNeighborRooms.Contains(tempRightTop) && getNumNeighbors(tempRightTop) > 1)
            {
                singleNeighborRooms.Remove(tempRightTop);
            }
            if (singleNeighborRooms.Contains(tempTopLeft) && getNumNeighbors(tempTopLeft) > 1)
            {
                singleNeighborRooms.Remove(tempTopLeft);
            }
            if (singleNeighborRooms.Contains(tempTopRight) && getNumNeighbors(tempTopRight) > 1)
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

            if (singleNeighborRooms.Contains(tempBottomLeft) && getNumNeighbors(tempBottomLeft) > 1)
            {
                singleNeighborRooms.Remove(tempBottomLeft);
            }
            if (singleNeighborRooms.Contains(tempBottom) && getNumNeighbors(tempBottom) > 1)
            {
                singleNeighborRooms.Remove(tempBottom);
            }
            if (singleNeighborRooms.Contains(tempBottomRight) && getNumNeighbors(tempBottomRight) > 1)
            {
                singleNeighborRooms.Remove(tempBottomRight);
            }
            if (singleNeighborRooms.Contains(tempLeftBottom) && getNumNeighbors(tempLeftBottom) > 1)
            {
                singleNeighborRooms.Remove(tempLeftBottom);
            }
            if (singleNeighborRooms.Contains(tempLeft) && getNumNeighbors(tempLeft) > 1)
            {
                singleNeighborRooms.Remove(tempLeft);
            }
            if (singleNeighborRooms.Contains(tempLeftTop) && getNumNeighbors(tempLeftTop) > 1)
            {
                singleNeighborRooms.Remove(tempLeftTop);
            }
            if (singleNeighborRooms.Contains(tempRightBottom) && getNumNeighbors(tempRightBottom) > 1)
            {
                singleNeighborRooms.Remove(tempRightBottom);
            }
            if (singleNeighborRooms.Contains(tempRight) && getNumNeighbors(tempRight) > 1)
            {
                singleNeighborRooms.Remove(tempRight);
            }
            if (singleNeighborRooms.Contains(tempRightTop) && getNumNeighbors(tempRightTop) > 1)
            {
                singleNeighborRooms.Remove(tempRightTop);
            }
            if (singleNeighborRooms.Contains(tempTopLeft) && getNumNeighbors(tempTopLeft) > 1)
            {
                singleNeighborRooms.Remove(tempTopLeft);
            }
            if (singleNeighborRooms.Contains(tempTop) && getNumNeighbors(tempTop) > 1)
            {
                singleNeighborRooms.Remove(tempTop);
            }
            if (singleNeighborRooms.Contains(tempTopRight) && getNumNeighbors(tempTopRight) > 1)
            {
                singleNeighborRooms.Remove(tempTopRight);
            }
        }





        Room tempBottomRoom = new Room(room.location + Vector2.down, room.type);
        Room tempLeftRoom = new Room(room.location + Vector2.left, room.type);
        Room tempRightRoom = new Room(room.location + Vector2.right, room.type);
        Room tempTopRoom = new Room(room.location + Vector2.up, room.type);

        if (singleNeighborRooms.Contains(tempBottomRoom) && getNumNeighbors(tempBottomRoom) > 1)
        {
            singleNeighborRooms.Remove(tempBottomRoom);
        }
        if (singleNeighborRooms.Contains(tempLeftRoom) && getNumNeighbors(tempLeftRoom) > 1)
        {
            singleNeighborRooms.Remove(tempLeftRoom);
        }
        if (singleNeighborRooms.Contains(tempRightRoom) && getNumNeighbors(tempRightRoom) > 1)
        {
            singleNeighborRooms.Remove(tempRightRoom);
        }
        if (singleNeighborRooms.Contains(tempTopRoom) && getNumNeighbors(tempTopRoom) > 1)
        {
            singleNeighborRooms.Remove(tempTopRoom);
        }
    }

    //Gets a random position that's adjacent to a random room
    private Vector2 getRandomPosition()
    {
        if (openRooms.Count == 0)
        {
            throw new System.Exception("There are no open rooms!");
        }
        Vector2 randomPos;
        bool validRandomPos;
        int index;
        int iterationsDir = 0; //Iterations of the do while loop that selects which direction to deviate from the random room

        do
        {
            //Pick a random room that's already in the grid that doesn't have four neighbors
            index = Mathf.Clamp(Mathf.RoundToInt(Random.value * (openRooms.Count)), 0, openRooms.Count - 1);
            index = Mathf.Clamp(index, 0, openRooms.Count);

            int x = (int)openRooms[index].location.x;
            int y = (int)openRooms[index].location.y;

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
        if (singleNeighborRooms.Count == 0)
        {
            return errorVector;
        }

        Vector2 randomPos;
        bool validRandomPos;
        int index;
        int iterationsMain = 0; //Iterations of the main do while loop
        int iterationsDir = 0; //Iterations of the do while loop that selects which direction to deviate from the random room

        do
        {
            //Pick a random room that's already in the grid that has only one neighbor
            index = Mathf.Clamp(Mathf.RoundToInt(Random.value * (singleNeighborRooms.Count)), 0, singleNeighborRooms.Count - 1);
            index = Mathf.Clamp(index, 0, singleNeighborRooms.Count);

            int x = (int)singleNeighborRooms[index].location.x;
            int y = (int)singleNeighborRooms[index].location.y;

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

            if (!doorBottomLeft && !doorBottomRight && room.getRoomBottomLeft().Equals(room.getRoomBottomRight()))
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
            else
            {
                room.setDoorBottomLeft(doorBottomLeft);
                room.setDoorBottomRight(doorBottomRight);
            }
            if (!doorTopLeft && !doorTopRight && room.getRoomTopLeft().Equals(room.getRoomTopRight()))
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

            if (!doorLeftBottom && !doorLeftTop && room.getRoomLeftBottom().Equals(room.getRoomLeftTop()))
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
            else
            {
                room.setDoorLeftBottom(doorLeftBottom);
                room.setDoorLeftTop(doorLeftTop);
            }
            if (!doorRightBottom && !doorRightTop && room.getRoomRightBottom().Equals(room.getRoomRightTop()))
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

            if (!doorBottomLeft && !doorBottomRight && room.getRoomBottomLeft().Equals(room.getRoomBottomRight()))
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
            else
            {
                room.setDoorBottomLeft(doorBottomLeft);
                room.setDoorBottomRight(doorBottomRight);
            }
            if (!doorLeftBottom && !doorLeftTop && room.getRoomLeftBottom().Equals(room.getRoomLeftTop()))
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
            else
            {
                room.setDoorLeftBottom(doorLeftBottom);
                room.setDoorLeftTop(doorLeftTop);
            }
            if (!doorRightBottom && !doorRightTop && room.getRoomRightBottom().Equals(room.getRoomRightTop()))
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
            else
            {
                room.setDoorRightBottom(doorRightBottom);
                room.setDoorRightTop(doorRightTop);
            }
            if (!doorTopLeft && !doorTopRight && room.getRoomTopLeft().Equals(room.getRoomTopRight()))
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
            else
            {
                room.setDoorTopLeft(doorTopLeft);
                room.setDoorTopRight(doorTopRight);
            }
        }
        else
        {
            //Check all 3, then left middle, then right middle
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
                && room.getRoomBottomLeft().Equals(room.getRoomBottom()) 
                && room.getRoomBottom().Equals(room.getRoomBottomRight()))
            {
                room.setDoorBottomLeft(true);
                room.setDoorBottom(false);
                room.setDoorBottomRight(true);
            }
            else if (!doorBottomLeft && !doorBottom && room.getRoomBottomLeft().Equals(room.getRoomBottom()))
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
                room.setDoorBottomRight(doorBottomRight);
            }
            else if (!doorBottom && !doorBottomRight && room.getRoomBottom().Equals(room.getRoomBottomRight()))
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
                room.setDoorBottomLeft(doorBottomLeft);
            }
            else
            {
                room.setDoorBottomLeft(doorBottomLeft);
                room.setDoorBottom(doorBottom);
                room.setDoorBottomRight(doorBottomRight);
            }

            if (!doorLeftBottom && !doorLeft && !doorLeftTop
                && room.getRoomLeftBottom().Equals(room.getRoomLeft())
                && room.getRoomLeft().Equals(room.getRoomLeftTop()))
            {
                room.setDoorLeftBottom(true);
                room.setDoorLeft(false);
                room.setDoorLeftTop(true);
            }
            else if (!doorLeftBottom && !doorLeft && room.getLeftBottom().Equals(room.getRoomLeft()))
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
                room.setDoorLeftTop(doorLeftTop);
            }
            else if (!doorLeft && !doorLeftTop && room.getRoomLeft().Equals(room.getRoomLeftTop()))
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
                room.setDoorLeftBottom(doorLeftBottom);
            }
            else
            {
                room.setDoorLeftBottom(doorLeftBottom);
                room.setDoorLeft(doorLeft);
                room.setDoorLeftTop(doorLeftTop);
            }

            if (!doorRightBottom && !doorRight && !doorRightTop
                && room.getRoomRightBottom().Equals(room.getRoomRight())
                && room.getRoomRight().Equals(room.getRoomRightTop()))
            {
                room.setDoorRightBottom(true);
                room.setDoorRight(false);
                room.setDoorRightTop(true);
            }
            else if (!doorRightBottom && !doorRight && room.getRightBottom().Equals(room.getRoomRight()))
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
                room.setDoorRightTop(doorRightTop);
            }
            else if (!doorRight && !doorRightTop && room.getRoomRight().Equals(room.getRoomRightTop()))
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
                room.setDoorRightBottom(doorRightBottom);
            }
            else
            {
                room.setDoorRightBottom(doorRightBottom);
                room.setDoorRight(doorRight);
                room.setDoorRightTop(doorRightTop);
            }

            if (!doorTopLeft && !doorTop && !doorTopRight
                && room.getRoomTopLeft().Equals(room.getRoomTop())
                && room.getRoomTop().Equals(room.getRoomTopRight()))
            {
                room.setDoorTopLeft(true);
                room.setDoorTop(false);
                room.setDoorTopRight(true);
            }
            else if (!doorTopLeft && !doorTop && room.getRoomTopLeft().Equals(room.getRoomTop()))
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
                room.setDoorTopRight(doorTopRight);
            }
            else if (!doorTop && !doorTopRight && room.getRoomTop().Equals(room.getRoomTopRight()))
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

    private bool hasBottomLeftNeighbor(Room room)
    {
        if (room.size != OnexTwo && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasBottomNeighbor(Room) instead!");
        }

        if (room.size == OnexTwo)
        {
            return takenPos.Contains(room.location + Vector2.down);
        }

        if (room.size == TwoxTwo)
        {
            return takenPos.Contains(room.location + (2 * Vector2.down));
        }

        return takenPos.Contains(room.location + (3 * Vector2.down));
    }

    private bool hasBottomNeighbor(Room room)
    {
        if (room.size != OnexOne && room.size != TwoxOne && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasBottomLeft/RightNeighbor(Room) instead!");
        }

        if (room.size == OnexOne)
        {
            return takenPos.Contains(room.location + Vector2.down);
        }

        if (room.size == TwoxOne)
        {
            return takenPos.Contains(room.location + (2 * Vector2.down));
        }

        return takenPos.Contains(room.location + (3 * Vector2.down) + Vector2.right);
    }

    private bool hasBottomRightNeighbor(Room room)
    {
        if (room.size != OnexTwo && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasBottomNeighbor(Room) instead!");
        }

        if (room.size == OnexTwo)
        {
            return takenPos.Contains(room.location + Vector2.down + Vector2.right);
        }

        if (room.size == TwoxTwo)
        {
            return takenPos.Contains(room.location + (2 * Vector2.down) + Vector2.right);
        }

        return takenPos.Contains(room.location + (3 * Vector2.down) + (2 * Vector2.right));
    }

    private bool hasLeftBottomNeighbor(Room room)
    {
        if (room.size != TwoxOne && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasLeftNeighbor(Room) instead!");
        }

        if (room.size == TwoxOne || room.size == TwoxTwo)
        {
            return takenPos.Contains(room.location + Vector2.down + Vector2.left);
        }

        return takenPos.Contains(room.location + (2 * Vector2.down) + Vector2.left);
    }

    private bool hasLeftNeighbor(Room room)
    {
        if (room.size != OnexOne && room.size != OnexTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasLeftTop/BottomNeighbor(Room) instead!");
        }

        if (room.size == OnexOne || room.size == OnexTwo)
        {
            return takenPos.Contains(room.location + Vector2.left);
        }

        return takenPos.Contains(room.location + Vector2.down + Vector2.left);
    }

    private bool hasLeftTopNeighbor(Room room)
    {
        if (room.size != TwoxOne && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasLeftNeighbor(Room) instead!");
        }

        return takenPos.Contains(room.location + Vector2.left);
    }

    private bool hasRightBottomNeighbor(Room room)
    {
        if (room.size != TwoxOne && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasRightNeighbor(Room) instead!");
        }

        if (room.size == TwoxOne)
        {
            return takenPos.Contains(room.location + Vector2.down + Vector2.right);
        }

        if (room.size == TwoxTwo)
        {
            return takenPos.Contains(room.location + Vector2.down + (2 * Vector2.right));
        }
        
        return takenPos.Contains(room.location + (2 * Vector2.down) + (3 * Vector2.right));
    }

    private bool hasRightNeighbor(Room room)
    {
        if (room.size != OnexOne && room.size != OnexTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasRightTop/BottomNeighbor(Room) instead!");
        }

        if (room.size == OnexOne)
        {
            return takenPos.Contains(room.location + Vector2.right);
        }

        if (room.size == OnexTwo)
        {
            return takenPos.Contains(room.location + (2 * Vector2.right));
        }

        return takenPos.Contains(room.location + Vector2.down + (3 * Vector2.right));
    }

    private bool hasRightTopNeighbor(Room room)
    {
        if (room.size != TwoxOne && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasRightNeighbor(Room) instead!");
        }

        if (room.size == TwoxOne)
        {
            return takenPos.Contains(room.location + Vector2.right);
        }

        if (room.size == TwoxTwo)
        {
            return takenPos.Contains(room.location + (2 * Vector2.right));
        }

        return takenPos.Contains(room.location + (3 * Vector2.right));
    }

    private bool hasTopLeftNeighbor(Room room)
    {
        if (room.size != OnexTwo && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasTopNeighbor(Room) instead!");
        }

        return takenPos.Contains(room.location + Vector2.up);
    }

    private bool hasTopNeighbor(Room room)
    {
        if (room.size != OnexOne && room.size != TwoxOne && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasTopLeft/RightNeighbor(Room) instead!");
        }

        if (room.size == OnexOne || room.size == TwoxOne)
        {
            return takenPos.Contains(room.location + Vector2.up);
        }

        return takenPos.Contains(room.location + Vector2.right + Vector2.up);
    }

    private bool hasTopRightNeighbor(Room room)
    {
        if (room.size != OnexTwo && room.size != TwoxTwo && room.size != ThreexThree)
        {
            throw new System.ArgumentException("Use hasTopNeighbor(Room) instead!");
        }

        if (room.size == OnexTwo || room.size == TwoxTwo)
        {
            return takenPos.Contains(room.location + Vector2.right + Vector2.up);
        }

        return takenPos.Contains(room.location + (2 * Vector2.right) + Vector2.up);
    }

    private void BuildPrimitives()
    {
        int gridSize = 10;

        for (int i = 0; i < rooms.Count; i++)
        {
            float offsetX = rooms[i].location.x * gridSize;
            float offsetZ = rooms[i].location.y * gridSize;
            int doorCount = getNumNeighbors(rooms[i]);

            if (rooms[i].size == OnexOne)
            {
                GameObject rm = Instantiate(OnexOneRoom, new Vector3(offsetX, 0, offsetZ), Quaternion.identity);

                if (rooms[i].getDoorBottom())
                {
                    GameObject bottomDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    bottomDoor.transform.parent = rm.transform;
                    bottomDoor.transform.localPosition = new Vector3(0f, 1f, -24.5f);
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
                if (rooms[i].getDoorTop())
                {
                    GameObject topDoor = Instantiate(RoomDoor, rm.transform.position, Quaternion.identity);
                    topDoor.transform.parent = rm.transform;
                    topDoor.transform.localPosition = new Vector3(0f, 1f, 24.5f);
                }

                rm.transform.parent = map;
                FillNavBaker(rm);
            }
            else if (rooms[i].size == OnexTwo)
            {
                GameObject rm = Instantiate(OnexTwoRoom, new Vector3(offsetX, 0, offsetZ), Quaternion.identity);

                if (rooms[i].getDoorBottomLeft())
                {

                }
                if (rooms[i].getDoorBottomRight())
                {

                }
                if (rooms[i].getDoorLeft())
                {

                }
                if (rooms[i].getDoorRight())
                {

                }
                if (rooms[i].getDoorTopLeft())
                {

                }
                if (rooms[i].getDoorTopRight())
                {

                }

                rm.transform.parent = map;
                FillNavBaker(rm);
            }
            else if (rooms[i].size == TwoxOne)
            {
                GameObject rm = Instantiate(TwoxOneRoom, new Vector3(offsetX, 0, offsetZ), Quaternion.identity);

                if (rooms[i].getDoorBottom())
                {

                }
                if (rooms[i].getDoorLeftBottom())
                {

                }
                if (rooms[i].getDoorLeftTop())
                {

                }
                if (rooms[i].getDoorRightBottom())
                {

                }
                if (rooms[i].getDoorRightTop())
                {

                }
                if (rooms[i].getDoorTop())
                {

                }

                rm.transform.parent = map;
                FillNavBaker(rm);
            }
            else if (rooms[i].size == TwoxTwo)
            {
                GameObject rm = Instantiate(OnexTwoRoom, new Vector3(offsetX, 0, offsetZ), Quaternion.identity);

                if (rooms[i].getDoorBottomLeft())
                {

                }
                if (rooms[i].getDoorBottomRight())
                {

                }
                if (rooms[i].getDoorLeftBottom())
                {

                }
                if (rooms[i].getDoorLeftTop())
                {

                }
                if (rooms[i].getDoorRightBottom())
                {

                }
                if (rooms[i].getDoorRightTop())
                {

                }
                if (rooms[i].getDoorTopLeft())
                {

                }
                if (rooms[i].getDoorTopRight())
                {

                }
            }
            else
            {
                GameObject rm = Instantiate(OnexTwoRoom, new Vector3(offsetX, 0, offsetZ), Quaternion.identity);

                if (rooms[i].getDoorBottomLeft())
                {

                }
                if (rooms[i].getDoorBottom())
                {

                }
                if (rooms[i].getDoorBottomRight())
                {

                }
                if (rooms[i].getDoorLeftBottom())
                {

                }
                if (rooms[i].getDoorLeft())
                {

                }
                if (rooms[i].getDoorLeftTop())
                {

                }
                if (rooms[i].getDoorRightBottom())
                {

                }
                if (rooms[i].getDoorRight())
                {

                }
                if (rooms[i].getDoorRightTop())
                {

                }
                if (rooms[i].getDoorTopLeft())
                {

                }
                if (rooms[i].getDoorTop())
                {

                }
                if (rooms[i].getDoorTopRight())
                {

                }
            }
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

        tempEnemy.transform.position = baker.surfaces[Random.Range(0, baker.surfaces.Count)].gameObject.transform.position;
    }
}
