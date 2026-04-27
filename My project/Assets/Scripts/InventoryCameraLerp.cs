using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public class InventoryCameraLerp : MonoBehaviour
{
    [Header("Target")]
    public Transform invCamPoint;   

    [Header("Speed")]
    [Range(0.01f, 0.3f)]
    public float lerpSpeed = 0.08f;

    private Vector3    _origPos;
    private Quaternion _origRot;

    void Start()
    {
        _origPos = transform.position;
        _origRot = transform.rotation;
    }

    void Update()
    {
        bool isOpen = false;
        try
        {
            isOpen = (bool)Variables.Scene(SceneManager.GetActiveScene()).Get("isOpen");
        }
        catch {}

        if (isOpen && invCamPoint != null)
        {
            transform.position = Vector3.Lerp(transform.position, invCamPoint.position, lerpSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, invCamPoint.rotation, lerpSpeed);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, _origPos, lerpSpeed);
            transform.rotation = Quaternion.Lerp(transform.rotation, _origRot, lerpSpeed);
        }
    }
}