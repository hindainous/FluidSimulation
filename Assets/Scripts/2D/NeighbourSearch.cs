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
    public GameObject Simulation;

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
