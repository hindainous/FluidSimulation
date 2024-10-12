using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Circle : MonoBehaviour
{
    //Circle properties
    [Range(0, 10)]
    public float radius = 1f;
    public int segments = 100;
    public float collisionDampening = 0.82f;

    //Circles properties
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

    void Update()
    {
        for (int i = 0; i < positions.Length; i++)
        {
            ResolveCollision(i);

            velocity[i] += Vector3.down * gravity * Time.deltaTime;
            positions[i] += velocity[i] * Time.deltaTime;
            Graphics.RenderMesh(rp, mesh, 0, Matrix4x4.Translate(positions[i]));
        }

        mesh = CreateCircleMesh(radius, segments);

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