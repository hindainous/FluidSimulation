using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Jobs;
using UnityEngine.Jobs;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Drawing;
using UnityEngine.SocialPlatforms;

public class Circle : MonoBehaviour
{
    //Time settings
    public float timeScale = 1;
    public int iterationsPerFrame;
    float deltaTime = 0.0001f;


    //Gizmos 
    public bool drawGizmosBoundry = true;
    public bool drawGizmosGrid = true;
    Tuple<int, int>[] cellOffsets = new Tuple<int, int>[]
    {
        Tuple.Create(-1, 1),
        Tuple.Create(0, 1),
        Tuple.Create(1, 1),
        Tuple.Create(-1, 0),
        Tuple.Create(0, 0),
        Tuple.Create(1, 0),
        Tuple.Create(-1, -1),
        Tuple.Create(0, -1),
        Tuple.Create(1, -1),
    };
    //Circle properties
    [Range(0, 10)]
    public float circleRadius = 0.1f;
    public float smoothingRadius = 0.5f;
    public int segments = 100;
    public float collisionDampening = 0.82f;

    public float targetDensity;
    public float pressureMultiplier;

    public float mass = 1.0f;

    //Circles properties
    private float[] densities;
    public int[] startIndices;
    private Entry[] spatialLookup;
    private Vector3[] velocity;
    private Vector3[] points;
    private Vector3[] positions;
    private Vector3[] predictedPositions;
    public int particleCount = 1;

    //Our universe properties
    public float gravity = 9.81f;
    public Vector2 boundsSize = new Vector2(10, 10);


    private Mesh mesh;
    RenderParams rp;

    public Material material;

    void Start()
    {
        positions = new Vector3[particleCount];
        velocity = new Vector3[particleCount];
        predictedPositions = new Vector3[particleCount];
        points = new Vector3[particleCount];
        spatialLookup = new Entry[particleCount];
        startIndices = new int[particleCount];

        densities = new float[particleCount];

        float minX = boundsSize.x / 2 * -1 + circleRadius;
        float maxX = boundsSize.x / 2 - circleRadius;

        float minY = boundsSize.y / 2 * -1 + circleRadius;
        float maxY = boundsSize.y / 2 - circleRadius;

        int particlesPerRow = (int)Mathf.Sqrt(particleCount);
        int particlesPerCol = (particleCount -1) / particlesPerRow + 1;
        float spacing = circleRadius * 2 + 0.01f;

        for (int i = 0; i < particleCount; i++)
        {
            float x = (i % particlesPerRow - particlesPerRow / 2f + 0.5f) * spacing;
            float y = (i / particlesPerRow - particlesPerCol / 2f + 0.5f) * spacing;
            //positions[i] = new Vector3(UnityEngine.Random.Range(minX, maxX), UnityEngine.Random.Range(minY, maxY), 0);    
            positions[i] = new Vector3(x, y, 0);
        }

        mesh = CreateCircleMesh(circleRadius, segments);
        rp = new RenderParams(material);
    }

    Vector2 CalculatePressureForce(int particleIndex)
    {
        Vector2 pressureForce = Vector2.zero;

        for (int i = 0; i < particleCount; i++)
        {
            if (i == particleIndex) continue;
            Vector3 offset = positions[i] - positions[particleIndex];
            float dst = offset.magnitude;
            if (dst > smoothingRadius) continue;
            Vector2 dir = dst == 0 ? new Vector2(0, 1) : offset / dst;
            float slope = SmoothingKernelDerivative(dst, smoothingRadius);
            float density = densities[i];
            float sharedPressure = CalculateSharedPressure(density, densities[particleIndex]);
            pressureForce += sharedPressure * dir * slope * mass / density;
        }

        return pressureForce;
    }

    float CalculateSharedPressure(float densityA, float densityB)
    {
        float pressureA = ConvertDensityToPressure(densityA);
        float pressureB = ConvertDensityToPressure(densityB);
        return (pressureA + pressureB) / 2;
    }

    void UpdateDensities()
    {
        Parallel.For(0, particleCount, i =>
        {
            densities[i] = CalculateDensity(predictedPositions[i]);
        });
    }

