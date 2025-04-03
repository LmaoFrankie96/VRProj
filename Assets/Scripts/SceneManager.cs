using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;

public class SceneManager : MonoBehaviour
{

    [Header("UI Setup")]
    public XRRayInteractor rayInteractor; 
    public XRUIInputModule uiInputModule; 

    [Header("Buttons")]
    public Button prac1Button; 
    public Button prac2Button; 
    public Button exp1Button;   
    public Button exp2Button;


    void Start()
    {
        // Button click handlers
        prac1Button.onClick.AddListener(LoadPractice1);
        prac2Button.onClick.AddListener(LoadPractice2);
        exp1Button.onClick.AddListener(LoadExperiment1);
        exp2Button.onClick.AddListener(LoadExperiment2);
        

        // Ensure UI is interactable
        if (rayInteractor != null)
        {
            rayInteractor.enabled = true;
        }
    }
    /// Method to load Experiment 1 scene
    public void LoadExperiment1()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Experiment 1"); 
        
    }

    // Method to load Experiment 2 scene
    public void LoadExperiment2()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Experiment 2");  
    }

    public void LoadPractice1()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("PracticeTask_1");
    }
    public void LoadPractice2()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("PracticeTask_2");
    }
}
