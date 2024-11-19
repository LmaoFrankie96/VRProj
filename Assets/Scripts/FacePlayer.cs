using UnityEngine;

public class FacePlayer : MonoBehaviour
{
    public Transform playerCamera;
    Vector3 lookAtPosition;

    private void Update()
    {
        if (playerCamera != null) {

            lookAtPosition = new Vector3(playerCamera.position.x, transform.position.y, playerCamera.position.z);
            transform.LookAt(lookAtPosition);
            transform.forward = -transform.forward;
        }
    }
}
