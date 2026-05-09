using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public class CameraController : MonoBehaviour
{
    [Header("Third-Person")]
    public Transform player;
    public float sensitivity  = 2f;
    public float distance     = 4f;
    public float heightOffset = 1.5f;
    public float minY = -20f;
    public float maxY =  60f;

    [Header("Inventory View")]
    public Transform invCamPoint;
    [Range(0.01f, 0.3f)]
    public float lerpSpeed = 0.08f;

    private float rotX, rotY;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void LateUpdate()
    {
        if (IsInventoryOpen() && invCamPoint != null)
        {
            transform.position = Vector3.Lerp(transform.position, invCamPoint.position, lerpSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, invCamPoint.rotation, lerpSpeed);
            return;
        }

        rotX += Input.GetAxis("Mouse X") * sensitivity;
        rotY  = Mathf.Clamp(rotY - Input.GetAxis("Mouse Y") * sensitivity, minY, maxY);

        Quaternion rot    = Quaternion.Euler(rotY, rotX, 0);
        Vector3    target = player.position + Vector3.up * heightOffset + rot * Vector3.back * distance;

        transform.position = Vector3.Lerp(transform.position, target, lerpSpeed);
        transform.LookAt(player.position + Vector3.up * heightOffset);
        player.rotation = Quaternion.Euler(0, rotX, 0);
    }

    static bool IsInventoryOpen()
    {
        try   { return (bool)Variables.Scene(SceneManager.GetActiveScene()).Get("isOpen"); }
        catch { return false; }
    }
}