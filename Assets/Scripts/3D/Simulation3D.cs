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
using UnityEngine.SceneManagement;
public class Simulation3D : MonoBehaviour
{
    //Time settings
    public float timeScale = 1;
    public int iterationsPerFrame;
    float deltaTime = 0.0001f;

    public bool gridSpawning = false;

    //Gizmos 
    public bool drawGizmosBoundry = true;
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
    //Circle properties
    [Range(0, 10)]
    public float circleRadius = 0.1f;
    public float smoothingRadius = 0.5f;
    public int longitudeSegments = 24;
    public int latitudeSegments = 16;
    public float collisionDampening = 0.82f;
    public float spacingFactor = 0.1f;

    public float targetDensity;
    public float viscosityStrength;
    public float pressureMultiplier;
    public float nearPressureMultiplier;

    public float mass = 1.0f;

    //Circles properties
    private MaterialPropertyBlock propertyBlock;

    private float[] speeds;
    private float[] densities;
    private float[] nearDensities;
    private int[] startIndices;
    private Vector3[] velocity;
    private Matrix4x4[] positionsMatrices;
    private Vector3[] positions;
    private Vector3[] predictedPositions;
    public int particleCount = 1;

    public NeighbourSearch3D fixedNeighbour;

    //Our universe properties
    public float gravity = 9.81f;
    public Vector3 boundsSize = new Vector3(10, 10, 10);


    private Mesh mesh;
    RenderParams rp;

    public Material material;

    public Vector3[] getPositions()
    {
        return positions;
    }

    void Start()
    {
        fixedNeighbour = gameObject.GetComponent<NeighbourSearch3D>();
        fixedNeighbour.particleCount = particleCount;
        fixedNeighbour.smoothingRadius = smoothingRadius;
        positions = new Vector3[particleCount];
        velocity = new Vector3[particleCount];
        positionsMatrices = new Matrix4x4[particleCount];
        predictedPositions = new Vector3[particleCount];
        startIndices = new int[particleCount];

        speeds = new float[particleCount];
        densities = new float[particleCount];
        nearDensities = new float[particleCount];

        //Random circle placement
        float minX = boundsSize.x / 2 * -1 + circleRadius;
        float maxX = boundsSize.x / 2 - circleRadius;

        float minY = boundsSize.y / 2 * -1 + circleRadius;
        float maxY = boundsSize.y / 2 - circleRadius;

        float minZ = boundsSize.z / 2 * -1 + circleRadius;
        float maxZ = boundsSize.z / 2 - circleRadius;

        if (gridSpawning)
        {
            // Grid spacing
            int cubeRoot = Mathf.CeilToInt(Mathf.Pow(particleCount, 1f / 3f));
            int particlesPerRow = cubeRoot;
            int particlesPerCol = cubeRoot;
            int particlesPerSlice = (particleCount - 1) / particlesPerRow + 1;
            float spacing = circleRadius * 2 + 0.01f + spacingFactor;

            for (int i = 0; i < particleCount; i++)
            {
                int sliceIndex = i / (particlesPerRow * particlesPerCol); // Which slice (z-axis)
                int rowIndex = (i % (particlesPerRow * particlesPerCol)) / particlesPerRow; // Which row (y-axis)
                int colIndex = i % particlesPerRow; // Which column (x-axis)

                float x = (colIndex - particlesPerRow / 2f + 0.5f) * spacing;
                float y = (rowIndex - particlesPerCol / 2f + 0.5f) * spacing;
                float z = (sliceIndex - particlesPerCol / 2f + 0.5f) * spacing;
                positions[i] = new Vector3(x, y, z);
            }
        }
        else
        {
            for (int i = 0; i < particleCount; i++)
            {
                //Random circle placement
                positions[i] = new Vector3(UnityEngine.Random.Range(minX, maxX), UnityEngine.Random.Range(minY, maxY), UnityEngine.Random.Range(minZ, maxZ));
            }
        }

        mesh = CreateSphereMesh();
        rp = new RenderParams(material)
        {
            layer = 0, // Default layer
            shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
            receiveShadows = false
        };
    }

