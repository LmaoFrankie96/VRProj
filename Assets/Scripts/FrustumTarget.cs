using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using Varjo.XR;

public class FrustumTarget : MonoBehaviour
{

    [Header("Gaze data")]
    public GazeDataSource gazeDataSource = GazeDataSource.InputSubsystem;

    //public Color changedColor = Color.red; // Color to change to when looked away
    private bool hasBeenLookedAt = false; // Tracks if the object has been looked at
    private bool isInFrustum = false; // Tracks if the object is currently in the frustum
    private Renderer objRenderer;
    private int blinkCount;

    private List<InputDevice> devices = new List<InputDevice>();
    private InputDevice device;
    private Eyes eyes;
    private bool leftClosed;
    private bool rightClosed;


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
        blinkCount = 0;
    }

    public void SetFrustumState(bool inFrustum)
    {
        
            if (inFrustum)
                EyeTracking();
        

        
        
    }
    private void EyeTracking()
    {

        // Get gaze data if gaze is allowed and calibrated
        if (VarjoEyeTracking.IsGazeAllowed() && VarjoEyeTracking.IsGazeCalibrated())
        {
            //Get device if not valid
            if (!device.isValid)
            {
                GetDevice();
            }

            // Show gaze target
            // gazeTarget.SetActive(true);

            if (gazeDataSource == GazeDataSource.InputSubsystem)
            {
                // Get data for eye positions, rotations and the fixation point
                if (device.TryGetFeatureValue(CommonUsages.eyesData, out eyes))
                {

                    if (eyes.TryGetLeftEyeOpenAmount(out float leftEyeOpenness))
                    {

                        Debug.Log("Left eye openness: " + leftEyeOpenness);
                        if (leftEyeOpenness < 0.3f)
                        {

                            leftClosed = true;
                        }
                        else
                        {
                            leftClosed = false;
                        }
                    }
                    if (eyes.TryGetRightEyeOpenAmount(out float rightEyeOpenness))
                    {

                        Debug.Log("Right eye openness: " + rightEyeOpenness);
                        if (rightEyeOpenness < 0.3f)
                        {

                            rightClosed = true;
                        }
                        else
                        {
                            rightClosed = false;
                        }
                    }
                    if (leftClosed == true && rightClosed == true)
                    {
                        Debug.Log("Blinked!");
                        ChangeColor();
                    }
                }


            }
        }

    }
    private void ChangeColor()
    {
        objRenderer.material.color = Random.ColorHSV();
    }
}
