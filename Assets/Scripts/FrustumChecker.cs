using UnityEngine;


public class FrustumChecker : MonoBehaviour
{
    public Camera playerCamera; // Assign the camera in the Inspector
    //public FrustumTarget[] targets; // Drag and drop all target objects here
    public InvertPainting[] paintings;
    public HatColor[] hats;

    void Update()
    {
        

           CheckFrustum();
        //}
    }

    void CheckFrustum()
    {
        // Get the frustum planes from the camera
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(playerCamera);
        InvertPaint(frustumPlanes);
        ChangeHatColor(frustumPlanes);


    }

    private void InvertPaint(Plane[] frustumPlanes)
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
}
