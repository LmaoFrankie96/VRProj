using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using Varjo.XR;

public class BlinkRateCalculator : MonoBehaviour
{
    [Header("Gaze Data")]
    public GazeDataSource gazeDataSource = GazeDataSource.InputSubsystem;

    private List<InputDevice> devices = new List<InputDevice>();
    private InputDevice device;
    private Eyes eyes;
    private bool leftClosed;
    private bool rightClosed;
    private int blinkCount = 0;
    private string filePath;
    private float startTime;

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

    // Start is called before the first frame update
    void Start()
    {
        startTime = Time.time;
        filePath = Path.Combine(Application.persistentDataPath, "blink_data.txt");

        // Check if the file exists, if not, create it
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, "Elapsed Time (sec), Total Blinks, Blink Rate (BPM)\n");
        }
    }

    // Update is called once per frame
    void Update()
    {
        EyeTracking();
    }

    private void EyeTracking()
    {
        if (VarjoEyeTracking.IsGazeAllowed() && VarjoEyeTracking.IsGazeCalibrated())
        {
            if (!device.isValid)
            {
                GetDevice();
            }

            if (gazeDataSource == GazeDataSource.InputSubsystem)
            {
                if (device.TryGetFeatureValue(CommonUsages.eyesData, out eyes))
                {
                    if (eyes.TryGetLeftEyeOpenAmount(out float leftEyeOpenness) &&
                        eyes.TryGetRightEyeOpenAmount(out float rightEyeOpenness))
                    {
                        leftClosed = leftEyeOpenness < 0.1f;
                        rightClosed = rightEyeOpenness < 0.1f;

                        if (leftClosed && rightClosed && IsHeadsetWorn())
                        {
                            blinkCount++;
                            Debug.Log("Blinked!");
                            UpdateBlinkRate();
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

    private void UpdateBlinkRate()
    {
        float elapsedTime = Time.time - startTime;
        float averageBlinkRate = blinkCount / (elapsedTime / 60); // Blinks per minute

        string data = $"{elapsedTime:F2}, {blinkCount}, {averageBlinkRate:F2}\n";
        Debug.Log($"Elapsed Time: {elapsedTime:F2} sec, Total Blinks: {blinkCount}, Blink Rate: {averageBlinkRate:F2} BPM");

        // Append new data to the text file
        File.AppendAllText(filePath, data);
    }
}
