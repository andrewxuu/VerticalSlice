using UnityEngine;
using Unity.VisualScripting;

public class PlayerController : MonoBehaviour
{
    [Header("Physics")]
    public float gravity = -9.81f;

    [Header("Movement Speeds")]
    [Tooltip("Must match the walk speed in your VS graph")]
    public float walkSpeed = 5f;
    [Tooltip("Must match the run speed in your VS graph")]
    public float runSpeed  = 9f;

    private CharacterController controller;
    private ScriptMachine       vsGraph;
    private Animator            animator;
    private float               velocityY;
    private Vector3             lastPosition;

    void Start()
    {
        controller   = GetComponent<CharacterController>();
        vsGraph      = GetComponent<ScriptMachine>();
        animator     = GetComponentInChildren<Animator>();
        lastPosition = transform.position;
    }

    void Update()
    {
        if (GameState.IsUIOpen())
        {
            if (vsGraph != null) vsGraph.enabled = false;
            animator.SetFloat("speed",      0f);
            animator.SetFloat("DirectionZ", 0f);
            lastPosition = transform.position;
            return;
        }

        if (vsGraph != null && !vsGraph.enabled)
            vsGraph.enabled = true;

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
        bool  isRunning  = Input.GetKey(KeyCode.LeftShift);

        float speed = 0f;
        if (isMoving)
            speed = isRunning ? runSpeed : walkSpeed;

        float dirZ = Vector3.Dot(delta.normalized, transform.forward) < -0.3f
            ? -speed
            :  speed;

        animator.SetFloat("speed",      speed);
        animator.SetFloat("DirectionZ", dirZ);
    }
}
