using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using UnityEngine;

public class NeighbourSearch : MonoBehaviour
{
    public bool drawGizmosGrid = true;
    public List<GridItem> grids = new List<GridItem>();
    public GameObject Simulation;

    public Entry[] gridLookup;
    public int[] startIndices;

    public bool getItems = false;

    // Start is called before the first frame update
    void Start()
    {
        gridLookup = new Entry[Simulation.GetComponent<Circle>().particleCount];
        startIndices = new int[Simulation.GetComponent<Circle>().particleCount];
    }

    // Update is called once per frame
    void Update()
    {
        if (getItems == true)
        {
            MakeList();
            int i = 0;
            foreach (Vector3 position in Simulation.GetComponent<Circle>().getPositions())
            {
                GridItem item = FindGridCell(position);
                gridLookup[i] = new Entry(i, GetCellKey(HashCell(item.coordX, item.coordY)));
                i++;
                //Debug.Log("Particle:" + i + " GridCell: " + posX + " " + posY);
                //Debug.Log("Counter:" + item.num +" X: " + item.x + " Y: " + item.y + " CoordX: "+ item.cordX + " CoordY: " + item.cordY);
            }

            Array.Sort(gridLookup);

            int lastIndex = 0;
            for(int j = 0; j < gridLookup.Length; j++) 
            {
                if (j + 1 == gridLookup.Length)
                {
                    startIndices[j] = lastIndex;
                    break;
                }

                if( gridLookup[j].cellKey == gridLookup[j+1].cellKey )
                {
                    startIndices[j] = int.MaxValue;
                }
                else
                {
                    startIndices[j] = lastIndex;
                    lastIndex = j+1;
                }
            }
            getItems = false;
        }
        
    }

    public GridItem FindGridCell(Vector3 position)
    {
        foreach (GridItem item in grids)
        {
            if (item.x < position.x)
                continue;

            if (item.y < position.y)
                continue;

            return item;
        }

        return new GridItem();
    }

    public uint GetCellKey(uint Hash)
    {
        return Hash % (uint)gridLookup.Length;
    }

    public uint HashCell(int x, int y)
    {
        uint a = (uint)x * 103387;
        uint b = (uint)y * 96763;
        return a + b;
    }

    void MakeList()
    {
        List<GridItem> gridTemp = new List<GridItem>();
        float x = Simulation.GetComponent<Circle>().boundsSize.x;
        int counter = 0;
        for (int i = 0; x > 0; i++)
        {
            x -= Simulation.GetComponent<Circle>().smoothingRadius * 2;
            float y = Simulation.GetComponent<Circle>().boundsSize.y;
            for (int j = 0; y > 0; j++)
            {
                counter++;
                y -= Simulation.GetComponent<Circle>().smoothingRadius * 2;
                gridTemp.Add(new GridItem(
                    Simulation.GetComponent<Circle>().smoothingRadius * 2 * i - (Simulation.GetComponent<Circle>().boundsSize.x / 2), 
                    Simulation.GetComponent<Circle>().smoothingRadius * 2 * j - (Simulation.GetComponent<Circle>().boundsSize.y / 2),
                    i, j));
            }
        }
        grids = gridTemp;
    }

    void OnDrawGizmos()
    {
        if (drawGizmosGrid == true)
        {
            Vector3 cellSize = new Vector3(Simulation.GetComponent<Circle>().smoothingRadius * 2, Simulation.GetComponent<Circle>().smoothingRadius * 2);
            Gizmos.color = UnityEngine.Color.green;

            float x = Simulation.GetComponent<Circle>().boundsSize.x;

            for (int i = 0; x > 0; i++)
            {
                x -= Simulation.GetComponent<Circle>().smoothingRadius * 2;
                float y = Simulation.GetComponent<Circle>().boundsSize.y;
                for (int j = 0; y > 0; j++)
                {
                    y -= Simulation.GetComponent<Circle>().smoothingRadius * 2;
                    Gizmos.DrawWireCube(new Vector3(
                        Simulation.GetComponent<Circle>().smoothingRadius * 2 * i - (Simulation.GetComponent<Circle>().boundsSize.x / 2) + Simulation.GetComponent<Circle>().smoothingRadius, 
                        Simulation.GetComponent<Circle>().smoothingRadius * 2 * j - (Simulation.GetComponent<Circle>().boundsSize.y / 2) + Simulation.GetComponent<Circle>().smoothingRadius, 
                        0), cellSize);
                }
            }
        }
    }
}

public struct Entry : IComparable<Entry>
{
    public int particleIndex { get; set; }
    public uint cellKey { get; set; }

    public Entry(int particleIndex, uint cellKey)
    {
        this.particleIndex = particleIndex;
        this.cellKey = cellKey;
    }

    public int CompareTo(Entry other)
    {
        return other.cellKey.CompareTo(this.cellKey);
    }
}

public struct GridItem
{
    public int coordX{ get; set; }
    public int coordY{ get; set; }
    public float x { get; set; }
    public float y { get; set; }
    

    public GridItem(float x, float y, int coordX, int coordY)
    {
        this.x = x;
        this.y = y;
        this.coordX = coordX;
        this.coordY = coordY;
    }
}
