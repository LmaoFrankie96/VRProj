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
        // Ensure the Ray Interactor is active for UI interaction
        if (rayInteractor != null)
        {
            rayInteractor.enableUIInteraction = true;
            rayInteractor.enabled = true;
        }

        // Make sure UI Input Module is present and properly linked
        if (uiInputModule == null)
        {
            Debug.LogWarning("XRUIInputModule is not assigned.");
        }

        // Safely assign button events
        if (prac1Button != null) prac1Button.onClick.AddListener(LoadPractice1);
        if (prac2Button != null) prac2Button.onClick.AddListener(LoadPractice2);
        if (exp1Button != null) exp1Button.onClick.AddListener(LoadExperiment1);
        if (exp2Button != null) exp2Button.onClick.AddListener(LoadExperiment2);
    }

    public void LoadExperiment1()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Experiment 1");
    }

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
