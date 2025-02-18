using UnityEngine;

public class SceneManager : MonoBehaviour
{
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
}
