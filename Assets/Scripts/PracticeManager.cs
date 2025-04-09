using UnityEngine;
using UnityEngine.InputSystem;
using Varjo.XR;
using UnityEngine.XR;
using static Varjo.XR.VarjoEyeTracking;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit;

public class PracticeManager : MonoBehaviour
{
    [Header("Settings")]
    public float trialDuration = 30f; // Set your practice duration
    public string breakSceneName = "BreakScene";
    public GameObject objectOfInterest;
    public XRRaycastTrigger raycastTrigger; // Add this reference

    [Header("Eye Tracking")]
    public Transform leftEyeTransform;
    public Transform rightEyeTransform;
    public float blinkCooldown = 1.5f;

    // Blink tracking
    private int blinkCount = 0;
    private float trialStartTime;
    private float lastBlinkTime = 0;
    private bool trialRunning = false;
    private Coroutine trialCoroutine;

    private List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();
    private UnityEngine.XR.InputDevice device;
    private Eyes eyes;

    void Start()
    {
        // Set up interaction listener
        if (raycastTrigger != null)
        {
            raycastTrigger.OnObjectInteracted += HandleObjectInteraction;
        }

        // Start practice trial
        StartTrial();
    }

    void OnEnable()
    {
        if (!device.isValid)
        {
            GetDevice();
        }
    }

    void Update()
    {
        if (trialRunning && device.isValid)
        {
            TrackEyesAndBlinks();
        }
    }

    void GetDevice()
    {
        UnityEngine.XR.InputDevices.GetDevicesAtXRNode(XRNode.CenterEye, devices);
        device = devices.FirstOrDefault();
    }

    void StartTrial()
    {
        trialStartTime = Time.time;
        blinkCount = 0;
        trialRunning = true;

        // Show practice object
        if (objectOfInterest != null)
        {
            objectOfInterest.SetActive(true);
        }

        trialCoroutine = StartCoroutine(RunTrial());
    }

    System.Collections.IEnumerator RunTrial()
    {
        yield return new WaitForSeconds(trialDuration);
        EndTrial();
    }

    void TrackEyesAndBlinks()
    {
        if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.eyesData, out eyes))
        {
            // Update eye positions (optional visualization)
            if (eyes.TryGetLeftEyePosition(out Vector3 leftEyePos))
                leftEyeTransform.localPosition = leftEyePos;

            if (eyes.TryGetRightEyePosition(out Vector3 rightEyePos))
                rightEyeTransform.localPosition = rightEyePos;

            // Blink detection
            if (eyes.TryGetLeftEyeOpenAmount(out float leftOpen) &&
                eyes.TryGetRightEyeOpenAmount(out float rightOpen))
            {
                if (leftOpen < 0.1f && rightOpen < 0.1f &&
                    Time.time - lastBlinkTime > blinkCooldown)
                {
                    blinkCount++;
                    lastBlinkTime = Time.time;
                    Debug.Log($"Blink detected! Total: {blinkCount}");
                }
            }
        }
    }

    // New method: Handle object interaction (like ExperimentManager)
    void HandleObjectInteraction(GameObject interactedObject)
    {
        if (interactedObject == objectOfInterest && trialRunning)
        {
            ObjectFound();
        }
    }

    // Updated to match ExperimentManager's behavior
    public void ObjectFound()
    {
        if (trialRunning)
        {
            Debug.Log("Practice object found!");

            // Disable object (like in ExperimentManager)
            if (objectOfInterest != null)
            {
                objectOfInterest.SetActive(false);
            }

            // End trial immediately
            EndTrial();
        }
    }

    void EndTrial()
    {
        if (!trialRunning) return;

        trialRunning = false;
        if (trialCoroutine != null)
            StopCoroutine(trialCoroutine);

        // Calculate and store blink rate
        float trialTime = Time.time - trialStartTime;
        float blinkRate = trialTime > 0 ? blinkCount / trialTime : 0;
        PlayerPrefs.SetFloat("AverageBlinkRate", blinkRate);
        PlayerPrefs.Save();

        Debug.Log($"Practice ended. Blink rate: {blinkRate} blinks/sec");

        // Go to break scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(breakSceneName);
    }

    void OnDisable()
    {
        // Clean up event listener
        if (raycastTrigger != null)
        {
            raycastTrigger.OnObjectInteracted -= HandleObjectInteraction;
        }
    }
}