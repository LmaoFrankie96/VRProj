using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using Varjo.XR;

public class InvertPainting : MonoBehaviour
{
    [Header("Gaze Data")]
    public GazeDataSource gazeDataSource = GazeDataSource.InputSubsystem;

    [Header("Materials")]
    public Material originalMaterial;
    public Material newMaterial;

    private Renderer objRenderer;
    private MeshFilter meshFilter;
    private List<InputDevice> devices = new List<InputDevice>();
    private InputDevice device;
    private Eyes eyes;
    private bool leftClosed;
    private bool rightClosed;
    private bool isUsingOriginalMaterial = true;

    private float blinkCooldown = 1.5f; // 1.5 seconds cooldown between blinks
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
        meshFilter = objRenderer.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("MeshFilter is missing on the object.");
        }

        if (originalMaterial == null || newMaterial == null)
        {
            Debug.LogError("Please assign both the original and new materials.");
        }
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
                            if (Time.time - lastBlinkTime >= blinkCooldown)
                            {
                                lastBlinkTime = Time.time;
                                Debug.Log("Blinked!");
                                ToggleMaterial();
                            }
                        }
                    }
                }
            }
        }
    }

    private bool IsHeadsetWorn()
    {
        // Example: Use a combination of eye calibration and valid gaze data
        return VarjoEyeTracking.IsGazeCalibrated() && device.isValid;
    }

    private void ToggleMaterial()
    {
        if (objRenderer != null)
        {
            if (isUsingOriginalMaterial)
            {
                objRenderer.material = newMaterial;
            }
            else
            {
                objRenderer.material = originalMaterial;
            }

            isUsingOriginalMaterial = !isUsingOriginalMaterial; // Toggle the flag
        }
    }
}
