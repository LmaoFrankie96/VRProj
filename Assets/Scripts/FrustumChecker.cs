using UnityEngine;

public class FrustumChecker : MonoBehaviour
{
    public Camera playerCamera; // Assign the camera in the Inspector
   /* public InvertPainting[] paintings;
    public HatColor[] hats;*/
    public VasePositionChanger vase; // Add this line

    void Update()
    {
        CheckFrustum();
    }

    void CheckFrustum()
    {
        // Get the frustum planes from the camera
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(playerCamera);
        /*InvertPaint(frustumPlanes);
        ChangeHatColor(frustumPlanes);*/
        ChangeVasePosition(frustumPlanes); // Add this line
    }

    /*private void InvertPaint(Plane[] frustumPlanes)
    {
        foreach (InvertPainting target in paintings)
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

    private void ChangeHatColor(Plane[] frustumPlanes)
    {
        foreach (HatColor target in hats)
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
*/
    private void ChangeVasePosition(Plane[] frustumPlanes) // Add this method
    {
        if (vase != null)
        {
            Renderer vaseRenderer = vase.GetComponent<Renderer>();
            if (vaseRenderer != null)
            {
                // Check if the vase's bounds are inside the frustum
                bool isInFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, vaseRenderer.bounds);
                vase.SetFrustumState(isInFrustum);
            }
        }
    }
}