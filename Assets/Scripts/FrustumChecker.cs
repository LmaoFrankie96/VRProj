using UnityEngine;
using Varjo.XR;

public class FrustumChecker : MonoBehaviour
{
    public Camera playerCamera; // Assign the camera in the Inspector
    public FrustumTarget[] targets; // Drag and drop all target objects here

    void Update()
    {
        

           CheckFrustum();
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
