using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
using System.Drawing;


public class NeighbourSearch3D : MonoBehaviour
{
    Tuple<int, int, int>[] cellOffsets = new Tuple<int, int, int>[]
    {
        Tuple.Create(0, 0, 0),
        Tuple.Create(0, 0, 1),
        Tuple.Create(0, 0, -1),
        Tuple.Create(0, 1, 0),
        Tuple.Create(0, 1, 1),
        Tuple.Create(0, 1, -1),
        Tuple.Create(0, -1, 0),
        Tuple.Create(0, -1, 1),
        Tuple.Create(0, -1, -1),
        Tuple.Create(-1, 0, 0),
        Tuple.Create(-1, 0, 1),
        Tuple.Create(-1, 0, -1),
        Tuple.Create(-1, 1, 0),
        Tuple.Create(-1, 1, 1),
        Tuple.Create(-1, 1, -1),
        Tuple.Create(-1, -1, 0),
        Tuple.Create(-1, -1, 1),
        Tuple.Create(-1, -1, -1),
        Tuple.Create(1, 0, 0),
        Tuple.Create(1, 0, 1),
        Tuple.Create(1, 0, -1),
        Tuple.Create(1, 1, 0),
        Tuple.Create(1, 1, 1),
        Tuple.Create(1, 1, -1),
        Tuple.Create(1, -1, 0),
        Tuple.Create(1, -1, 1),
        Tuple.Create(1, -1, -1)
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
            (int x, int y, int z) = GetGridLocation(positions[i]);
            uint Hash = GetHash(x, y, z);
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

        (int cellX, int cellY, int cellZ) = GetGridLocation(point);
        foreach ((int offsetX, int offsetY, int offsetZ) in cellOffsets)
        {
            uint key = GetCellKey(GetHash(cellX + offsetX, cellY + offsetY, cellZ + offsetZ), particleCount);
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

    public (int x, int y, int z) GetGridLocation(Vector3 position)
    {
        int x = (int)(position.x / smoothingRadius);
        int y = (int)(position.y / smoothingRadius);
        int z = (int)(position.z / smoothingRadius);
        return (x, y, z);
    }

    public uint GetHash(int x, int y, int z)
    {
        uint a = (uint)(x * 7019);
        uint b = (uint)(y * 35317);
        uint c = (uint)(z * 131071);
        return a + b + c;
    }

    public uint GetCellKey(uint Hash, int length)
    {
        return (uint)(Hash % length);
    }
}
