using UnityEngine;

public class Poly6KernelGraph : MonoBehaviour
{
    [Header("Kernel Parameters")]
    [Range(0.1f, 2f)] public float h = 1.0f; // Smoothing length

    [Header("Graph Settings")]
    public float graphWidth = 10f;
    public float graphHeight = 2f;
    public int resolution = 100;
    public Color curveColor = Color.cyan;
    public Color axisColor = Color.white;

    private LineRenderer curveRenderer;
    private LineRenderer xAxisRenderer;
    private LineRenderer yAxisRenderer;
    private LineRenderer hIndicatorRenderer;

    void Start()
    {
        CreateGraphObjects();
        UpdateGraph();
    }

    void OnValidate()
    {
        if (curveRenderer != null) UpdateGraph();
    }

    void CreateGraphObjects()
    {
        // Main curve
        curveRenderer = CreateRenderer("Poly6 Curve", curveColor, 0.1f);

        // X axis
        xAxisRenderer = CreateRenderer("X Axis", axisColor, 0.03f);
        xAxisRenderer.positionCount = 2;
        xAxisRenderer.SetPositions(new Vector3[]{
            new Vector3(-graphWidth/2, 0, 0),
            new Vector3(graphWidth/2, 0, 0)
        });

        // Y axis
        yAxisRenderer = CreateRenderer("Y Axis", axisColor, 0.03f);
        yAxisRenderer.positionCount = 2;
        yAxisRenderer.SetPositions(new Vector3[]{
            new Vector3(-graphWidth/2, 0, 0),
            new Vector3(-graphWidth/2, graphHeight, 0)
        });

        // h indicator
        hIndicatorRenderer = CreateRenderer("h Indicator", Color.red, 0.05f);
        hIndicatorRenderer.positionCount = 2;
    }

    LineRenderer CreateRenderer(string name, Color color, float width)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform);
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Unlit/Color")) { color = color };
        lr.startWidth = width;
        lr.endWidth = width;
        return lr;
    }

    void UpdateGraph()
    {
        // Calculate kernel constant
        float poly6Constant = 315f / (64f * Mathf.PI * Mathf.Pow(h, 9));
        float hSquared = h * h;

        // Generate curve points
        Vector3[] points = new Vector3[resolution];
        for (int i = 0; i < resolution; i++)
        {
            float r = (float)i / (resolution - 1) * graphWidth;
            float value = 0;

            if (r <= h)
            {
                float rSquared = r * r;
                float diff = hSquared - rSquared;
                value = poly6Constant * diff * diff * diff;
            }

            points[i] = new Vector3(
                r - graphWidth / 2,
                value * graphHeight,
                0
            );
        }

        curveRenderer.positionCount = resolution;
        curveRenderer.SetPositions(points);

        // Update h indicator
        hIndicatorRenderer.SetPositions(new Vector3[]{
            new Vector3(h - graphWidth/2, 0, 0),
            new Vector3(h - graphWidth/2, graphHeight, 0)
        });
    }
}