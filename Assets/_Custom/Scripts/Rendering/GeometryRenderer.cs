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

    private const int CircleSegments = 32;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh generatedMesh;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        CacheComponents();
        GenerateMesh();
    }

    private void OnDestroy()
    {
        // La malla se crea por instancia: sin esto queda huérfana en memoria.
        if (generatedMesh == null) return;

        if (Application.isPlaying) Destroy(generatedMesh);
        else DestroyImmediate(generatedMesh);

        generatedMesh = null;
    }

    private void OnValidate()
    {
        CacheComponents();
        GenerateMesh();
    }

    private void CacheComponents()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
    }

    public void GenerateMesh()
    {
        if (meshFilter == null) return;

        // Se reutiliza siempre la misma Mesh. Crear una nueva en cada llamada (Awake +
        // cada cambio del Inspector vía OnValidate) dejaba mallas huérfanas sin liberar.
        if (generatedMesh == null)
        {
            generatedMesh = new Mesh { name = "GeneratedShape" };
            generatedMesh.MarkDynamic();
        }

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
                vertices = GenerateFanVertices(6);
                triangles = GenerateFanTriangles(6);
                break;
            case ShapeType.Circle:
            default:
                vertices = GenerateFanVertices(CircleSegments);
                triangles = GenerateFanTriangles(CircleSegments);
                break;
        }

        generatedMesh.Clear();
        generatedMesh.vertices = vertices;
        generatedMesh.triangles = triangles;
        generatedMesh.RecalculateNormals();
        generatedMesh.RecalculateBounds();

        meshFilter.sharedMesh = generatedMesh;
        ApplyColor();
    }

    private Vector3[] GenerateFanVertices(int segments)
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

    private int[] GenerateFanTriangles(int segments)
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

    private void ApplyColor()
    {
        if (meshRenderer == null) return;

        // MaterialPropertyBlock en vez de .material: no instancia un material por
        // renderer (que además nunca se liberaba) y no rompe el batching.
        CacheComponents();
        meshRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColorId, color);
        propertyBlock.SetColor(ColorId, color);
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    public void SetColor(Color newColor)
    {
        color = newColor;
        ApplyColor();
    }
}