    Vector3 CalculatePressureForce(int particleIndex, List<int> neighbours)
    {
        Vector3 pressureForce = Vector3.zero;
        Vector3 myPosition = predictedPositions[particleIndex];

        foreach (int i in neighbours)
        {
            if (i == particleIndex) continue;
            Vector3 offset = predictedPositions[i] - myPosition;
            float dst = offset.magnitude;
            if (dst > smoothingRadius) continue;
            Vector3 dir = dst == 0 ? new Vector3(0, 1, -1) : offset / dst;
            float slope = SmoothingKernelDerivative(dst, smoothingRadius);
            float density = densities[i];
            float nearDensity = nearDensities[i];
            float sharedPressure = CalculateSharedPressure(density, densities[particleIndex]);
            float sharedNearPressure = CalculateSharedNearPressure(nearDensity, nearDensities[particleIndex]);
            pressureForce += mass * sharedNearPressure * dir / nearDensity;
            pressureForce += mass * sharedPressure * slope * dir / density;
        }

        return pressureForce;
    }


    float CalculateSharedNearPressure(float NearDensityA, float NearDensityB)
    {
        float NearPressureA = ConvertDensityToNearPressure(NearDensityA);
        float NearPressureB = ConvertDensityToNearPressure(NearDensityB);
        return (NearPressureA + NearPressureB) / 2;
    }

    float CalculateSharedPressure(float densityA, float densityB)
    {
        float pressureA = ConvertDensityToPressure(densityA);
        float pressureB = ConvertDensityToPressure(densityB);
        return (pressureA + pressureB) / 2;
    }

    /*void UpdateDensities()
    {
        Parallel.For(0, particleCount, i =>
        {
            densities[i] = CalculateDensity(predictedPositions[i]);
        });
    }*/

    float ConvertDensityToNearPressure(float nearDensity)
    {
        float pressure = nearDensity * nearPressureMultiplier;
        return pressure;
    }

    float ConvertDensityToPressure(float density)
    {
        float densityError = density - targetDensity;
        float pressure = densityError * pressureMultiplier;
        return pressure;
    }

    static float SmoothingDensityKernel(float radius, float distance)
    {
        if (distance < radius)
        {
            float v = radius - distance;
            float volume = (2 * Mathf.PI * Mathf.Pow(radius, 5)) / 15;
            return v * v / volume;
        }
        return 0;
    }

    static float SmoothingKernelDerivative(float dst, float radius)
    {
        if (dst >= radius) return 0;

        float scale = 15 / (Mathf.PI * Mathf.Pow(radius, 5));
        return (dst - radius) * scale;
    }

    static float ViscositySmoothingKernel(float dst, float radius)
    {
        float volume = 64 * Mathf.PI * Mathf.Pow(radius, 9) / 315;
        float value = Mathf.Max(0, radius * radius - dst * dst);
        return value * value * value / volume;
    }

    Vector3 CalculateViscosityForce(int particleIndex, List<int> neighbours)
    {
        Vector3 viscosityForce = Vector3.zero;
        Vector3 positionsInner = positions[particleIndex];

        foreach (int i in neighbours)
        {
            float dst = (positionsInner - positions[i]).magnitude;
            if (dst > smoothingRadius) continue;
            float influence = ViscositySmoothingKernel(dst, smoothingRadius);
            viscosityForce += (velocity[i] - velocity[particleIndex]) * influence;
        }

        return viscosityForce * viscosityStrength * deltaTime;
    }

    // Old code
    /*float CalculateDensity(Vector3 particlePosition)
    {
        float density = 0;

        foreach (Vector3 position in predictedPositions)
        {
            float dst = (position - particlePosition).magnitude;
            float influence = SmoothingDensityKernel(smoothingRadius, dst);
            density += influence;
        }

        return density;
    }*/

    void LateUpdate()
    {
        propertyBlock = new MaterialPropertyBlock();
        for (int i = 0; i < particleCount; i++)
        {
            positionsMatrices[i] = Matrix4x4.Translate(positions[i]);
            speeds[i] = velocity[i].magnitude;
        }
        propertyBlock.SetFloatArray("_Speed", speeds);
        Graphics.DrawMeshInstanced(mesh, 0, material, positionsMatrices, particleCount, propertyBlock);

    }

