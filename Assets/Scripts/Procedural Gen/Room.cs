using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room {

    //Where the room is located
    public Vector2 location;

    //What type of room it is:
    //0: Default
    //TODO: More types of rooms (spawn room, loot room, boss room, etc.)
    //TODO: Could use strings instead of ints to make it more clear
    public int type;

    //Whether there is a room above, below, left, or right of the current room
    public bool doorTop, doorBottom, doorLeft, doorRight;

    //Constructor
    public Room(Vector2 l, int t)
    {
        location = l;
        type = t;
    }
}