    float ConvertDensityToPressure(float density)
    {
        float densityError = density - targetDensity;
        float pressure = densityError * pressureMultiplier;
        return pressure;
    }

    float SmoothingKernel(float radius, float distance)
    {
        if (distance >= radius) return 0;

        float volume = (Mathf.PI * Mathf.Pow(radius, 4)) / 6;
        return (radius - distance) * (radius - distance) / volume;
    }

    static float SmoothingKernelDerivative(float dst, float radius)
    {
        if (dst >= radius) return 0;

        float scale = 12 / (Mathf.PI * Mathf.Pow(radius, 4));
        return (dst - radius) * scale;
    }

    float CalculateDensity(Vector3 particlePosition)
    {
        float density = 0;

        foreach(Vector3 position in positions)
        {
            float dst = (position - particlePosition).magnitude;
            float influence = SmoothingKernel(smoothingRadius, dst);
            density += mass * influence;
        }

        return density;
    }

    void Update()
    {
        for (int i = 0; i < particleCount; i++)
        {
            Graphics.RenderMesh(rp, mesh, 0, Matrix4x4.Translate(positions[i]));
        }

        if (Time.frameCount > 10)
        {
            RunSimulationFrame(Time.deltaTime);
        }
    }

    void RunSimulationFrame(float frameTime)
    {
        float timeStep = frameTime / iterationsPerFrame * timeScale;

        deltaTime = timeStep;

        for (int i = 0; i < iterationsPerFrame; i++)
        {
            SimulationStep();
        }
    }

    void SimulationStep()
    {
        Parallel.For(0, particleCount, i =>
        {
            velocity[i] += Vector3.down * gravity * deltaTime;
            predictedPositions[i] = positions[i] + velocity[i] * 1 / 120f;
        });

        //UpdateSpatialLookUp(predictedPositions, smoothingRadius);


        UpdateDensities();


        Parallel.For(0, particleCount, i =>
        {
            Vector3 pressureForce = CalculatePressureForce(i);
            Vector3 pressureAcceleration = pressureForce / densities[i];
            velocity[i] += pressureAcceleration * deltaTime;
        });


        Parallel.For(0, particleCount, i =>
        {
            positions[i] += velocity[i] * deltaTime;
            ResolveCollision(i);
        });
    }

    public void UpdateSpatialLookUp(Vector3[] points, float radius)
    {
        this.points = points;

        Parallel.For(0, points.Length, i =>
        {
            (int cellX, int cellY) = PositionToCellCoord(points[i], radius);
            uint cellKey = CellKeyFromHash(HashCell(cellX, cellY));
            spatialLookup[i] = new Entry(i, cellKey);
            startIndices[i] = int.MaxValue;
        });

        Array.Sort(spatialLookup);

        Parallel.For(0, points.Length, i =>
        {
            uint key = spatialLookup[i].cellKey;
            uint keyPrev = i == 0 ? uint.MaxValue : spatialLookup[i - 1].cellKey;
            if (key != keyPrev)
            {
                startIndices[key] = i;
            }
        });
    }

    public (int x, int y) PositionToCellCoord(Vector3 point, float radius)
    {
        int cellX = (int)(point.x / radius);
        int cellY = (int)(point.y / radius);
        return (cellX, cellY);
    }

    /*public void ForeachPointWithinRadius(Vector3 samplePoint)
    {
        (int centreX, int centreY) = PositionToCellCoord(samplePoint, smoothingRadius);
        float sqrRadius = smoothingRadius * smoothingRadius;

        foreach((int offsetX, int offsetY) in cellOffsets)
        {
            uint key = CellKeyFromHash(HashCell(centreX + offsetX, centreY + offsetY));
            int cellStartIndex = startIndices[key];

            for (int i = cellStartIndex; i < spatialLookup.Length; i++)
            {
                if (spatialLookup[i].cellKey != key) break;

                int particleIndex = spatialLookup[i].num;
                float sqrDst = (points[particleIndex] - samplePoint).sqrMagnitude;
                if (sqrDst <= sqrRadius)
                {
                }
            }
        }
    }*/

