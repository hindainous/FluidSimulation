using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
using System.Drawing;


public class NeighbourSearch : MonoBehaviour
{

    Tuple<int, int>[] cellOffsets = new Tuple<int, int>[]
    {
            Tuple.Create(0, 0),
            Tuple.Create(0, 1),
            Tuple.Create(0, -1),
            Tuple.Create(-1, 0),
            Tuple.Create(-1, 1),
            Tuple.Create(-1, -1),
            Tuple.Create(1, 0),
            Tuple.Create(1, 1),
            Tuple.Create(1, -1)
    };

    public bool drawGizmosGrid = true;
    public HashSet<GridItem> grids = new HashSet<GridItem>();
    public GameObject Simulation;

    public Vector3[] points;
    public float radius;

    public Entry[] gridLookup;
    private int[] startIndices;

    public int particleCount;
    public float smoothingRadius;

    // Start is called before the first frame update
    void Start()
    {
        gridLookup = new Entry[particleCount];
        startIndices = new int[particleCount];
    }

    public int [] UpdateGridLookup(Vector3[] positions)
    {
        Parallel.For(0, positions.Length, i =>
        {
            (int x, int y) = GetGridLocation(positions[i]);
            uint Hash = GetHash(x, y);
            gridLookup[i] = new Entry(i, GetCellKey(Hash, positions.Length));
            startIndices[i] = int.MaxValue;
        });

        Array.Sort(gridLookup, (Entry a, Entry b) => a.cellKey.CompareTo(b.cellKey));

        Parallel.For(0, positions.Length, i =>
        {
            uint CurrentKey = gridLookup[i].cellKey;
            uint PreviusKey = i == 0 ? uint.MaxValue : gridLookup[i - 1].cellKey;
            if (CurrentKey != PreviusKey)
            {
                startIndices[CurrentKey] = i;
            }
        });

        return startIndices;
    }

    public List<int> GetNeighbours(Vector3 point)
    {
        List<int> neighbours = new List<int>();

        (int cellX, int cellY) = GetGridLocation(point);
        foreach ((int offsetX, int offsetY) in cellOffsets)
        {
            uint key = GetCellKey(GetHash(cellX + offsetX, cellY + offsetY), particleCount);
            int cellStartIndex = startIndices[key];

            for (int i = cellStartIndex; i < particleCount; i++)
            {
                if (gridLookup[i].cellKey != key) break;

                int particleIndex = gridLookup[i].particleIndex;

                neighbours.Add(particleIndex);
            }
        }

        return neighbours;
    }

    public (int x, int y) GetGridLocation(Vector3 position)
    {
        int x = (int)(position.x / smoothingRadius);
        int y = (int)(position.y / smoothingRadius);
        return (x, y);
    }

    public uint GetHash(int x, int y)
    {
        uint a = (uint)(x * 7019);
        uint b = (uint)(y * 35317);
        return a + b;
    }

    public uint GetCellKey(uint Hash, int length)
    {
        return (uint)(Hash % length);
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
        if( other.cellKey < this.cellKey ) return 1;
        else if( other.cellKey > this.cellKey )return -1;
        else
        {
            return 0;
        }
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
