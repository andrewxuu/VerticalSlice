using UnityEngine;
using Unity.VisualScripting;

public class PlayerController : MonoBehaviour
{
    [Header("Physics")]
    public float gravity = -9.81f;

    [Header("Animation")]
    [Tooltip("How quickly walk/run blend. Only active while moving.")]
    public float animSmoothSpeed = 10f;

    private CharacterController controller;
    private Animator animator;
    private float velocityY;
    private Vector3 lastPosition;

    void Start()
    {
        controller   = GetComponent<CharacterController>();
        animator     = GetComponentInChildren<Animator>();
        lastPosition = transform.position;
    }

    void Update()
    {
        ApplyGravity();
        UpdateAnimation();
    }

    void ApplyGravity()
    {
        velocityY = controller.isGrounded ? -2f : velocityY + gravity * Time.deltaTime;
        controller.Move(new Vector3(0, velocityY * Time.deltaTime, 0));
    }

    void UpdateAnimation()
    {
        Vector3 delta    = transform.position - lastPosition;
        lastPosition     = transform.position;

        float horizontal = new Vector3(delta.x, 0f, delta.z).magnitude;
        bool  isMoving   = horizontal > 0.001f;

        float smoothSpeed;
        if (!isMoving)
        {
            smoothSpeed = 0f;
        }
        else
        {
            float targetSpeed;
            try   { targetSpeed = (float)Variables.Object(gameObject).Get("speed"); }
            catch { targetSpeed = horizontal / Time.deltaTime; }

            smoothSpeed = Mathf.Lerp(animator.GetFloat("speed"), targetSpeed, Time.deltaTime * animSmoothSpeed);
        }

        float dirZ = Vector3.Dot(delta.normalized, transform.forward) < -0.3f
            ? -smoothSpeed
            :  smoothSpeed;

        animator.SetFloat("speed",      smoothSpeed);
        animator.SetFloat("DirectionZ", dirZ);
    }
}