﻿using System.Collections;
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
        BuildPrimitives();
    }

    //Populates the "rooms" array with rooms
    private void CreateRooms()
    {
        //Add starter room in middle
        //TODO: Different type for starter room (Change 0 to another number)
        //Room startRoom = new Room(new Vector2(areaSizeX / 2, areaSizeY / 2), 0);
        //rooms.Insert(0, startRoom);
        //takenPos.Insert(0, startRoom.location);
        //openRooms.Insert(0, startRoom);
        //singleNeighborRooms.Insert(0, startRoom);

        Room startRoom = new Room(new Vector2(areaSizeX / 2, areaSizeY / 2), ThreexThree, 0);
        rooms.Insert(0, startRoom);
        addLocationsToTakenPos(startRoom);
        openRooms.Insert(0, startRoom);
        singleNeighborRooms.Insert(0, startRoom);

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
        Vector2 randomPos;
        bool validRandomPos;
        int index;
        int iterationsDir = 0; //Iterations of the do while loop that selects which direction to deviate from the random room

        do
        {
            //Pick a random room that's already in the grid that doesn't have four neighbors
            index = Mathf.RoundToInt(Random.value * (openRooms.Count - 1));

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
        Vector2 randomPos;
        bool validRandomPos;
        int index;
        int iterationsMain = 0; //Iterations of the main do while loop
        int iterationsDir = 0; //Iterations of the do while loop that selects which direction to deviate from the random room

        do
        {
            //Pick a random room that's already in the grid that has only one neighbor
            index = Mathf.RoundToInt(Random.value * (singleNeighborRooms.Count - 1));

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
            room.setDoorBottom(hasBottomNeighbor(room));
            room.setDoorLeft(hasLeftNeighbor(room));
            room.setDoorRight(hasRightNeighbor(room));
            room.setDoorTop(hasTopNeighbor(room));
        }
        else if (room.size == OnexTwo)
        {
            room.setDoorBottomLeft(hasBottomLeftNeighbor(room));
            room.setDoorBottomRight(hasBottomRightNeighbor(room));
            room.setDoorLeft(hasLeftNeighbor(room));
            room.setDoorRight(hasRightNeighbor(room));
            room.setDoorTopLeft(hasTopLeftNeighbor(room));
            room.setDoorTopRight(hasTopRightNeighbor(room));
        }
        else if (room.size == TwoxOne)
        {
            room.setDoorBottom(hasBottomNeighbor(room));
            room.setDoorLeftBottom(hasLeftBottomNeighbor(room));
            room.setDoorLeftTop(hasLeftTopNeighbor(room));
            room.setDoorRightBottom(hasRightBottomNeighbor(room));
            room.setDoorRightTop(hasRightTopNeighbor(room));
            room.setDoorTop(hasTopNeighbor(room));
        }
        else if (room.size == TwoxTwo)
        {
            room.setDoorBottomLeft(hasBottomLeftNeighbor(room));
            room.setDoorBottomRight(hasBottomRightNeighbor(room));
            room.setDoorLeftBottom(hasLeftBottomNeighbor(room));
            room.setDoorLeftTop(hasLeftTopNeighbor(room));
            room.setDoorRightBottom(hasRightBottomNeighbor(room));
            room.setDoorRightTop(hasRightTopNeighbor(room));
            room.setDoorTopLeft(hasTopLeftNeighbor(room));
            room.setDoorTopRight(hasTopRightNeighbor(room));
        }
        else
        {
            room.setDoorBottomLeft(hasBottomLeftNeighbor(room));
            room.setDoorBottom(hasBottomNeighbor(room));
            room.setDoorBottomRight(hasBottomRightNeighbor(room));
            room.setDoorLeftBottom(hasLeftBottomNeighbor(room));
            room.setDoorLeft(hasLeftNeighbor(room));
            room.setDoorLeftTop(hasLeftTopNeighbor(room));
            room.setDoorRightBottom(hasRightBottomNeighbor(room));
            room.setDoorRight(hasRightNeighbor(room));
            room.setDoorRightTop(hasRightTopNeighbor(room));
            room.setDoorTopLeft(hasTopLeftNeighbor(room));
            room.setDoorTop(hasTopNeighbor(room));
            room.setDoorTopRight(hasTopRightNeighbor(room));
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
        Vector3 rot;
        int gridSize = 10;

        for (int i = 0; i < rooms.Count; i++)
        {
            float offsetX = rooms[i].location.x * gridSize;
            float offsetZ = rooms[i].location.y * gridSize;

            if (i == 0)
            {
                GameObject rm = Instantiate(roomDoorAll, new Vector3(offsetX, 0, offsetZ), Quaternion.identity);
                rm.transform.localScale.Set(3, 1, 3);
                rm.transform.parent = map;
                FillNavBaker(rm);
                return;
            }

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
                if ((hasTopNeighbor(rooms[i]) && hasBottomNeighbor(rooms[i]))
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
        bool top = hasTopNeighbor(room);

        if (right && bottom && left)
        {
            return new Vector3(0, 0, 0);
        }
        else if (bottom && left && top)
        {
            return new Vector3(0, 90, 0);
        }
        else if (left && top && right)
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
        bool top = hasTopNeighbor(room);

        if (bottom && top)
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
        bool top = hasTopNeighbor(room);

        if (bottom && left)
        {
            return new Vector3(0, 0, 0);
        }
        else if (left && top)
        {
            return new Vector3(0, 90, 0);
        }
        else if (top && right)
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
        bool top = hasTopNeighbor(room);

        if (bottom)
        {
            return new Vector3(0, 0, 0);
        }
        else if (left)
        {
            return new Vector3(0, 90, 0);
        }
        else if (top)
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
