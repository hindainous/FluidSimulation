using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditorInternal;

public class NeighbourSearch : MonoBehaviour
{
    public bool drawGizmosGrid = true;
    public List<GridItem> grids = new List<GridItem>();
    public GameObject Simulation;

    public Vector3[] points;
    public float radius;

    public Entry[] gridLookup;
    public int[] startIndices;

    // Start is called before the first frame update
    void Start()
    {
        gridLookup = new Entry[Simulation.GetComponent<Circle>().particleCount];
        startIndices = new int[Simulation.GetComponent<Circle>().particleCount];
    }

    public void UpdateGridSorting(Vector3[] points, float radius, bool recalculateGrid = false)
    {
        this.points = points;
        this.radius = radius;

        if( recalculateGrid )
        {
            MakeList();
        }

        Parallel.For(0, points.Length, i =>
        {
            GridItem item = FindGridCell(points[i]);
            gridLookup[i] = new Entry(i, GetCellKey(HashCell(item.coordX, item.coordY)));
            startIndices[i] = int.MaxValue;
        });

        /*for (int i = 0; i < gridLookup.Length; i++)
        {
            Debug.Log("GRIDLOOKUP: " +  gridLookup[i].cellKey + " index: " + gridLookup[i].particleIndex);
        }*/

        Array.Sort(gridLookup);

        Parallel.For(0, points.Length, i =>
        {
            uint CurrentKey = gridLookup[i].cellKey;
            uint PreviousKey = i == 0 ? uint.MaxValue : gridLookup[i - 1].cellKey;
            if (CurrentKey != PreviousKey)
            {
                startIndices[CurrentKey] = i;
            }
        });

    }

    public GridItem FindGridCell(Vector3 position)
    {
        for(int i = 0; i < grids.Count; i++)
        {
            GridItem item = grids[i];

            if (item.upperLeftx < position.x)
                continue;

            if (item.upperLefty < position.y)
                continue;

            if (item.lowerRightx > position.x)
                continue;

            if(item.lowerRighty > position.y)
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
        float smoothingDiameter = Simulation.GetComponent<Circle>().smoothingRadius * 2;
        float x = Simulation.GetComponent<Circle>().boundsSize.x;
        int counter = 0;
        for (int i = 0; x > 0; i++)
        {
            x -= smoothingDiameter;
            float y = Simulation.GetComponent<Circle>().boundsSize.y;
            for (int j = 0; y > 0; j++)
            {
                counter++;
                y -= smoothingDiameter;
                float xPosi = smoothingDiameter * i - (Simulation.GetComponent<Circle>().boundsSize.x / 2);
                float yPosi = smoothingDiameter * j - (Simulation.GetComponent<Circle>().boundsSize.y / 2);
                //Debug.Log("Positions x: " + xPosi + " y: " + yPosi + " i: " + i + " j: " + j);
                gridTemp.Add(new GridItem(xPosi, 
                                          yPosi, 
                                          xPosi + smoothingDiameter, 
                                          yPosi + smoothingDiameter, 
                                          i, 
                                          j));
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
    public float lowerRightx { get; set; }
    public float lowerRighty { get; set; }

    public float upperLeftx { get; set; }
    public float upperLefty { get; set; }


    public GridItem(float lrx, float lry, float ulx, float uly, int coordX, int coordY)
    {
        this.upperLeftx = ulx;
        this.upperLefty = uly;
        this.lowerRightx = lrx;
        this.lowerRighty = lry;
        this.coordX = coordX;
        this.coordY = coordY;
    }
}
