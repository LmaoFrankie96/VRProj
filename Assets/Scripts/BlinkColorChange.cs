using UnityEngine;
using Varjo.XR; 

public class BlinkColorChange : MonoBehaviour
{
    public Renderer objectRenderer; // Renderer for the object
    public int blinkThreshold = 3; // Number of blinks required to change color
    public float facingAngleThreshold = 15f; // Maximum angle for "facing" condition
    private int blinkCount = 0; // Blink counter
    private bool wasBlinking = false; // Tracks the previous blink state

    void Start()
    {
        if (objectRenderer == null)
        {
            objectRenderer = GetComponent<Renderer>();
        }
    }

    void Update()
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
                Debug.Log("Inside blink condition");
                // Check if the user is facing the object
                if (IsFacingObject())
                {
                    // Increment the blink counter on a new blink while facing the object
                    blinkCount++;
                    Debug.Log($"Blink Count: {blinkCount}");

                    // Check if the blink count has reached the threshold
                    if (blinkCount >= blinkThreshold)
                    {
                        ChangeObjectColor();
                        blinkCount = 0; // Reset the counter
                    }
                }
            }

            // Update the blink state
            wasBlinking = isBlinking;
        }
    }

    bool IsFacingObject()
    {
        // Get the vector pointing from the user (camera) to the object
        Vector3 toObject = (transform.position - Camera.main.transform.position).normalized;

        // Calculate the angle between the user's forward direction and the object
        float angle = Vector3.Angle(Camera.main.transform.forward, toObject);

        // Check if the angle is within the threshold
        return angle <= facingAngleThreshold;
    }

    void ChangeObjectColor()
    {
        // Change the object's color to a random color
        objectRenderer.material.color = new Color(0, 68, 255);
        Debug.Log("Object color changed!");
    }
}
