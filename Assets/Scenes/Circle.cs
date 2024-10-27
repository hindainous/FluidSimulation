using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.Jobs;
using UnityEngine.Jobs;
using System.Threading.Tasks;

public class Circle : MonoBehaviour
{
    //Gizmos 
    public bool drawGizmosBoundry = true;
    public bool drawGizmosGrid = true;

    //Circle properties
    [Range(0, 10)]
    public float radius = 0.1f;
    public float smoothingRadius = 0.5f;
    public int segments = 100;
    public float collisionDampening = 0.82f;

    public float density = 0f;

    public float targetDensity;
    public float pressureMultiplier;

    [Range(0.001f, 0.1f)]
    public float deltaTime = 0.001f;

    public float mass = 1.0f;

    //Circles properties
    public float[] densities;
    private Vector3[] velocity;
    private Vector3[] positions;
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

        densities = new float[particleCount];

        float minX = boundsSize.x / 2 * -1 + radius;
        float maxX = boundsSize.x / 2 - radius;

        float minY = boundsSize.y / 2 * -1 + radius;
        float maxY = boundsSize.y / 2 - radius;

        for (int i = 0; i < particleCount; i++)
        {
            positions[i] = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), 0);    
        }

        mesh = CreateCircleMesh(radius, segments);
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
            Vector2 dir = dst == 0 ? new Vector2(1,0) : offset / dst;
            float slope = SmoothingKernelDerivative(dst, smoothingRadius);
            float density = densities[i];
            float sharedPressure = CalculateSharedPressure(density, densities[particleIndex]);
            pressureForce += -sharedPressure * dir * slope * mass / density;
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
            densities[i] = CalculateDensity(positions[i]);
        });
    }

    float ConvertDensityToPressure(float density)
    {
        float densityError = density - targetDensity;
        float pressure = density * pressureMultiplier;
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
        SimulationStep(deltaTime);

        for (int i = 0; i < particleCount; i++)
        {
            Material material2 = material;
            material2.SetFloat("_ColorParameter", velocity[i].magnitude);
            RenderParams rp2 = new RenderParams(material2);
            Graphics.RenderMesh(rp2, mesh, 0, Matrix4x4.Translate(positions[i]));
        }
    }

    void SimulationStep(float deltaTime)
    {
        Parallel.For(0, particleCount, i =>
        {
            velocity[i] += Vector3.down * gravity * deltaTime;
            densities[i] = CalculateDensity(positions[i]);
        });
        
        Parallel.For(0, particleCount, i =>
        {
            Vector3 pressureForce = -CalculatePressureForce(i);
            Vector3 pressureAcceleration = pressureForce / densities[i];
            velocity[i] += pressureAcceleration * deltaTime;
        });

        Parallel.For(0, particleCount, i =>
        {
            positions[i] += velocity[i] * deltaTime;
            ResolveCollision(i);
        });
    }

    void ResolveCollision(int arrayPosition)
    {
        Vector2 halfBoundSize = boundsSize / 2 - Vector2.one * radius;

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
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(0, 0, 0), size);
        }

        if(drawGizmosGrid == true)
        {
            Vector3 cellSize = new Vector3(smoothingRadius * 2, smoothingRadius * 2);
            Gizmos.color = Color.green;

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

    public uint HashKey(int x, int y)
    {
        uint a = (uint)x * 103387;
        uint b = (uint)y * 96763;
        return a + b;
    }

    public uint CellKey(uint hash)
    {
        return hash % (uint)positions.Length;
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