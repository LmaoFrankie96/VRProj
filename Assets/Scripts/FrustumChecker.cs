using UnityEngine;

public class FrustumChecker : MonoBehaviour
{
    public Camera playerCamera; // Assign the camera in the Inspector
    public GameObject distractorObject; // Reference to the distractor object
    public ExperimentManager experimentManager; // Reference to the ExperimentManager to notify about frustum status

    void Update()
    {
        CheckFrustum();
    }

    void CheckFrustum()
    {
        // Get the frustum planes from the camera
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(playerCamera);

        // Check if the distractor object is inside the frustum
        Renderer distractorRenderer = distractorObject.GetComponent<Renderer>();
        if (distractorRenderer != null)
        {
            bool isInFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, distractorRenderer.bounds);
            experimentManager.SetDistractorInFrustum(isInFrustum); // Notify ExperimentManager
        }
    }
}
