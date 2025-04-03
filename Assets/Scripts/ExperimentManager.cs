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
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class ExperimentManager : MonoBehaviour
{
    [Header("Experiment Objects")]
    public GameObject objectOfInterest;
    public GameObject distractorObject;
    public XRRaycastTrigger raycastTrigger;
    public InputActionProperty confirmAction;

    [Header("File Settings")]
    public string customFilePath = "C:/Users/PE ERP Lab/Documents/Ramish/ExperimentData.csv";
    public KeyCode endExperimentKey = KeyCode.Escape;

    [Header("Trial Settings")]
    public float trial1Duration = 30f;
    public float trial2Duration = 45f;
    public float trial3Duration = 60f;

    [Header("UI Settings")]
    public GameObject trialEndUI;
    public TextMeshProUGUI trialEndText;
    public float uiDisplayTime = 10f;
    public Color successColor = Color.green;
    public Color failColor = Color.red;

    [Header("Scene Settings")]
    public string failureSceneName = "BreakScene";
    public string completionSceneName = "BreakScene";

    [Header("Gaze Data")]
    public GazeDataSource gazeDataSource = GazeDataSource.InputSubsystem;

    [Header("Visualization Transforms")]
    public Transform fixationPointTransform;
    public Transform leftEyeTransform;
    public Transform rightEyeTransform;

    [Header("Distractor Positions")]
    public Transform distractorOriginalPosition;
    public Transform distractorPosition1;
    public Transform distractorPosition2;

    // Private variables
    private float experimentStartTime;
    private float trialStartTime; // NEW: Tracks when each trial begins
    private float objectDetectionTime = -1f;
    private float distractorDetectionTime = -1f;
    private int currentTrial = 1;
    private bool objectFound = false;
    private bool distractorFound = false;
    private bool experimentEnded = false;
    private bool eyeTrackingStarted = false;
    private bool distractorInFrustum = false;
    private List<string> logData = new List<string>();
    private Coroutine currentTrialCoroutine;

    private List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
    private UnityEngine.XR.InputDevice device;
    private Eyes eyes;
    private bool leftClosed;
    private bool rightClosed;

    private float blinkCooldown = 1.5f;
    private float lastBlinkTime = 0;

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
        "Trial", "TrialObjectDetectionTime", "DistractorDetected", "TrialDistractorDetectionTime", // Updated names
        "Blinked"
    };

    private const string ValidString = "VALID";
    private const string InvalidString = "INVALID";

    void GetDevice()
    {
        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(XRNode.CenterEye, devices);
        device = devices.FirstOrDefault();

        if (!device.isValid)
        {
            Debug.LogError("Failed to get eye tracking device");
        }
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
        if (trialEndUI != null)
        {
            trialEndUI.SetActive(false);
        }

        if (raycastTrigger != null)
        {
            raycastTrigger.OnObjectInteracted += HandleObjectInteraction;
        }

        if (distractorOriginalPosition == null || distractorPosition1 == null || distractorPosition2 == null)
        {
            Debug.LogError("Please assign all distractor position transforms.");
        }

        VarjoEyeTracking.RequestGazeCalibration();
    }

    void Update()
    {
        if (!eyeTrackingStarted && VarjoEyeTracking.IsGazeAllowed() && VarjoEyeTracking.IsGazeCalibrated())
        {
            eyeTrackingStarted = true;
            StartExperiment();
        }

        if (eyeTrackingStarted)
        {
            EyeTracking();
        }
    }

    void HandleObjectInteraction(GameObject interactedObject)
    {
        if (interactedObject == objectOfInterest)
        {
            ObjectFound();
        }
        else if (interactedObject == distractorObject && currentTrial >= 3)
        {
            distractorDetectionTime = Time.time - trialStartTime; // Updated to use trial time
            distractorFound = true;
            Debug.Log($"Distractor found in Trial {currentTrial} at: {distractorDetectionTime}s");

            if (currentTrialCoroutine != null)
            {
                StopCoroutine(currentTrialCoroutine);
            }
            StartCoroutine(ShowTrialEndUIAndProceed());
        }
    }

    void EyeTracking()
    {
        if (IsGazeAllowed() && IsGazeCalibrated())
        {
            if (!device.isValid)
            {
                GetDevice();
                if (!device.isValid) return;
            }

            if (gazeDataSource == GazeDataSource.InputSubsystem)
            {
                if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.eyesData, out eyes))
                {
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

                    if (!logging)
                    {
                        StartLogging();
                    }

                    if (eyes.TryGetLeftEyeOpenAmount(out float leftEyeOpenness) &&
                        eyes.TryGetRightEyeOpenAmount(out float rightEyeOpenness))
                    {
                        LogEyeTrackingData(leftEyeOpenness, rightEyeOpenness);
                        leftClosed = leftEyeOpenness < 0.1f;
                        rightClosed = rightEyeOpenness < 0.1f;
                    }

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
        string[] logData = new string[28];

        logData[0] = Time.frameCount.ToString();
        logData[1] = (DateTime.Now.Ticks / 100).ToString();
        logData[2] = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond).ToString();
        logData[3] = Camera.main.transform.localPosition.ToString("F3");
        logData[4] = Camera.main.transform.localRotation.ToString("F3");
        logData[5] = ValidString;
        logData[6] = eyes.TryGetFixationPoint(out Vector3 fixationPoint) ? fixationPoint.ToString("F3") : "";
        logData[7] = fixationPoint.ToString("F3");

        Vector3 rightEyePosition = Vector3.zero;
        Vector3 leftEyePosition = Vector3.zero;

        if (eyes.TryGetLeftEyePosition(out leftEyePosition))
        {
            logData[11] = leftEyePosition.ToString("F3");
        }

        if (eyes.TryGetRightEyePosition(out rightEyePosition))
        {
            logData[17] = rightEyePosition.ToString("F3");
        }

        logData[8] = Vector3.Distance(leftEyePosition, rightEyePosition).ToString("F3");
        logData[9] = ValidString;
        logData[10] = eyes.TryGetLeftEyeRotation(out Quaternion leftEyeRotation) ? leftEyeRotation.ToString("F3") : "";
        logData[12] = leftEyeOpenness.ToString("F3");
        logData[13] = leftEyeOpenness.ToString("F3");
        logData[14] = leftEyeOpenness.ToString("F3");
        logData[15] = ValidString;
        logData[16] = eyes.TryGetRightEyeRotation(out Quaternion rightEyeRotation) ? rightEyeRotation.ToString("F3") : "";
        logData[18] = rightEyeOpenness.ToString("F3");
        logData[19] = rightEyeOpenness.ToString("F3");
        logData[20] = rightEyeOpenness.ToString("F3");
        logData[21] = fixationPoint.ToString("F3");
        logData[22] = "1.0";
        logData[23] = currentTrial.ToString();
        logData[24] = objectDetectionTime.ToString("F3");
        logData[25] = distractorFound.ToString();
        logData[26] = distractorDetectionTime.ToString("F3");
        logData[27] = (leftEyeOpenness < 0.1f && rightEyeOpenness < 0.1f) ? "1" : "0";

        Log(logData);
    }

    void Log(string[] values)
    {
        if (!logging || writer == null)
            return;

        string line = "";
        for (int i = 0; i < values.Length; ++i)
        {
            values[i] = values[i].Replace("\r", "").Replace("\n", "");
            line += values[i] + (i == (values.Length - 1) ? "" : ";");
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
        Directory.CreateDirectory(logPath);

        string fileName = Path.GetFileName(customFilePath);
        string path = Path.Combine(logPath, fileName);

        writer = new StreamWriter(path);
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
        trialStartTime = Time.time; // NEW: Record trial start time
        Debug.Log($"Starting Trial {currentTrial} at time: {trialStartTime}");

        objectFound = false;
        distractorFound = false;
        objectDetectionTime = -1f;
        distractorDetectionTime = -1f;

        if (objectOfInterest != null)
        {
            objectOfInterest.SetActive(true);
        }

        float duration = currentTrial switch
        {
            1 => trial1Duration,
            2 => trial2Duration,
            3 => trial3Duration,
            _ => 30f
        };

        currentTrialCoroutine = StartCoroutine(RunTrial(duration));
    }

    IEnumerator RunTrial(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (currentTrial == 1 && !objectFound)
        {
            StartCoroutine(ShowExperimentEndUI("Experiment Ended\n(No object detected in Trial 1)"));
            yield break;
        }

        StartCoroutine(ShowTrialEndUIAndProceed());
    }

    public void ObjectFound()
    {
        if (!objectFound)
        {
            objectDetectionTime = Time.time - trialStartTime; // Updated to use trial time
            objectFound = true;
            Debug.Log($"Object found in Trial {currentTrial} at: {objectDetectionTime}s");

            if (objectOfInterest != null)
            {
                objectOfInterest.SetActive(false);
            }

            if (currentTrialCoroutine != null)
            {
                StopCoroutine(currentTrialCoroutine);
            }
            StartCoroutine(ShowTrialEndUIAndProceed());
        }
    }

    IEnumerator ShowTrialEndUIAndProceed()
    {
        if (trialEndUI != null)
        {
            trialEndUI.SetActive(true);
            if (currentTrial == 1)
                trialEndText.text = $"Did you see anything unusual happening? Look for it!";
            else if (currentTrial == 2)
                trialEndText.text = $"Something is moving inside the environment. Find it and point towards it!";
            else if (currentTrial == 3)
                trialEndText.text = $"Something moves when you blink! Look for it and find it!";
        }

        yield return new WaitForSeconds(uiDisplayTime);

        if (trialEndUI != null)
        {
            trialEndUI.SetActive(false);
        }

        ProceedToNextTrial();
    }

    IEnumerator ShowExperimentEndUI(string message)
    {
        if (trialEndUI != null)
        {
            trialEndUI.SetActive(true);
            trialEndText.text = message;
            trialEndText.color = failColor;
        }

        yield return new WaitForSeconds(uiDisplayTime);

        UnityEngine.SceneManagement.SceneManager.LoadScene(failureSceneName);
    }

    void ProceedToNextTrial()
    {
        currentTrial++;

        if (currentTrial > 3)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(completionSceneName);
        }
        else
        {
            StartTrial();
        }
    }

    void SaveData()
    {
        if (!logging || writer == null)
        {
            Debug.LogError("SaveData() called but logging is not active or writer is null.");
            return;
        }

        string[] trialData = new string[]
        {
            currentTrial.ToString(),
            objectDetectionTime.ToString("F3"),
            distractorFound.ToString(),
            distractorDetectionTime.ToString("F3")
        };

        string trialDataString = string.Join(",", trialData);

        try
        {
            writer.WriteLine(trialDataString);
            writer.Flush();
            Debug.Log("Trial data saved: " + trialDataString);
        }
        catch (IOException e)
        {
            Debug.LogError("Error saving trial data: " + e.Message);
        }
    }
}