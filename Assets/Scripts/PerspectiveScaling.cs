using UnityEngine;

public class PerspectiveScaling : MonoBehaviour
{
    public Transform cameraTransform; // Assign the main camera
    public float initialDistance = 2f; // Initial distance from the camera
    private Vector3 initialScale; // Store the original scale

    void Start()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Store the original scale
        initialScale = transform.localScale;
    }

    void Update()
    {
        // Move the object away from the camera
        float newDistance = initialDistance + Time.time * 2f; // Moves away over time
        transform.position = cameraTransform.position + cameraTransform.forward * newDistance;

        // Scale the object proportionally to its distance
        float scaleMultiplier = newDistance / initialDistance;
        transform.localScale = initialScale * scaleMultiplier;
        Debug.Log("Distance from camera is: "+newDistance);
    }
}
