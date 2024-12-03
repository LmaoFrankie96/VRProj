using UnityEngine;

public class FrustumCheck : MonoBehaviour
{
    public Camera mainCamera; // Assign the camera in the inspector
    public string targetTag = "Target"; // The tag of objects to check
    public Color insideColor = Color.red; // Color when the object is inside the frustum
    public Color outsideColor = Color.white; // Color when the object is outside the frustum

    void Update()
    {
        // Get the frustum planes from the camera
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);

        // Find all objects with the specified tag
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(targetTag);

        foreach (GameObject obj in taggedObjects)
        {
            Renderer objRenderer = obj.GetComponent<Renderer>();
            if (objRenderer != null)
            {
                // Get the object's bounds
                Bounds objBounds = objRenderer.bounds;

                // Check if the object's bounds intersect the frustum
                if (GeometryUtility.TestPlanesAABB(frustumPlanes, objBounds))
                {
                    // Object is inside the frustum
                    objRenderer.material.color = insideColor;
                }
                else
                {
                    // Object is outside the frustum
                    objRenderer.material.color = outsideColor;
                }
            }
        }
    }
}
