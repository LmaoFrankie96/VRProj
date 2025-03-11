using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using Varjo.XR;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;
using UnityEngine.XR;

public class ExperimentManager : MonoBehaviour
{
    public GameObject objectOfInterest;
    public GameObject distractorObject;
    public XRRaycastTrigger raycastTrigger;
    public InputActionProperty confirmAction;

    public string customFilePath = "C:/Users/PE ERP Lab/Documents/Ramish/ExperimentData.csv"; // Set your custom path here
    public KeyCode nextTrialKey = KeyCode.Space; // Key to start the next trial
    public KeyCode endExperimentKey = KeyCode.Escape; // Key to end the experiment immediately

    [Header("Gaze Data")]
    public GazeDataSource gazeDataSource = GazeDataSource.InputSubsystem;

    [Header("Distractor Positions")]
    public Transform distractorOriginalPosition;
    public Transform distractorPosition1;
    public Transform distractorPosition2;

    private float experimentStartTime;
    private float objectDetectionTime = -1f;
    private float distractorDetectionTime = -1f;
    private int currentTrial = 1;
    private bool objectFound = false;
    private bool distractorFound = false;
    private bool experimentEnded = false;
    private bool waitingForKeyPress = false;
    private bool eyeTrackingStarted = false;
    private bool distractorInFrustum = false; // Track if the distractor is inside the frustum
    private List<string> logData = new List<string>();

    // Explicitly use the correct InputDevice class
    private List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
    private UnityEngine.XR.InputDevice device;
    private Eyes eyes;
    private bool leftClosed;
    private bool rightClosed;

    private float blinkCooldown = 1.5f; // 1.5 seconds cooldown between blinks
    private float lastBlinkTime = 0;

    void GetDevice()
    {
        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(XRNode.CenterEye, devices);
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
        if (raycastTrigger != null)
        {
            raycastTrigger.OnObjectInteracted += HandleObjectInteraction;
        }
        if (distractorOriginalPosition == null || distractorPosition1 == null || distractorPosition2 == null)
        {
            Debug.LogError("Please assign all distractor position transforms.");
        }
    }

    void Update()
    {
        if (!eyeTrackingStarted && VarjoEyeTracking.IsGazeAllowed() && VarjoEyeTracking.IsGazeCalibrated())
        {
            eyeTrackingStarted = true;
            StartExperiment();
        }

        if (Input.GetKeyDown(endExperimentKey))
        {
            Debug.Log("Escape key pressed. Ending experiment immediately.");
            EndExperiment();
        }

        if (waitingForKeyPress && Input.GetKeyDown(nextTrialKey))
        {
            waitingForKeyPress = false;
            ProceedToNextTrial();
        }

        EyeTracking();
    }

    void HandleObjectInteraction(GameObject interactedObject)
    {
        if (interactedObject == objectOfInterest)
        {
            ObjectFound();
        }
        else if (interactedObject == distractorObject)
        {
            Debug.Log("Distractor object interacted. Ending experiment.");
            distractorDetectionTime = Time.time - experimentStartTime;
            distractorFound = true;
            EndExperiment();
        }
    }

    void EyeTracking()
    {
        if (VarjoEyeTracking.IsGazeAllowed() && VarjoEyeTracking.IsGazeCalibrated())
        {
            if (!device.isValid)
            {
                GetDevice();
            }

            if (gazeDataSource == GazeDataSource.InputSubsystem)
            {
                if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.eyesData, out eyes))
                {
                    if (eyes.TryGetLeftEyeOpenAmount(out float leftEyeOpenness) &&
                        eyes.TryGetRightEyeOpenAmount(out float rightEyeOpenness))
                    {
                        leftClosed = leftEyeOpenness < 0.1f;
                        rightClosed = rightEyeOpenness < 0.1f;

                        if (leftClosed && rightClosed && IsHeadsetWorn() && distractorInFrustum)
                        {
                            if (Time.time - lastBlinkTime >= blinkCooldown)
                            {
                                lastBlinkTime = Time.time;
                                Debug.Log("Blinked!");
                                ChangeDistractorPosition();
                            }
                        }
                    }
                }
            }
        }
    }

    private bool IsHeadsetWorn()
    {
        return VarjoEyeTracking.IsGazeCalibrated() && device.isValid;
    }

    private void ChangeDistractorPosition()
    {
        int randomIndex = Random.Range(0, 2);
        Transform newPosition = randomIndex == 0 ? distractorPosition1 : distractorPosition2;
        distractorObject.transform.position = newPosition.position;
        distractorObject.transform.rotation = newPosition.rotation;
    }

    public void SetDistractorInFrustum(bool isInFrustum)
    {
        distractorInFrustum = isInFrustum;
    }

    void StartExperiment()
    {
        experimentStartTime = Time.time;
        confirmAction.action.Enable();
        StartTrial();
    }

    void StartTrial()
    {
        Debug.Log("Starting Trial " + currentTrial);
        StartCoroutine(TrialTimer());
    }

    IEnumerator TrialTimer()
    {
        yield return new WaitForSeconds(30); // Each trial lasts 30 seconds
        AskForDistractions();
    }

    void AskForDistractions()
    {
        Debug.Log("Did you see any distractions? Press " + nextTrialKey + " to continue to the next trial.");
        waitingForKeyPress = true;
    }

    void ProceedToNextTrial()
    {
        currentTrial++;
        if (currentTrial > 3)
        {
            EndExperiment();
        }
        else
        {
            StartTrial();
        }
    }

    public void ObjectFound()
    {
        if (!objectFound)
        {
            objectDetectionTime = Time.time - experimentStartTime;
            objectFound = true;
            Debug.Log("Object found at: " + objectDetectionTime + " seconds");
        }
    }

    void EndExperiment()
    {
        experimentEnded = true;
        SaveData();
        Debug.Log("Experiment Ended");
    }

    void SaveData()
    {
        logData.Add("Trial,ObjectDetectionTime,DistractorDetected,DistractorDetectionTime");
        logData.Add(currentTrial + "," + objectDetectionTime + "," + distractorFound + "," + distractorDetectionTime);

        try
        {
            File.WriteAllLines(customFilePath, logData);
            Debug.Log("Data saved to " + customFilePath);
        }
        catch (IOException e)
        {
            Debug.LogError("Error saving file: " + e.Message);
        }
    }
}
