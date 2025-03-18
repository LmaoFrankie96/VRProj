using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using Varjo.XR;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;
using UnityEngine.XR;
using static Varjo.XR.VarjoEyeTracking;
using System;

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
    public GazeDataSource gazeDataSource = GazeDataSource.InputSubsystem; // Use Input Subsystem
    
    [Header("Visualization Transforms")]
    public Transform fixationPointTransform;
    public Transform leftEyeTransform;
    public Transform rightEyeTransform;

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

    // Gaze data logging
    private StreamWriter writer = null;
    private bool logging = false;
    private int gazeDataCount = 0;
    private float gazeTimer = 0f;

    private static readonly string[] ColumnNames = {
    "Frame", "CaptureTime", "LogTime", "HMDPosition", "HMDRotation",
    "GazeStatus", "CombinedGazeForward", "CombinedGazePosition", "InterPupillaryDistanceInMM",
    "LeftEyeStatus", "LeftEyeForward", "LeftEyePosition", "LeftPupilIrisDiameterRatio",
    "LeftPupilDiameterInMM", "LeftIrisDiameterInMM", "RightEyeStatus", "RightEyeForward",
    "RightEyePosition", "RightPupilIrisDiameterRatio", "RightPupilDiameterInMM",
    "RightIrisDiameterInMM", "FocusDistance", "FocusStability",
    "Trial", "ObjectDetectionTime", "DistractorDetected", "DistractorDetectionTime",
    "Blinked" // New column for blink status
};

    private const string ValidString = "VALID";
    private const string InvalidString = "INVALID";

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
            if (currentTrial >= 3) // Only check for distractor detection after Trial 3 starts
            {
                Debug.Log("Distractor object interacted in Trial " + currentTrial + ". Ending experiment.");
                distractorDetectionTime = Time.time - experimentStartTime;
                distractorFound = true;
                EndExperiment();
            }
            else
            {
                Debug.Log("Distractor ignored in Trial " + currentTrial);
            }
        }
    }


    void EyeTracking()
    {
        /*Debug.Log("Checking if eye tracking is allowed: " + IsGazeAllowed());
        Debug.Log("Checking if eye tracking is calibrated: " + IsGazeCalibrated());
        Debug.Log("Checking if device is valid: " + device.isValid);*/

        if (IsGazeAllowed() && IsGazeCalibrated())
        {
            if (!device.isValid)
            {
                GetDevice();
            }

            if (gazeDataSource == GazeDataSource.InputSubsystem)
            {
                /*bool gotEyesData = device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.eyesData, out eyes);
                Debug.Log("Got eyes data: " + gotEyesData);*/

                if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.eyesData, out eyes))
                {
                    // Retrieve eye positions, rotations, and fixation point
                    if (eyes.TryGetLeftEyePosition(out Vector3 leftEyePosition))
                    {
                        leftEyeTransform.localPosition = leftEyePosition;
                    }

                    if (eyes.TryGetLeftEyeRotation(out Quaternion leftEyeRotation))
                    {
                        leftEyeTransform.localRotation = leftEyeRotation;
                    }

                    if (eyes.TryGetRightEyePosition(out Vector3 rightEyePosition))
                    {
                        rightEyeTransform.localPosition = rightEyePosition;
                    }

                    if (eyes.TryGetRightEyeRotation(out Quaternion rightEyeRotation))
                    {
                        rightEyeTransform.localRotation = rightEyeRotation;
                    }

                    if (eyes.TryGetFixationPoint(out Vector3 fixationPoint))
                    {
                        fixationPointTransform.localPosition = fixationPoint;
                    }

                    // Start logging if not already started
                    if (!logging)
                    {
                        StartLogging();
                    }

                    // Log gaze data
                    if (eyes.TryGetLeftEyeOpenAmount(out float leftEyeOpenness) &&
                        eyes.TryGetRightEyeOpenAmount(out float rightEyeOpenness))
                    {
                        

                        LogEyeTrackingData(leftEyeOpenness, rightEyeOpenness);
                        leftClosed = leftEyeOpenness < 0.1f;
                        rightClosed = rightEyeOpenness < 0.1f;
                        /*bool gotLeftEyeOpenness = eyes.TryGetLeftEyeOpenAmount(out leftEyeOpenness);
                        bool gotRightEyeOpenness = eyes.TryGetRightEyeOpenAmount(out  rightEyeOpenness);
                        Debug.Log("Left Eye Openness: " + (gotLeftEyeOpenness ? leftEyeOpenness.ToString() : "Failed"));
                        Debug.Log("Right Eye Openness: " + (gotRightEyeOpenness ? rightEyeOpenness.ToString() : "Failed"));*/
                    }

                    
                    // Check for blink and change distractor position if necessary
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

    void LogEyeTrackingData(float leftEyeOpenness, float rightEyeOpenness)
    {
        string[] logData = new string[28]; // Updated size to include blink status

        // Gaze data frame number
        logData[0] = Time.frameCount.ToString();

        // Gaze data capture time (nanoseconds)
        logData[1] = (DateTime.Now.Ticks / 100).ToString(); // Simulated capture time

        // Log time (milliseconds)
        logData[2] = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString();

        // HMD
        logData[3] = Camera.main.transform.localPosition.ToString("F3");
        logData[4] = Camera.main.transform.localRotation.ToString("F3");

        // Combined gaze
        logData[5] = ValidString;
        logData[6] = eyes.TryGetFixationPoint(out Vector3 fixationPoint) ? fixationPoint.ToString("F3") : "";
        logData[7] = fixationPoint.ToString("F3");

        // Ensure rightEyePosition is initialized
        Vector3 rightEyePosition = Vector3.zero; // Default value
        Vector3 leftEyePosition = Vector3.zero;  // Default value

        if (eyes.TryGetLeftEyePosition(out leftEyePosition))
        {
            logData[11] = leftEyePosition.ToString("F3");
        }

        if (eyes.TryGetRightEyePosition(out rightEyePosition))
        {
            logData[17] = rightEyePosition.ToString("F3");
        }

        // IPD
        logData[8] = Vector3.Distance(leftEyePosition, rightEyePosition).ToString("F3");

        // Left eye
        logData[9] = ValidString;
        logData[10] = eyes.TryGetLeftEyeRotation(out Quaternion leftEyeRotation) ? leftEyeRotation.ToString("F3") : "";
        logData[12] = leftEyeOpenness.ToString("F3");
        logData[13] = leftEyeOpenness.ToString("F3");
        logData[14] = leftEyeOpenness.ToString("F3");

        // Right eye
        logData[15] = ValidString;
        logData[16] = eyes.TryGetRightEyeRotation(out Quaternion rightEyeRotation) ? rightEyeRotation.ToString("F3") : "";
        logData[18] = rightEyeOpenness.ToString("F3");
        logData[19] = rightEyeOpenness.ToString("F3");
        logData[20] = rightEyeOpenness.ToString("F3");

        // Focus
        logData[21] = fixationPoint.ToString("F3");
        logData[22] = "1.0";

        // Append trial data
        logData[23] = currentTrial.ToString();
        logData[24] = objectDetectionTime.ToString("F3");
        logData[25] = distractorFound.ToString();
        logData[26] = distractorDetectionTime.ToString("F3");

        // Blink Detection
        int blinked = (leftEyeOpenness < 0.1f && rightEyeOpenness < 0.1f) ? 1 : 0;
        logData[27] = blinked.ToString();

        Log(logData);
    }


    void Log(string[] values)
    {
        if (!logging || writer == null)
            return;

        string line = "";
        for (int i = 0; i < values.Length; ++i)
        {
            values[i] = values[i].Replace("\r", "").Replace("\n", ""); // Remove new lines so they don't break csv
            line += values[i] + (i == (values.Length - 1) ? "" : ";"); // Do not add semicolon to last data string
        }
        writer.WriteLine(line);
    }

    void StartLogging()
    {
        if (logging)
        {
            Debug.LogWarning("Logging was already started.");
            return;
        }

        logging = true;
        Debug.Log("StartLogging() was called.");

        string logPath = Path.GetDirectoryName(customFilePath);
        Directory.CreateDirectory(logPath); // Ensure directory exists

        string fileName = Path.GetFileName(customFilePath);
        string path = Path.Combine(logPath, fileName);

        writer = new StreamWriter(path);

        // Write updated headers
        Log(ColumnNames);

        Debug.Log("Log file created at: " + path);
    }

    void StopLogging()
    {
        if (!logging)
            return;

        if (writer != null)
        {
            writer.Flush();
            writer.Close();
            writer = null;
        }
        logging = false;
        Debug.Log("Logging ended");
    }

    void OnApplicationQuit()
    {
        StopLogging();
    }

    private bool IsHeadsetWorn()
    {
        return VarjoEyeTracking.IsGazeCalibrated() && device.isValid;
    }

    private void ChangeDistractorPosition()
    {
        int randomIndex = UnityEngine.Random.Range(0, 2);
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
        StopLogging();
        SaveData();
        Debug.Log("Experiment Ended");
    }

    void SaveData()
    {
        if (!logging || writer == null)
        {
            Debug.LogError("SaveData() called but logging is not active or writer is null.");
            return;
        }

        // Create an array of trial data
        string[] trialData = new string[]
        {
        currentTrial.ToString(),
        objectDetectionTime.ToString("F3"),
        distractorFound.ToString(),
        distractorDetectionTime.ToString("F3")
        };

        // Convert trial data to CSV format
        string trialDataString = string.Join(",", trialData);

        try
        {
            writer.WriteLine(trialDataString);  // Append to the existing log
            writer.Flush();  // Ensure data is saved
            Debug.Log("Trial data saved: " + trialDataString);
        }
        catch (IOException e)
        {
            Debug.LogError("Error saving trial data: " + e.Message);
        }
    }

}