    void ResolveCollision(int arrayPosition)
    {
        Vector2 halfBoundSize = boundsSize / 2 - Vector2.one * circleRadius;

        if (Mathf.Abs(positions[arrayPosition].x) > halfBoundSize.x)
        {
            positions[arrayPosition].x = halfBoundSize.x * Mathf.Sign(positions[arrayPosition].x);
            velocity[arrayPosition].x *= -1 * collisionDampening;
        }

        if (Mathf.Abs(positions[arrayPosition].y) > halfBoundSize.y)
        {
            positions[arrayPosition].y = halfBoundSize.y * Mathf.Sign(positions[arrayPosition].y);
            velocity[arrayPosition].y *= -1 * collisionDampening;
        }
    }

    void OnDrawGizmos()
    {
        Vector3 size = boundsSize;
        if (drawGizmosBoundry == true)
        {
            // Draw a yellow sphere at the transform's position
            Gizmos.color = UnityEngine.Color.yellow;
            Gizmos.DrawWireCube(new Vector3(0, 0, 0), size);
        }

        if(drawGizmosGrid == true)
        {
            Vector3 cellSize = new Vector3(smoothingRadius * 2, smoothingRadius * 2);
            Gizmos.color = UnityEngine.Color.green;

            for (int i = 0; i < boundsSize.x; i++)
            {
                for(int j = 0; j < boundsSize.y; j++)
                {
                    Gizmos.DrawWireCube(new Vector3(i - (boundsSize.x / 2) + smoothingRadius, j - (boundsSize.y / 2) + smoothingRadius, 0), cellSize);
                }
            }
        }
    }

    public (int x, int y) CellCord(Vector3 point)
    { 
        int cellX = (int)Mathf.Floor(-point.x / (smoothingRadius * 2));
        int cellY = (int)Mathf.Floor(point.y / (smoothingRadius * 2));
        return (cellX, cellY);
    }

    public uint HashCell(int x, int y)
    {
        uint a = (uint)x * 103387;
        uint b = (uint)y * 96763;
        return a + b;
    }

    public uint CellKeyFromHash(uint hash)
    {
        return hash % (uint)spatialLookup.Length;
    }

    Mesh CreateCircleMesh(float radius, int segments)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[segments + 1];
        int[] triangles = new int[segments * 3];

        // Center point
        vertices[0] = Vector3.zero;

        // Generate vertices around the circumference
        for (int i = 0; i < segments; i++)
        {
            float angle = 2 * Mathf.PI * i / segments;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
        }

        // Generate triangles
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % segments + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        return mesh;
    }
}

public struct Entry : IComparable<Entry>
{
    public Entry(int x, uint y)
    {
        num = x;
        cellKey = y;
    }

    public int num { get; }
    public uint cellKey { get; }

    public int CompareTo(Entry other)
    {
        // Example: Compare based on the X coordinate first, then Y coordinate
        if (num != other.num)
        {
            return num.CompareTo(other.num);
        }
        return cellKey.CompareTo(other.cellKey);
    }

    // Override Equals for value-based equality
    public override bool Equals(object obj)
    {
        if (obj is Entry other)
        {
            return num == other.num && cellKey == other.cellKey;
        }
        return false;
    }

    // Override GetHashCode
    public override int GetHashCode()
    {
        return HashCode.Combine(num, cellKey);
    }

    // Overload comparison operators
    public static bool operator ==(Entry p1, Entry p2) => p1.Equals(p2);
    public static bool operator !=(Entry p1, Entry p2) => !p1.Equals(p2);
    public static bool operator <(Entry p1, Entry p2) => p1.CompareTo(p2) < 0;
    public static bool operator >(Entry p1, Entry p2) => p1.CompareTo(p2) > 0;
    public static bool operator <=(Entry p1, Entry p2) => p1.CompareTo(p2) <= 0;
    public static bool operator >=(Entry p1, Entry p2) => p1.CompareTo(p2) >= 0;
}