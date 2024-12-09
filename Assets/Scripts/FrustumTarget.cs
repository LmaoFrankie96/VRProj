using UnityEngine;
using Varjo.XR;

public class FrustumTarget : MonoBehaviour
{
    public Color changedColor = Color.red; // Color to change to when looked away
    private bool hasBeenLookedAt = false; // Tracks if the object has been looked at
    private bool isInFrustum = false; // Tracks if the object is currently in the frustum
    private Renderer objRenderer;
    private int blinkCount;
    void Start()
    {
        objRenderer = GetComponent<Renderer>();
        blinkCount = 0;
    }

    public void SetFrustumState(bool inFrustum)
    {
        if (inFrustum)
        {
            // Check if eye tracking data is available
            if (VarjoEyeTracking.IsGazeCalibrated())
            {
                // Debug.Log("Inside gaze function");
                var eyedata1 = VarjoEyeTracking.GetEyeMeasurements();

                // Get eye openness values for left and right eyes
                float leftEyeOpenness = eyedata1.leftEyeOpenness;
                float rightEyeOpenness = eyedata1.rightEyeOpenness;


                // Detect a blink (when both eyes are closed)
                bool isBlinking = leftEyeOpenness < 0.1f; //&& rightEyeOpenness <0.1f;

                if (isBlinking)
                {
                    blinkCount++;
                    Debug.Log("Blinked " + blinkCount + " times");
                    ChangeColor();


                }


            }
        }
        /*else
        {
            // If leaving the frustum and it has been looked at, change color
            if (isInFrustum && hasBeenLookedAt)
            {
                // Check if eye tracking data is available
                if (VarjoEyeTracking.IsGazeCalibrated())
                {
                    // Debug.Log("Inside gaze function");
                    var eyedata1 = VarjoEyeTracking.GetEyeMeasurements();

                    // Get eye openness values for left and right eyes
                    float leftEyeOpenness = eyedata1.leftEyeOpenness;
                    float rightEyeOpenness = eyedata1.rightEyeOpenness;


                    // Detect a blink (when both eyes are closed)
                    bool isBlinking = leftEyeOpenness < 0.1f; //&& rightEyeOpenness <0.1f;

                    if (isBlinking)
                    {
                        blinkCount++;
                        Debug.Log("Blinked "+blinkCount+" times");
                        ChangeColor();


                    }

                    
                }
                isInFrustum = false; // Update state
                
            }
        }*/
    }

    private void ChangeColor()
    {
        objRenderer.material.color = Random.ColorHSV();
    }
}
