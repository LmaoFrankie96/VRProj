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

    private static readonly string[] ColumnNames = { "Frame", "CaptureTime", "LogTime", "HMDPosition", "HMDRotation", "GazeStatus", "CombinedGazeForward", "CombinedGazePosition", "InterPupillaryDistanceInMM", "LeftEyeStatus", "LeftEyeForward", "LeftEyePosition", "LeftPupilIrisDiameterRatio", "LeftPupilDiameterInMM", "LeftIrisDiameterInMM", "RightEyeStatus", "RightEyeForward", "RightEyePosition", "RightPupilIrisDiameterRatio", "RightPupilDiameterInMM", "RightIrisDiameterInMM", "FocusDistance", "FocusStability" };
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
            Debug.Log("Distractor object interacted. Ending experiment.");
            distractorDetectionTime = Time.time - experimentStartTime;
            distractorFound = true;
            EndExperiment();
        }
    }

    void EyeTracking()
    {
        if (IsGazeAllowed() && IsGazeCalibrated())
        {
            if (!device.isValid)
            {
                GetDevice();
            }

            if (gazeDataSource == GazeDataSource.InputSubsystem)
            {
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
        string[] logData = new string[23];

        // Gaze data frame number
        logData[0] = Time.frameCount.ToString();

        // Gaze data capture time (nanoseconds)
        logData[1] = (DateTime.Now.Ticks / 100).ToString(); // Simulate capture time

        // Log time (milliseconds)
        logData[2] = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString();

        // HMD
        logData[3] = Camera.main.transform.localPosition.ToString("F3");
        logData[4] = Camera.main.transform.localRotation.ToString("F3");

        // Combined gaze
        logData[5] = ValidString; // Assume valid for simplicity
        logData[6] = eyes.TryGetFixationPoint(out Vector3 fixationPoint) ? fixationPoint.ToString("F3") : "";
        logData[7] = eyes.TryGetFixationPoint(out fixationPoint) ? fixationPoint.ToString("F3") : "";

        // IPD
        logData[8] = eyes.TryGetLeftEyePosition(out Vector3 leftEyePosition) && eyes.TryGetRightEyePosition(out Vector3 rightEyePosition)
            ? Vector3.Distance(leftEyePosition, rightEyePosition).ToString("F3")
            : "";

        // Left eye
        logData[9] = ValidString; // Assume valid for simplicity
        logData[10] = eyes.TryGetLeftEyeRotation(out Quaternion leftEyeRotation) ? leftEyeRotation.ToString("F3") : "";
        logData[11] = eyes.TryGetLeftEyePosition(out leftEyePosition) ? leftEyePosition.ToString("F3") : "";
        logData[12] = leftEyeOpenness.ToString("F3"); // Left pupil-iris ratio (simulated)
        logData[13] = leftEyeOpenness.ToString("F3"); // Left pupil diameter (simulated)
        logData[14] = leftEyeOpenness.ToString("F3"); // Left iris diameter (simulated)

        // Right eye
        logData[15] = ValidString; // Assume valid for simplicity
        logData[16] = eyes.TryGetRightEyeRotation(out Quaternion rightEyeRotation) ? rightEyeRotation.ToString("F3") : "";
        logData[17] = eyes.TryGetRightEyePosition(out rightEyePosition) ? rightEyePosition.ToString("F3") : "";
        logData[18] = rightEyeOpenness.ToString("F3"); // Right pupil-iris ratio (simulated)
        logData[19] = rightEyeOpenness.ToString("F3"); // Right pupil diameter (simulated)
        logData[20] = rightEyeOpenness.ToString("F3"); // Right iris diameter (simulated)

        // Focus
        logData[21] = eyes.TryGetFixationPoint(out fixationPoint) ? fixationPoint.ToString("F3") : "";
        logData[22] = "1.0"; // Focus stability (simulated)

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
            Debug.LogWarning("Logging was on when StartLogging was called. No new log was started.");
            return;
        }

        logging = true;

        string logPath = Path.GetDirectoryName(customFilePath);
        Directory.CreateDirectory(logPath);

        string fileName = Path.GetFileName(customFilePath);
        string path = Path.Combine(logPath, fileName);

        writer = new StreamWriter(path);

        Log(ColumnNames);
        Debug.Log("Log file started at: " + path);
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
        // Adding the column headers for eye-tracking data
        logData.Add("Trial,ObjectDetectionTime,DistractorDetected,DistractorDetectionTime,GazeOriginX,GazeOriginY,GazeOriginZ,GazeDirectionX,GazeDirectionY,GazeDirectionZ,LeftEyeOpenness,RightEyeOpenness,LeftClosed,RightClosed");

        // Saving the data for the trial, including the gaze data
        logData.Add($"{currentTrial},{objectDetectionTime},{distractorFound},{distractorDetectionTime}");

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