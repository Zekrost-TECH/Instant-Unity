using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GeometryRenderer : MonoBehaviour
{
    public enum ShapeType
    {
        Triangle,
        Circle,
        Diamond,
        Square,
        Hexagon
    }

    public ShapeType shape = ShapeType.Circle;
    public float size = 1f;
    public Color color = Color.white;
    public float borderWidth = 0.05f;
    public Color borderColor = Color.white;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        GenerateMesh();
    }

    private void OnValidate()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        GenerateMesh();
    }

    public void GenerateMesh()
    {
        if (meshFilter == null) return;

        Mesh mesh = new Mesh();
        Vector3[] vertices;
        int[] triangles;

        switch (shape)
        {
            case ShapeType.Triangle:
                vertices = new Vector3[]
                {
                    new Vector3(0f, size, 0f),
                    new Vector3(-size * 0.577f, -size * 0.5f, 0f),
                    new Vector3(size * 0.577f, -size * 0.5f, 0f)
                };
                triangles = new int[] { 0, 1, 2 };
                break;
            case ShapeType.Circle:
                vertices = GenerateCircleVertices(32);
                triangles = GenerateCircleTriangles(32);
                break;
            case ShapeType.Diamond:
                vertices = new Vector3[]
                {
                    new Vector3(0f, size, 0f),
                    new Vector3(size * 0.5f, 0f, 0f),
                    new Vector3(0f, -size, 0f),
                    new Vector3(-size * 0.5f, 0f, 0f)
                };
                triangles = new int[] { 0, 1, 2, 0, 2, 3 };
                break;
            case ShapeType.Square:
                vertices = new Vector3[]
                {
                    new Vector3(-size * 0.5f, size * 0.5f, 0f),
                    new Vector3(size * 0.5f, size * 0.5f, 0f),
                    new Vector3(size * 0.5f, -size * 0.5f, 0f),
                    new Vector3(-size * 0.5f, -size * 0.5f, 0f)
                };
                triangles = new int[] { 0, 1, 2, 0, 2, 3 };
                break;
            case ShapeType.Hexagon:
                vertices = GenerateHexagonVertices();
                triangles = GenerateHexagonTriangles();
                break;
            default:
                vertices = GenerateCircleVertices(32);
                triangles = GenerateCircleTriangles(32);
                break;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
        if (meshRenderer != null)
            meshRenderer.material.color = color;
    }

    private Vector3[] GenerateCircleVertices(int segments)
    {
        Vector3[] vertices = new Vector3[segments + 1];
        vertices[0] = Vector3.zero;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * size, Mathf.Sin(angle) * size, 0f);
        }
        return vertices;
    }

    private int[] GenerateCircleTriangles(int segments)
    {
        int[] triangles = new int[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % segments + 1;
        }
        return triangles;
    }

    private Vector3[] GenerateHexagonVertices()
    {
        Vector3[] vertices = new Vector3[7];
        vertices[0] = Vector3.zero;
        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.PI / 3f;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * size, Mathf.Sin(angle) * size, 0f);
        }
        return vertices;
    }

    private int[] GenerateHexagonTriangles()
    {
        int[] triangles = new int[18];
        for (int i = 0; i < 6; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % 6 + 1;
        }
        return triangles;
    }

    public void SetColor(Color newColor)
    {
        color = newColor;
        if (meshRenderer != null)
            meshRenderer.material.color = color;
    }
}
