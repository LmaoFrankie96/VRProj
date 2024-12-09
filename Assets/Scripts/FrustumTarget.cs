using UnityEngine;

public class FrustumTarget : MonoBehaviour
{
    public Color changedColor = Color.red; // Color to change to when looked away
    private bool hasBeenLookedAt = false; // Tracks if the object has been looked at
    private bool isInFrustum = false; // Tracks if the object is currently in the frustum
    private Renderer objRenderer;

    void Start()
    {
        objRenderer = GetComponent<Renderer>();
    }

    public void SetFrustumState(bool inFrustum)
    {
        if (inFrustum)
        {
            // If entering the frustum for the first time, mark as looked at
            if (!hasBeenLookedAt)
            {
                hasBeenLookedAt = true;
                isInFrustum = true;
            }
        }
        else
        {
            // If leaving the frustum and it has been looked at, change color
            if (isInFrustum && hasBeenLookedAt)
            {
                isInFrustum = false; // Update state
                ChangeColor();
            }
        }
    }

    private void ChangeColor()
    {
        objRenderer.material.color = changedColor;
    }
}
