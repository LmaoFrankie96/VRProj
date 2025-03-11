using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class XRRaycastTrigger : MonoBehaviour
{
    public XRRayInteractor rayInteractor; // Assign your XRRayInteractor in the inspector
    public InputActionProperty triggerAction; // Assign the trigger action

    public event System.Action<GameObject> OnObjectInteracted; // Define the event

    private void Start()
    {
        triggerAction.action.Enable();
    }

    private void Update()
    {
        bool triggerPressed = triggerAction.action.IsPressed();
        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit)) // First check if the ray hits an object
        {
            // Debug.Log("Ray Hit: " + hit.collider.name);

            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null && triggerPressed) // Then check if trigger is pressed
            {
                interactable.Interact();
                OnObjectInteracted?.Invoke(hit.collider.gameObject); // Trigger the event
            }
        }
    }
}

// Interface for interactable objects
public interface IInteractable
{
    void Interact();
}
