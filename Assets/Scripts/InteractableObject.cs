using UnityEngine;

public class InteractableObject : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        Debug.Log("Object Interacted: " + gameObject.name);
        //GetComponent<MeshRenderer>().material.color = Random.ColorHSV();
    }
}
