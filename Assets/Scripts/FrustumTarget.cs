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
    private MeshFilter meshFilter;
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
        meshFilter = objRenderer.GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("MeshFilter is missing on the object.");
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
                                ToggleMesh();
                            }
                        }
                    }
                }
            }
        }
    }

    private void ToggleMesh()
    {
        if (meshFilter != null)
        {
            // Check the current mesh type and switch to the other type
            if (meshFilter.sharedMesh.name == "Cube")
            {
                ChangeToSphere();
            }
            else if (meshFilter.sharedMesh.name == "Sphere")
            {
                ChangeToCube();
            }
            else
            {
                Debug.Log("Unknown mesh type. Not changing.");
            }
        }
        else
        {
            Debug.LogError("MeshFilter is missing on the object.");
        }
    }

    private void ChangeToSphere()
    {
        // Create a temporary GameObject with a sphere
        GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);

        // Extract the sphere's Mesh
        Mesh sphereMesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;

        if (sphereMesh != null)
        {
            // Assign the sphere Mesh to this object's MeshFilter
            meshFilter.mesh = sphereMesh;

            Debug.Log("Object mesh changed to a sphere.");
        }
        else
        {
            Debug.LogError("Failed to retrieve sphere mesh.");
        }

        // Destroy the temporary sphere object
        Destroy(tempSphere);
    }

    private void ChangeToCube()
    {
        // Create a temporary GameObject with a cube
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);

        // Extract the cube's Mesh
        Mesh cubeMesh = tempCube.GetComponent<MeshFilter>().sharedMesh;

        if (cubeMesh != null)
        {
            // Assign the cube Mesh to this object's MeshFilter
            meshFilter.mesh = cubeMesh;

            Debug.Log("Object mesh changed to a cube.");
        }
        else
        {
            Debug.LogError("Failed to retrieve cube mesh.");
        }

        // Destroy the temporary cube object
        Destroy(tempCube);
    }

    private bool IsHeadsetWorn()
    {
        // Example: Use a combination of eye calibration and valid gaze data
        return VarjoEyeTracking.IsGazeCalibrated() && device.isValid;
    }
}
