using UnityEngine;
using Varjo.XR;

public class FrustumChecker : MonoBehaviour
{
    public Camera playerCamera; // Assign the camera in the Inspector
    public FrustumTarget[] targets; // Drag and drop all target objects here

    void Update()
    {
        /*if (VarjoEyeTracking.IsGazeCalibrated())
        {*/
            // Debug.Log("Inside gaze function");
            var eyedata1 = VarjoEyeTracking.GetEyeMeasurements();

            // Get eye openness values for left and right eyes
            float leftEyeOpenness = eyedata1.leftEyeOpenness;
            float rightEyeOpenness = eyedata1.rightEyeOpenness;
            Debug.Log("Openness: " + leftEyeOpenness);

            // Detect a blink (when both eyes are closed)
            bool isBlinking = leftEyeOpenness < 0.3f; //&& rightEyeOpenness < 0.1f;

            if (isBlinking)
            {
                Debug.Log("Blinked!");
            }

           // CheckFrustum();
        //}
    }

    void CheckFrustum()
    {
        // Get the frustum planes from the camera
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(playerCamera);

        foreach (FrustumTarget target in targets)
        {
            if (target != null)
            {
                Renderer targetRenderer = target.GetComponent<Renderer>();
                if (targetRenderer != null)
                {
                    // Check if the target's bounds are inside the frustum
                    bool isInFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, targetRenderer.bounds);
                    target.SetFrustumState(isInFrustum);
                }
            }
        }
    }
}
