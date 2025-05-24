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
using static UnityEditor.PlayerSettings;
using Unity.Mathematics;
public class Circle : MonoBehaviour
{
    //Time settings
    public float timeScale = 1;
    public int iterationsPerFrame;
    float deltaTime = 0.0001f;

    public bool gridSpawning = false;

    //Gizmos 
    public bool drawGizmosBoundry = true;
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
    //Circle properties
    [Range(0, 10)]
    public float circleRadius = 0.1f;
    public float smoothingRadius = 0.5f;
    public int segments = 100;
    public float collisionDampening = 0.82f;
    public float spacingFactor = 0.1f;

    public float targetDensity;
    public float viscosityStrength;
    public float nearPressureMultiplier;
    public float pressureMultiplier;

    public bool isLeftPressed = false;
    public bool isRightPressed = false;
    public float mouseForceStrength = 1.0f;
    public float mouseForceRadius = 5.0f;
    public float mass = 1.0f;

    //Circles properties
    private float[] densities;
    private float[] nearDensities;
    private int[] startIndices;
    private Vector3[] velocity;
    private Matrix4x4[] positionsMatrices;
    private Vector3[] positions;
    private Vector3[] predictedPositions;
    public int particleCount = 1;

    public NeighbourSearch fixedNeighbour;

    //Our universe properties
    public float gravity = 9.81f;
    public Vector2 boundsSize = new Vector2(10, 10);


    private Mesh mesh;
    RenderParams rp;

    public Material material;

    private float poly6Constant;
    private float hSquared;

    public Vector3[] getPositions()
    {
        return positions;
    }

    void CalculatePoly6Constant()
    {
        // 315 / (64 * pi * h^9)
        poly6Constant = 315f / (64f * Mathf.PI * Mathf.Pow(smoothingRadius, 9));
        hSquared = smoothingRadius * smoothingRadius;
    }

    float Poly6Kernel(float rSquared)
    {
        if (rSquared >= hSquared) return 0f;

        float diff = hSquared - rSquared;
        return poly6Constant * diff * diff * diff;
    }

    void Start()
    {
        CalculatePoly6Constant();
        fixedNeighbour = gameObject.GetComponent<NeighbourSearch>();
        fixedNeighbour.particleCount = particleCount;
        fixedNeighbour.smoothingRadius = smoothingRadius;
        positions = new Vector3[particleCount];
        velocity = new Vector3[particleCount];
        positionsMatrices = new Matrix4x4[particleCount];
        predictedPositions = new Vector3[particleCount];
        startIndices = new int[particleCount];

        densities = new float[particleCount];
        nearDensities = new float[particleCount];

        //Random circle placement
        float minX = boundsSize.x / 2 * -1 + circleRadius;
        float maxX = boundsSize.x / 2 - circleRadius;

        float minY = boundsSize.y / 2 * -1 + circleRadius;
        float maxY = boundsSize.y / 2 - circleRadius;


        if (gridSpawning)
        {
            // Grid spacing
            int particlesPerRow = (int)Mathf.Sqrt(particleCount);
            int particlesPerCol = (particleCount - 1) / particlesPerRow + 1;
            float spacing = circleRadius * 2 + 0.01f + spacingFactor;

            for (int i = 0; i < particleCount; i++)
            {
                float x = (i % particlesPerRow - particlesPerRow / 2f + 0.5f) * spacing;
                float y = (i / particlesPerRow - particlesPerCol / 2f + 0.5f) * spacing;
                positions[i] = new Vector3(x, y, 0);
            }
        }
        else
        {
            for (int i = 0; i < particleCount; i++)
            {
                //Random circle placement
                positions[i] = new Vector3(UnityEngine.Random.Range(minX, maxX), UnityEngine.Random.Range(minY, maxY), 0);
            }
        }

        mesh = CreateCircleMesh(circleRadius, segments);
        rp = new RenderParams(material)
        {
            layer = 0, // Default layer
            shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off,
            receiveShadows = false
        };
    }

    Vector2 CalculatePressureForce(int particleIndex, List<int> neighbours)
    {
        Vector2 pressureForce = Vector2.zero;
        Vector3 myPosition = positions[particleIndex];

        foreach (int i in neighbours)
        {
            if (i == particleIndex) continue;
            Vector3 offset = positions[i] - myPosition;
            float dst = offset.magnitude;
            if (dst > smoothingRadius) continue;
            Vector2 dir = dst == 0 ? new Vector2(0, 1) : offset / dst;
            float slope = SmoothingKernelDerivative(dst, smoothingRadius);
            float density = densities[i];
            float nearDensity = nearDensities[i];
            float sharedPressure = CalculateSharedPressure(density, densities[particleIndex]);
            float nearSharedPressure = CalculateSharedNearPressure(nearDensity, nearDensities[particleIndex]);
            pressureForce += mass * nearSharedPressure * dir / nearDensity;
            pressureForce += mass * sharedPressure * nearSharedPressure * slope * dir / density;
        }

        return pressureForce;
    }