    void Update()
    {
        if (Time.frameCount > 10)
        {
            RunSimulationFrame(Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(1);
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            SceneManager.LoadScene(0);
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

        startIndices = fixedNeighbour.UpdateGridLookup(predictedPositions);

        Parallel.For(0, particleCount, i =>
        {
            InsideRadiusInfluenceDensity(predictedPositions[i], i);
        });

        Parallel.For(0, particleCount, i =>
        {
            List<int> neighbours = fixedNeighbour.GetNeighbours(positions[i]);
            Vector3 pressureForce = CalculatePressureForce(i, neighbours);
            Vector3 pressureAcceleration = pressureForce / densities[i];
            Vector3 viscosityForce = CalculateViscosityForce(i, neighbours);

            velocity[i] += viscosityForce;
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
        Vector3 halfBoundSize = boundsSize / 2 - Vector3.one * circleRadius;

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

        if (Mathf.Abs(positions[arrayPosition].z) > halfBoundSize.z)
        {
            positions[arrayPosition].z = halfBoundSize.z * Mathf.Sign(positions[arrayPosition].z);
            velocity[arrayPosition].z *= -1 * collisionDampening;
        }
    }

    public void InsideRadiusInfluenceDensity(Vector3 point, int myIndex)
    {
        float density = 0;
        float nearDensity = 0;

        (int cellX, int cellY, int cellZ) = fixedNeighbour.GetGridLocation(point);
        foreach ((int offsetX, int offsetY, int offSetZ) in cellOffsets)
        {
            uint key = fixedNeighbour.GetCellKey(fixedNeighbour.GetHash(cellX + offsetX, cellY + offsetY, cellZ + offSetZ), particleCount);
            int cellStartIndex = startIndices[key];

            for (int i = cellStartIndex; i < fixedNeighbour.gridLookup.Length; i++)
            {
                if (fixedNeighbour.gridLookup[i].cellKey != key) break;

                int particleIndex = fixedNeighbour.gridLookup[i].particleIndex;

                float sqrDst = (positions[particleIndex] - point).sqrMagnitude;
                if (sqrDst <= smoothingRadius * smoothingRadius)
                {
                    float influence = SmoothingDensityKernel(smoothingRadius, sqrDst);
                    density += mass * influence;
                    nearDensity += mass * influence;
                }
            }
        }
        densities[myIndex] = density;
        nearDensities[myIndex] = nearDensity;
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
    }

    Mesh CreateSphereMesh()
    {
        Mesh mesh = new Mesh();
        int vertexCount = (longitudeSegments + 1) * (latitudeSegments + 1);
        Vector3[] vertices = new Vector3[vertexCount];
        Vector3[] normals = new Vector3[vertexCount];
        Vector2[] uv = new Vector2[vertexCount];
        int[] triangles = new int[longitudeSegments * latitudeSegments * 6];

        // Generate vertices, normals, and UVs
        int vertexIndex = 0;
        for (int lat = 0; lat <= latitudeSegments; lat++)
        {
            float latAngle = Mathf.PI * lat / latitudeSegments; // Latitude angle (0 to PI)
            float y = Mathf.Cos(latAngle); // Y position
            float radiusXZ = Mathf.Sin(latAngle); // Radius in the XZ plane

            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                float lonAngle = 2 * Mathf.PI * lon / longitudeSegments; // Longitude angle (0 to 2*PI)
                float x = Mathf.Cos(lonAngle) * radiusXZ; // X position
                float z = Mathf.Sin(lonAngle) * radiusXZ; // Z position

                vertices[vertexIndex] = new Vector3(x, y, z) * circleRadius;
                normals[vertexIndex] = vertices[vertexIndex].normalized;
                uv[vertexIndex] = new Vector2((float)lon / longitudeSegments, (float)lat / latitudeSegments);

                vertexIndex++;
            }
        }

        // Generate triangles
        int triangleIndex = 0;
        for (int lat = 0; lat < latitudeSegments; lat++)
        {
            for (int lon = 0; lon < longitudeSegments; lon++)
            {
                int current = lat * (longitudeSegments + 1) + lon;
                int next = current + longitudeSegments + 1;

                // First triangle of the quad
                triangles[triangleIndex++] = current;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = current + 1;

                // Second triangle of the quad
                triangles[triangleIndex++] = current + 1;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = next + 1;
            }
        }

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = triangles;

        mesh.RecalculateBounds();
        return mesh;
    }
}