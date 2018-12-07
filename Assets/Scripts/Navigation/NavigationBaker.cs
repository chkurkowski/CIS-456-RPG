using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NavigationBaker : MonoBehaviour
{
    public List<NavMeshSurface> surfaces = new List<NavMeshSurface>();

    public void generateNavMesh()
    {
        for (int i = 0; i < surfaces.Count; i++)
        {
            surfaces[i].BuildNavMesh();
        }
    }
}
