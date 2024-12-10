using UnityEngine;

public class ChangeColor : MonoBehaviour
{
    // Static instance to provide global access
    public static ChangeColor Instance { get; private set; }

    private Renderer objRenderer;
    private int blinkCount;

    // Called when the script instance is being loaded
    private void Awake()
    {
        // Ensure there is only one instance of this class
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple instances of ChangeColor detected. Destroying duplicate.");
            Destroy(gameObject); // Destroy any duplicate instances
            return;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        objRenderer = GetComponent<Renderer>();
        blinkCount = 0;
    }

    public void ChangeObjectColor()
    {
        objRenderer.material.color = Random.ColorHSV();
    }
}
