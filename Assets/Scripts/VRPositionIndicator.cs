using UnityEngine;

public class VRPositionIndicator : MonoBehaviour
{
    [Header("Position Reference")]
    [Tooltip("Drag the transform where the circle should appear")]
    public Transform positionReference;

    [Header("Circle Settings")]
    public float radius = 0.5f;
    public Color color = Color.green;
    public float lineWidth = 0.02f;
    public int segments = 64;
    public float floorOffset = 0.01f;

    private LineRenderer lineRenderer;

    void Start()
    {
        // Create the LineRenderer if it doesn't exist
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        // Set up the LineRenderer
        lineRenderer.useWorldSpace = false;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.material = new Material(Shader.Find("Unlit/Color")) { color = color };
        lineRenderer.loop = true;
        lineRenderer.positionCount = segments + 1;

        // Create the circle geometry
        float angle = 0f;
        for (int i = 0; i < segments + 1; i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;
            lineRenderer.SetPosition(i, new Vector3(x, 0, z));
            angle += 360f / segments;
        }

        // Position the indicator
        if (positionReference != null)
        {
            transform.position = positionReference.position + Vector3.up * floorOffset;
        }
        else
        {
            transform.position = Vector3.up * floorOffset;
        }
    }
}