using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room {

    //Where the room is located (top-left point)
    public Vector2 location;
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
    public Room(Vector2 l, Vector2 s, int t)
    {
        checkValidSize(s);

        location = l;
        size = s;
        type = t;

        setMaxNeighbors();

        locations = new List<Vector2>();
        setLocations();
    }

    public Room(Vector2 l, Vector2 s)
    {
        checkValidSize(s);

        location = l;
        size = s;
        type = 0;

        setMaxNeighbors();

        locations = new List<Vector2>();
        setLocations();
    }

    public Room(Vector2 l, int t)
    {
        location = l;
        size = OnexOne;
        type = t;

        setMaxNeighbors();

        locations = new List<Vector2>();
        setLocations();
    }

    public Room(Vector2 l)
    {
        location = l;
        size = OnexOne;
        type = 0;

        setMaxNeighbors();

        locations = new List<Vector2>();
        setLocations();
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
            locations.Insert(0, location);
            middle = location;
        }
        else if (size == OnexTwo)
        {
            locations.Insert(0, location);
            left = location;
            locations.Insert(0, location + Vector2.right);
            right = location + Vector2.right;
        }
        else if (size == TwoxOne)
        {
            locations.Insert(0, location);
            top = location;
            locations.Insert(0, location + Vector2.down);
            bottom = location + Vector2.down;
        }
        else if (size == TwoxTwo)
        {
            locations.Insert(0, location);
            topLeft = location;
            locations.Insert(0, location + Vector2.down);
            bottomLeft = location + Vector2.down;
            locations.Insert(0, location + Vector2.down + Vector2.right);
            bottomRight = location + Vector2.down + Vector2.right;
            locations.Insert(0, location + Vector2.right);
            topRight = location + Vector2.right;

            leftTop = topLeft;
            leftBottom = bottomLeft;
            rightBottom = bottomRight;
            rightTop = topRight;
        }
        else
        {
            locations.Insert(0, location);
            topLeft = location;
            locations.Insert(0, location + Vector2.down);
            left = location + Vector2.down;
            locations.Insert(0, location + (2 * Vector2.down));
            bottomLeft = location + (2 * Vector2.down);
            locations.Insert(0, location + Vector2.right);
            top = location + Vector2.right;
            locations.Insert(0, location + Vector2.down + Vector2.right);
            middle = location + Vector2.down + Vector2.right;
            locations.Insert(0, location + (2 * Vector2.down) + Vector2.right);
            bottom = location + (2 * Vector2.down) + Vector2.right;
            locations.Insert(0, location + (2 * Vector2.right));
            topRight = location + (2 * Vector2.right);
            locations.Insert(0, location + Vector2.down + (2 * Vector2.right));
            right = location + Vector2.down + (2 * Vector2.right);
            locations.Insert(0, location + (2 * Vector2.down) + (2 * Vector2.right));
            bottomRight = location + (2 * Vector2.down) + (2 * Vector2.right);

            leftTop = topLeft;
            leftBottom = bottomLeft;
            rightBottom = bottomRight;
            rightTop = topRight;
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
}
