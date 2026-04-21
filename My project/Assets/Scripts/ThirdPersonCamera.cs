using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform player;
    public float sensitivity = 2f;
    public float distance = 4f;
    public float heightOffset = 1.5f;
    public float minY = -20f;
    public float maxY = 60f;

    private float rotX = 0f;
    private float rotY = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        rotX += Input.GetAxis("Mouse X") * sensitivity;
        rotY -= Input.GetAxis("Mouse Y") * sensitivity;
        rotY = Mathf.Clamp(rotY, minY, maxY);

        Quaternion rotation = Quaternion.Euler(rotY, rotX, 0);
        transform.position = player.position + Vector3.up * heightOffset + rotation * Vector3.back * distance;
        transform.LookAt(player.position + Vector3.up * heightOffset);

        player.rotation = Quaternion.Euler(0, rotX, 0);
    }
}