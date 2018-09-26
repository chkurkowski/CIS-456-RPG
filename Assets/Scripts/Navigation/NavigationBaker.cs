using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NavigationBaker : MonoBehaviour {

    public List <NavMeshSurface> surfaces = new List<NavMeshSurface>();
    public int roomCount = 0;

    private bool generated;

	// Use this for initialization
	void Update () {
        if (surfaces.Count >= roomCount && !generated)
        {
            generated = true;
            for (int i = 0; i < surfaces.Count; i++)
            {
                surfaces[i].BuildNavMesh();
            }
        }
        else if (generated)
            this.enabled = false;
	}
}
