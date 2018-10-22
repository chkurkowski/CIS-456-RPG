using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room {

    //Where the room is located (CENTER)
    public Vector2 center;
    //Top-leftmost location
    public Vector2 topLeftInnerLocation;
    //All 10x10 sections of the room (CENTER OF ROOMS)
    public List<Vector2> locations;

    //How big the room is
    public Vector2 size;

    public int maxNeighbors;

    //What type of room it is:
    //0: Default
    //TODO: More types of rooms (spawn room, loot room, boss room, etc.)
    //TODO: Could use strings instead of ints to make it more clear
    public int type;

    //Whether there is a room above, below, left, or right of the current room
    private bool doorBottomLeft, doorBottom, doorBottomRight,
        doorLeftBottom, doorLeft, doorLeftTop,
        doorRightBottom, doorRight, doorRightTop,
        doorTopLeft, doorTop, doorTopRight;

    private Room roomBottomLeft, roomBottom, roomBottomRight,
        roomLeftBottom, roomLeft, roomLeftTop,
        roomRightBottom, roomRight, roomRightTop,
        roomTopLeft, roomTop, roomTopRight;

    private Vector2 bottomLeft, bottom, bottomRight,
        leftBottom, left, leftTop,
        rightBottom, right, rightTop,
        topLeft, top, topRight,
        middle;

    //Useful Vectors
    private Vector2 OnexOne = new Vector2(1f, 1f);
    private Vector2 OnexTwo = new Vector2(1f, 2f);
    private Vector2 TwoxOne = new Vector2(2f, 1f);
    private Vector2 TwoxTwo = new Vector2(2f, 2f);
    private Vector2 ThreexThree = new Vector2(3f, 3f);

    //Constructor
    public Room(Vector2 c, Vector2 s, int t)
    {
        checkValidSize(s);

        center = c;
        size = s;
        type = t;

        setMaxNeighbors();

        locations = new List<Vector2>();
        setLocations();
        setDoors();
    }

    public Room(Vector2 c, Vector2 s)
    {
        checkValidSize(s);

        center = c;
        size = s;
        type = 0;

        setMaxNeighbors();

        locations = new List<Vector2>();
        setLocations();
        setDoors();
    }

    public Room(Vector2 c, int t)
    {
        center = c;
        size = OnexOne;
        type = t;

        setMaxNeighbors();

        locations = new List<Vector2>();
        setLocations();
        setDoors();
    }

    public Room(Vector2 c)
    {
        center = c;
        size = OnexOne;
        type = 0;

        setMaxNeighbors();

        locations = new List<Vector2>();
        setLocations();
        setDoors();
    }

    private void setDoors()
    {
        if (size == OnexOne)
        {
            doorBottom = true;
            doorLeft = true;
            doorRight = true;
            doorTop = true;
        }
        else if (size == OnexTwo)
        {
            doorBottomLeft = true;
            doorBottomRight = true;
            doorLeft = true;
            doorRight = true;
            doorTopLeft = true;
            doorTopRight = true;
        }
        else if (size == TwoxOne)
        {

            doorBottom = true;
            doorLeftBottom = true;
            doorLeftTop = true;
            doorRightBottom = true;
            doorRightTop = true;
            doorTop = true;
        }
        else if (size == TwoxTwo)
        {
            doorBottomLeft = true;
            doorBottomRight = true;
            doorLeftBottom = true;
            doorLeftTop = true;
            doorRightBottom = true;
            doorRightTop = true;
            doorTopLeft = true;
            doorTopRight = true;
        }
        else
        {
            doorBottomLeft = true;
            doorBottom = true;
            doorBottomRight = true;
            doorLeftBottom = true;
            doorLeft = true;
            doorLeftTop = true;
            doorRightBottom = true;
            doorRight = true;
            doorRightTop = true;
            doorTopLeft = true;
            doorTop = true;
            doorTopRight = true;
        }
    }

    private void checkValidSize(Vector2 s)
    {
        if (s != OnexOne
            && s != OnexTwo
            && s != TwoxOne
            && s != TwoxTwo
            && s != ThreexThree)
        {
            throw new System.ArgumentException("Not a valid Room Size!");
        }
    }

    private void setMaxNeighbors()
    {
        if (size == OnexOne)
        {
            maxNeighbors = 4;
        }
        else if (size == OnexTwo || size == TwoxOne)
        {
            maxNeighbors = 6;
        }
        else if (size == TwoxTwo)
        {
            maxNeighbors = 8;
        }
        else
        {
            maxNeighbors = 12;
        }
    }

    private void setLocations()
    {
        if (size == OnexOne)
        {
            locations.Insert(0, center);
            middle = center;

            topLeftInnerLocation = center;
        }
        else if (size == OnexTwo)
        {
            Vector2 newLeft = center + new Vector2(-0.5f, 0f);
            Vector2 newRight = center + new Vector2(0.5f, 0f);
            locations.Insert(0, newLeft);
            left = newLeft;
            locations.Insert(0, newRight);
            right = newRight;

            topLeftInnerLocation = left;
        }
        else if (size == TwoxOne)
        {
            Vector2 newBottom = center + new Vector2(0f, -0.5f);
            Vector2 newTop = center + new Vector2(0f, 0.5f);
            locations.Insert(0, newBottom);
            bottom = newBottom;
            locations.Insert(0, newTop);
            top = newTop;

            topLeftInnerLocation = top;
        }
        else if (size == TwoxTwo)
        {
            Vector2 newBottomLeft = center + new Vector2(-0.5f, -0.5f);
            Vector2 newBottomRight = center + new Vector2(0.5f, -0.5f);
            Vector2 newTopLeft = center + new Vector2(-0.5f, 0.5f);
            Vector2 newTopRight = center + new Vector2(0.5f, 0.5f);

            locations.Insert(0, newBottomLeft);
            bottomLeft = newBottomLeft;
            locations.Insert(0, newBottomRight);
            bottomRight = newBottomRight;
            locations.Insert(0, newTopLeft);
            topLeft = newTopLeft;
            locations.Insert(0, newTopRight);
            topRight = newTopRight;

            leftTop = topLeft;
            leftBottom = bottomLeft;
            rightBottom = bottomRight;
            rightTop = topRight;

            topLeftInnerLocation = topLeft;
        }
        else
        {
            Vector2 newBottomLeft = center + new Vector2(-1f, -1f);
            Vector2 newBottom = center + new Vector2(0f, -1f);
            Vector2 newBottomRight = center + new Vector2(1f, -1f);
            Vector2 newLeft = center + new Vector2(-1f, 0f);
            Vector2 newMiddle = center;
            Vector2 newRight = center + new Vector2(1f, 0f);
            Vector2 newTopLeft = center + new Vector2(-1f, 1f);
            Vector2 newTop = center + new Vector2(0f, 1f);
            Vector2 newTopRight = center + new Vector2(1f, 1f);

            locations.Insert(0, newBottomLeft);
            bottomLeft = newBottomLeft;
            locations.Insert(0, newBottom);
            bottom = newBottom;
            locations.Insert(0, newBottomRight);
            bottomRight = newBottomRight;
            locations.Insert(0, newLeft);
            left = newLeft;
            locations.Insert(0, newMiddle);
            middle = newMiddle;
            locations.Insert(0, newRight);
            right = newRight;
            locations.Insert(0, newTopLeft);
            topLeft = newTopLeft;
            locations.Insert(0, newTop);
            top = newTop;
            locations.Insert(0, newTopRight);
            topRight = newTopRight;

            leftTop = topLeft;
            leftBottom = bottomLeft;
            rightBottom = bottomRight;
            rightTop = topRight;

            topLeftInnerLocation = topLeft;
        }
    }

    public void setDoorBottomLeft(bool b)
    {
        if (size != OnexTwo && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setDoorBottom(bool) instead!");
        }
        doorBottomLeft = b;
    }

    public void setDoorBottom(bool b)
    {
        if (size != OnexOne && size != TwoxOne && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setDoorBottomLeft/Right(bool) instead!");
        }
        doorBottom = b;
    }

    public void setDoorBottomRight(bool b)
    {
        if (size != OnexTwo && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setDoorBottom(bool) instead!");
        }
        doorBottomRight = b;
    }

    public void setDoorLeftBottom(bool b)
    {
        if (size != TwoxOne && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setDoorLeft(bool) instead!");
        }
        doorLeftBottom = b;
    }

    public void setDoorLeft(bool b)
    {
        if (size != OnexOne && size != OnexTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setDoorLeftBottom/Top(bool) instead!");
        }
        doorLeft = b;
    }

    public void setDoorLeftTop(bool b)
    {
        if (size != TwoxOne && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setDoorLeft(bool) instead!");
        }
        doorLeftTop = b;
    }

    public void setDoorRightBottom(bool b)
    {
        if (size != TwoxOne && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setDoorRight(bool) instead!");
        }
        doorRightBottom = b;
    }

    public void setDoorRight(bool b)
    {
        if (size != OnexOne && size != OnexTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setDoorRightBottom/Top(bool) instead!");
        }
        doorRight = b;
    }

    public void setDoorRightTop(bool b)
    {
        if (size != TwoxOne && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setDoorRight(bool) instead!");
        }
        doorRightTop = b;
    }

    public void setDoorTopLeft(bool b)
    {
        if (size != OnexTwo && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setDoorTop(bool) instead!");
        }
        doorTopLeft = b;
    }

    public void setDoorTop(bool b)
    {
        if (size != OnexOne && size != TwoxOne && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setDoorTopLeft/Right(bool) instead!");
        }
        doorTop = b;
    }

    public void setDoorTopRight(bool b)
    {
        if (size != OnexTwo && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setDoorTop(bool) instead!");
        }
        doorTopRight = b;
    }

    public bool getDoorBottomLeft()
    {
        if (size != OnexTwo && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getDoorBottom() instead!");
        }
        return doorBottomLeft;
    }

    public bool getDoorBottom()
    {
        if (size != OnexOne && size != TwoxOne && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getDoorBottomLeft/Right() instead!");
        }
        return doorBottom;
    }

    public bool getDoorBottomRight()
    {
        if (size != OnexTwo && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getDoorBottom() instead!");
        }
        return doorBottomRight;
    }

    public bool getDoorLeftBottom()
    {
        if (size != TwoxOne && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getDoorLeft() instead!");
        }
        return doorLeftBottom;
    }

    public bool getDoorLeft()
    {
        if (size != OnexOne && size != OnexTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getDoorLeftBottom/Top() instead!");
        }
        return doorLeft;
    }

    public bool getDoorLeftTop()
    {
        if (size != TwoxOne && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getDoorLeft() instead!");
        }
        return doorLeftTop;
    }

    public bool getDoorRightBottom()
    {
        if (size != TwoxOne && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getDoorRight() instead!");
        }
        return doorRightBottom;
    }

    public bool getDoorRight()
    {
        if (size != OnexOne && size != OnexTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getDoorRightBottom/Top() instead!");
        }
        return doorRight;
    }

    public bool getDoorRightTop()
    {
        if (size != TwoxOne && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getDoorRight() instead!");
        }
        return doorRightTop;
    }

    public bool getDoorTopLeft()
    {
        if (size != OnexTwo && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getDoorTop() instead!");
        }
        return doorTopLeft;
    }

    public bool getDoorTop()
    {
        if (size != OnexOne && size != TwoxOne && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getDoorTopLeft/Right() instead!");
        }
        return doorTop;
    }

    public bool getDoorTopRight()
    {
        if (size != OnexTwo && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getDoorTop() instead!");
        }
        return doorTopRight;
    }

    public void setRoomBottomLeft(Room room)
    {
        if (size != OnexTwo && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setRoomBottom(Room) instead!");
        }
        roomBottomLeft = room;
    }

    public void setRoomBottom(Room room)
    {
        if (size != OnexOne && size != TwoxOne && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setRoomBottomLeft/Right(Room) instead!");
        }
        roomBottom = room;
    }

    public void setRoomBottomRight(Room room)
    {
        if (size != OnexTwo && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setRoomBottom(Room) instead!");
        }
        roomBottomRight = room;
    }

    public void setRoomLeftBottom(Room room)
    {
        if (size != TwoxOne && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setRoomLeft(Room) instead!");
        }
        roomLeftBottom = room;
    }

    public void setRoomLeft(Room room)
    {
        if (size != OnexOne && size != OnexTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setRoomLeftBottom/Top(Room) instead!");
        }
        roomLeft = room;
    }

    public void setRoomLeftTop(Room room)
    {
        if (size != TwoxOne && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setRoomLeft(Room) instead!");
        }
        roomLeftTop = room;
    }

    public void setRoomRightBottom(Room room)
    {
        if (size != TwoxOne && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setRoomRight(Room) instead!");
        }
        roomRightBottom = room;
    }

    public void setRoomRight(Room room)
    {
        if (size != OnexOne && size != OnexTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setRoomRightBottom/Top(Room) instead!");
        }
        roomRight = room;
    }

    public void setRoomRightTop(Room room)
    {
        if (size != TwoxOne && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setRoomRight(Room) instead!");
        }
        roomRightTop = room;
    }

    public void setRoomTopLeft(Room room)
    {
        if (size != OnexTwo && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setRoomTop(Room) instead!");
        }
        roomTopLeft = room;
    }

    public void setRoomTop(Room room)
    {
        if (size != OnexOne && size != TwoxOne && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setRoomTopLeft/Right(Room) instead!");
        }
        roomTop = room;
    }

    public void setRoomTopRight(Room room)
    {
        if (size != OnexTwo && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use setRoomTop(Room) instead!");
        }
        roomTopRight = room;
    }

    public Room getRoomBottomLeft()
    {
        if (size != OnexTwo && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getRoomBottom() instead!");
        }
        return roomBottomLeft;
    }

    public Room getRoomBottom()
    {
        if (size != OnexOne && size != TwoxOne && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getRoomBottomLeft/Right() instead!");
        }
        return roomBottom;
    }

    public Room getRoomBottomRight()
    {
        if (size != OnexTwo && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getRoomBottom() instead!");
        }
        return roomBottomRight;
    }

    public Room getRoomLeftBottom()
    {
        if (size != TwoxOne && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getRoomLeft() instead!");
        }
        return roomLeftBottom;
    }

    public Room getRoomLeft()
    {
        if (size != OnexOne && size != OnexTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getRoomLeftBottom/Top() instead!");
        }
        return roomLeft;
    }

    public Room getRoomLeftTop()
    {
        if (size != TwoxOne && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getRoomLeft() instead!");
        }
        return roomLeftTop;
    }

    public Room getRoomRightBottom()
    {
        if (size != TwoxOne && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getRoomRight() instead!");
        }
        return roomRightBottom;
    }

    public Room getRoomRight()
    {
        if (size != OnexOne && size != OnexTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getRoomRightBottom/Top() instead!");
        }
        return roomRight;
    }

    public Room getRoomRightTop()
    {
        if (size != TwoxOne && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getRoomRight() instead!");
        }
        return roomRightTop;
    }

    public Room getRoomTopLeft()
    {
        if (size != OnexTwo && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getRoomTop() instead!");
        }
        return roomTopLeft;
    }

    public Room getRoomTop()
    {
        if (size != OnexOne && size != TwoxOne && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getRoomTopLeft/Right() instead!");
        }
        return roomTop;
    }

    public Room getRoomTopRight()
    {
        if (size != OnexTwo && size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("Use getRoomTop() instead!");
        }
        return roomTopRight;
    }

    public Vector2 getBottomLeft()
    {
        if (size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("This room does not have a bottomLeft location!");
        }
        return bottomLeft;
    }

    public Vector2 getBottom()
    {
        if (size != TwoxOne && size != ThreexThree)
        {
            throw new System.ArgumentException("This room does not have a bottom location!");
        }
        return bottom;
    }

    public Vector2 getBottomRight()
    {
        if (size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("This room does not have a bottomRight location!");
        }
        return bottomRight;
    }

    public Vector2 getLeftBottom()
    {
        if (size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("This room does not have a leftBottom location!");
        }
        return leftBottom;
    }

    public Vector2 getLeft()
    {
        if (size != OnexTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("This room does not have a left location!");
        }
        return left;
    }

    public Vector2 getLeftTop()
    {
        if (size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("This room does not have a leftTop location!");
        }
        return leftTop;
    }

    public Vector2 getRightBottom()
    {
        if (size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("This room does not have a rightBottom location!");
        }
        return rightBottom;
    }

    public Vector2 getRight()
    {
        if (size != OnexTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("This room does not have a right location!");
        }
        return right;
    }

    public Vector2 getRightTop()
    {
        if (size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("This room does not have a rightTop location!");
        }
        return rightTop;
    }

    public Vector2 getTopLeft()
    {
        if (size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("This room does not have a topLeft location!");
        }
        return topLeft;
    }

    public Vector2 getTop()
    {
        if (size != TwoxOne && size != ThreexThree)
        {
            throw new System.ArgumentException("This room does not have a top location!");
        }
        return top;
    }

    public Vector2 getTopRight()
    {
        if (size != TwoxTwo && size != ThreexThree)
        {
            throw new System.ArgumentException("This room does not have a topRight location!");
        }
        return topRight;
    }

    public Vector2 getMiddle()
    {
        if (size != OnexOne && size != ThreexThree)
        {
            throw new System.ArgumentException("This room does not have a middle location!");
        }

        return middle;
    }

    public List<Room> getNeighboringRooms()
    {
        List<Room> roomList = new List<Room>();

        if (size == OnexOne)
        {
            if (doorBottom)
            {
                roomList.Add(roomBottom);
            }
            if (doorLeft)
            {
                roomList.Add(roomLeft);
            }
            if (doorRight)
            {
                roomList.Add(roomRight);
            }
            if (doorTop)
            {
                roomList.Add(roomTop);
            }
        }
        else if (size == OnexTwo)
        {
            if (doorBottomLeft)
            {
                roomList.Add(roomBottomLeft);
            }
            if (doorBottomRight)
            {
                roomList.Add(roomBottomRight);
            }
            if (doorLeft)
            {
                roomList.Add(roomLeft);
            }
            if (doorRight)
            {
                roomList.Add(roomRight);
            }
            if (doorTopLeft)
            {
                roomList.Add(roomTopLeft);
            }
            if (doorTopRight)
            {
                roomList.Add(roomTopRight);
            }
        }
        else if (size == TwoxOne)
        {

            if (doorBottom)
            {
                roomList.Add(roomBottom);
            }
            if (doorLeftBottom)
            {
                roomList.Add(roomLeftBottom);
            }
            if (doorLeftTop)
            {
                roomList.Add(roomLeftTop);
            }
            if (doorRightBottom)
            {
                roomList.Add(roomRightBottom);
            }
            if (doorRightTop)
            {
                roomList.Add(roomRightTop);
            }
            if (doorTop)
            {
                roomList.Add(roomTop);
            }
        }
        else if (size == TwoxTwo)
        {
            if (doorBottomLeft)
            {
                roomList.Add(roomBottomLeft);
            }
            if (doorBottomRight)
            {
                roomList.Add(roomBottomRight);
            }
            if (doorLeftBottom)
            {
                roomList.Add(roomLeftBottom);
            }
            if (doorLeftTop)
            {
                roomList.Add(roomLeftTop);
            }
            if (doorRightBottom)
            {
                roomList.Add(roomRightBottom);
            }
            if (doorRightTop)
            {
                roomList.Add(roomRightTop);
            }
            if (doorTopLeft)
            {
                roomList.Add(roomTopLeft);
            }
            if (doorTopRight)
            {
                roomList.Add(roomTopRight);
            }
        }
        else
        {
            if (doorBottomLeft)
            {
                roomList.Add(roomBottomLeft);
            }
            if (doorBottom)
            {
                roomList.Add(roomBottom);
            }
            if (doorBottomRight)
            {
                roomList.Add(roomBottomRight);
            }
            if (doorLeftBottom)
            {
                roomList.Add(roomLeftBottom);
            }
            if (doorLeft)
            {
                roomList.Add(roomLeft);
            }
            if (doorLeftTop)
            {
                roomList.Add(roomLeftTop);
            }
            if (doorRightBottom)
            {
                roomList.Add(roomRightBottom);
            }
            if (doorRight)
            {
                roomList.Add(roomRight);
            }
            if (doorRightTop)
            {
                roomList.Add(roomRightTop);
            }
            if (doorTopLeft)
            {
                roomList.Add(roomTopLeft);
            }
            if (doorTop)
            {
                roomList.Add(roomTop);
            }
            if (doorTopRight)
            {
                roomList.Add(roomTopRight);
            }
        }

        return roomList;
    }
}
