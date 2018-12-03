using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

using System;
using System.Collections.Specialized;
using UnityEditor;
using UnityEditorInternal;

public class NavigationBaker : MonoBehaviour
{
    public List<NavMeshSurface> surfaces = new List<NavMeshSurface>();
    public int roomCount = 0;

    public void generateNavMesh()
    {
        for (int i = 0; i < surfaces.Count; i++)
        {
            surfaces[i].BuildNavMesh();
        }
    }
}
