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
    //Circle properties
    [Range(0, 10)]
    public float radius = 0.1f;
    public float smoothingRadius = 0.5f;
    public int segments = 100;
    public float collisionDampening = 0.82f;

    public float density = 0f;

    public float targetDensity;
    public float pressureMultiplier;

    public float deltaTime = 1f;

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
            Vector2 dir = dst == 0 ? new Vector2(1,0) : offset / dst;
            float slope = SmoothingKernelDerivative(dst, smoothingRadius);
            float density = densities[i];
            pressureForce += -ConvertDensityToPressure(density) * dir * slope * mass / density;
        }

        return pressureForce;
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

    float SmoothingKernel(float radius, float density)
    {
        float volume = Mathf.PI * Mathf.Pow(radius, 8) / 4;
        float value = Mathf.Max(0, radius*radius - density*density);
        return value * value * value / volume;
    }

    static float SmoothingKernelDerivative(float dst, float radius)
    {
        if (dst >= radius) return 0;
        float f = radius * radius - dst * dst;
        float scale = -24 / (Mathf.PI * Mathf.Pow(radius, 8));
        return scale * dst * f * f;
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
        /*for (int i = 0; i < positions.Length; i++)
        {
            ResolveCollision(i);

            velocity[i] += Vector3.down * gravity * Time.deltaTime;
            positions[i] += velocity[i] * Time.deltaTime;
            Graphics.RenderMesh(rp, mesh, 0, Matrix4x4.Translate(positions[i]));
        }*/

        for(int i = 0; i < particleCount; i++)
        {
            Graphics.RenderMesh(rp, mesh, 0, Matrix4x4.Translate(positions[i]));
        }

        //density = CalculateDensity();

        //mesh = CreateCircleMesh(radius, segments);

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
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.yellow;
        Vector3 size = boundsSize;
        Gizmos.DrawWireCube(new Vector3(0, 0, 0), size);
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