    float CalculateSharedNearPressure(float nearDensityA, float nearDensityB)
    {
        float nearPressureA = ConvertNearDensityToPressure(nearDensityA);
        float nearPressureB = ConvertNearDensityToPressure(nearDensityB);
        return (nearPressureA + nearPressureB) / 2;
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

    float ConvertNearDensityToPressure(float nearDensity)
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

    static float SmoothingKernelDerivative(float dst, float radius)
    {
        if (dst >= radius) return 0;

        float scale = 12 / (Mathf.PI * Mathf.Pow(radius, 4));
        return (dst - radius) * scale;
    }

    static float ViscositySmoothingKernel(float dst, float radius)
    {
        float volume = Mathf.PI * Mathf.Pow(radius, 8) / 4;
        float value = Mathf.Max(0, radius * radius - dst * dst);
        return value * value * value / volume;
    }

    Vector3 CalculateViscosityForce(int particleIndex, List<int> neighbours)
    {
        Vector3 viscosityForce = Vector3.zero;
        Vector3 positionsInner = positions[particleIndex];

        foreach(int i in neighbours)
        {
            float dst = (positionsInner - positions[i]).magnitude;
            if (dst > smoothingRadius) continue;
            float influence = ViscositySmoothingKernel(dst, smoothingRadius);
            viscosityForce += (velocity[i] - velocity[particleIndex]) * influence;
        }

        return viscosityForce * viscosityStrength;
    }

    /*static float SmoothingDensityKernel(float radius, float distance)
    {
        float v = radius - distance;
        float volume = (Mathf.PI * Mathf.Pow(radius, 4)) / 6;
        return v * v / volume;
    }

    float CalculateDensity(Vector3 particlePosition)
    {
        float density = 0;

        foreach(Vector3 position in predictedPositions)
        {
            float dst = (position - particlePosition).magnitude;
            float influence = SmoothingDensityKernel(smoothingRadius, dst);
            density += influence;
        }

        return density;
    }*/

    void LateUpdate()
    {
        Parallel.For(0, particleCount, i =>
        {
            positionsMatrices[i] = Matrix4x4.Translate(positions[i]);
        });
        Graphics.DrawMeshInstanced(mesh, 0, material, positionsMatrices);

    }

    void Update()
    { 
        if (Time.frameCount > 10)
        {
            RunSimulationFrame(Time.deltaTime);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(0);
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            SceneManager.LoadScene(1);
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
        isLeftPressed = Input.GetMouseButton(0);
        isRightPressed = Input.GetMouseButton(1);

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Parallel.For(0, particleCount, i =>
        {
            if (isLeftPressed || isRightPressed)
                velocity[i] += InteractionForce(mousePos, mouseForceRadius, i);

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

            velocity[i] += viscosityForce + pressureAcceleration * deltaTime;
        });


        Parallel.For(0, particleCount, i =>
        {
            positions[i] += velocity[i] * deltaTime;
            ResolveCollision(i);
        });
    }

    Vector3 InteractionForce(Vector2 inputPos, float radius, int particleIndex)
    {
        if (isRightPressed)
        {
            mouseForceStrength = -1 * Mathf.Abs(mouseForceStrength);
        }
        else if (isLeftPressed)
        {
            mouseForceStrength = Mathf.Abs(mouseForceStrength);
        }
        Vector3 inputPos3D = new Vector3(inputPos.x, inputPos.y, 0);
        Vector3 interactionForce = Vector3.zero;
        Vector3 offset = inputPos3D - positions[particleIndex];
        float sqrDst = Vector3.Dot(offset, offset);

        if(sqrDst < radius * radius) 
        {
            float dst = Mathf.Sqrt(sqrDst);
            Vector3 dirToInputPoint = dst <= float.Epsilon ? Vector3.zero : offset / dst;
            float centreT = 1 - dst / radius;
            interactionForce += (dirToInputPoint * mouseForceStrength - velocity[particleIndex]) * centreT;
        }
        return interactionForce;
    }

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

    public void InsideRadiusInfluenceDensity(Vector3 point, int myIndex)
    {
        float density = 0;
        float nearDensity = 0;

        (int cellX, int cellY) = fixedNeighbour.GetGridLocation(point);
        foreach ((int offsetX, int offsetY) in cellOffsets)
        {
            uint key = fixedNeighbour.GetCellKey(fixedNeighbour.GetHash(cellX + offsetX, cellY + offsetY), particleCount);
            int cellStartIndex = startIndices[key];

            for (int i = cellStartIndex; i < fixedNeighbour.gridLookup.Length; i++)
            {
                if (fixedNeighbour.gridLookup[i].cellKey != key) break;

                int particleIndex = fixedNeighbour.gridLookup[i].particleIndex;

                float sqrDst = (positions[particleIndex] - point).sqrMagnitude;
                if (sqrDst <= smoothingRadius * smoothingRadius)
                {
                    float influence = Poly6Kernel(sqrDst);
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


            bool isPullInteraction = Input.GetMouseButton(0);
            if (isRightPressed || isLeftPressed)
            {
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Gizmos.color = isRightPressed ? UnityEngine.Color.green : UnityEngine.Color.red;
                Gizmos.DrawWireSphere(mousePos, mouseForceRadius);
            }
        }
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