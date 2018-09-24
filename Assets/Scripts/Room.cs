using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Room {

    public Vector2 location;
    public int type;
    public bool doorTop, doorBottom, doorLeft, doorRight;

    public Room(Vector2 l, int t)
    {
        location = l;
        type = t;
    }
}
