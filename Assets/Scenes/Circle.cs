using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Mathematics;

public class Circle : MonoBehaviour
{
    public float radius = 1f;
    public int segments = 100;
    public Vector2 boundsSize = new Vector2(10,10);
    public float collisionDampening = 0;
    private MeshFilter meshFilter;
    public Material material;
    public float gravity = -9.81f;
    Vector3 velocity;
    Vector3 position;

    void Start()
    {
        meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();

        meshFilter.mesh = CreateCircleMesh(radius, segments);
        renderer.material = material;
    }

    void Update()
    {
        ResolveCollision();

        velocity += Vector3.down * gravity * Time.deltaTime;
        position += velocity * Time.deltaTime;
        gameObject.transform.position = position;
    }

    void ResolveCollision()
    {
        Vector2 halfBoundSize = boundsSize / 2 - Vector2.one * radius;

        if(Mathf.Abs(position.x) > halfBoundSize.x)
        {
            position.x = halfBoundSize.x * Mathf.Sign(position.x);
            velocity.x *= -1 * collisionDampening;
        }

        if (Mathf.Abs(position.y) > halfBoundSize.y)
        {
            position.y = halfBoundSize.y * Mathf.Sign(position.y);
            velocity.y *= -1 * collisionDampening;
        }
    }

    void OnDrawGizmos()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.yellow;
        Vector3 size = boundsSize;
        Gizmos.DrawWireCube(new Vector3(0,0,0), size);
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