using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using Varjo.XR;

public class FrustumTarget : MonoBehaviour
{
    [Header("Gaze Data")]
    public GazeDataSource gazeDataSource = GazeDataSource.InputSubsystem;

    private Renderer objRenderer;
    private List<InputDevice> devices = new List<InputDevice>();
    private InputDevice device;
    private Eyes eyes;
    private bool leftClosed;
    private bool rightClosed;

    private float blinkCooldown = 1.5f; // 0.5 seconds cooldown between blinks
    private float lastBlinkTime = 0;

    void GetDevice()
    {
        InputDevices.GetDevicesAtXRNode(XRNode.CenterEye, devices);
        device = devices.FirstOrDefault();
    }

    void OnEnable()
    {
        if (!device.isValid)
        {
            GetDevice();
        }
    }

    void Start()
    {
        objRenderer = GetComponent<Renderer>();
    }

    public void SetFrustumState(bool inFrustum)
    {
        if (inFrustum)
            EyeTracking();
    }

    private void EyeTracking()
    {
        // Ensure gaze is allowed, calibrated, and the device is valid
        if (VarjoEyeTracking.IsGazeAllowed() && VarjoEyeTracking.IsGazeCalibrated())
        {
            if (!device.isValid)
            {
                GetDevice();
            }

            if (gazeDataSource == GazeDataSource.InputSubsystem)
            {
                // Get data for eye positions, rotations, and the fixation point
                if (device.TryGetFeatureValue(CommonUsages.eyesData, out eyes))
                {
                    if (eyes.TryGetLeftEyeOpenAmount(out float leftEyeOpenness) &&
                        eyes.TryGetRightEyeOpenAmount(out float rightEyeOpenness))
                    {
                        Debug.Log($"Left eye openness: {leftEyeOpenness}, Right eye openness: {rightEyeOpenness}");

                        leftClosed = leftEyeOpenness < 0.1f;
                        rightClosed = rightEyeOpenness < 0.1f;

                        if (leftClosed && rightClosed && IsHeadsetWorn())
                        {
                            //Debug.Log("Blinked")
                            // Check if enough time has passed since the last blink
                            if (Time.time - lastBlinkTime >= blinkCooldown)
                            {
                                lastBlinkTime = Time.time;
                                Debug.Log("Blinked!");
                                ChangeColor();
                            }
                        }
                    }
                }
            }
        }
    }

    private void ChangeColor()
    {
        objRenderer.material.color = Random.ColorHSV();
    }

    private bool IsHeadsetWorn()
    {
        // Example: Use a combination of eye calibration and valid gaze data
        return VarjoEyeTracking.IsGazeCalibrated() && device.isValid;
    }
}
