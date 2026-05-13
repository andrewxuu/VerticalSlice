using UnityEngine;
using Unity.VisualScripting;

/// Merged from PlayerController.cs and ItemHolder.cs.
/// Attach to the root player GameObject.
public class PlayerController : MonoBehaviour
{
    // ── Movement ──────────────────────────────────────────────────────────────
    [Header("Physics")]
    public float gravity = -9.81f;

    [Header("Movement Speeds")]
    [Tooltip("Must match the walk speed in your VS graph")]
    public float walkSpeed = 5f;
    [Tooltip("Must match the run speed in your VS graph")]
    public float runSpeed  = 9f;

    // ── Item holding ──────────────────────────────────────────────────────────
    [Header("Item Holding")]
    [Tooltip("Drag the RightHand (or equivalent) bone from the Hierarchy here.")]
    public Transform handBone;

    public static PlayerController Instance { get; private set; }

    private CharacterController controller;
    private ScriptMachine       vsGraph;
    private Animator            animator;
    private float               velocityY;
    private Vector3             lastPosition;

    private GameObject currentHeldObject;
    private ItemData   currentHeldItem;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    void Start()
    {
        controller   = GetComponent<CharacterController>();
        vsGraph      = GetComponent<ScriptMachine>();
        animator     = GetComponentInChildren<Animator>();
        lastPosition = transform.position;
    }

    void Update()
    {
        if (UIManager.IsUIOpen())
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

    // ── Movement ──────────────────────────────────────────────────────────────
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

    // ── Item holding ──────────────────────────────────────────────────────────
    public void UpdateHeldItem(ItemData item)
    {
        if (item == currentHeldItem) return;

        if (currentHeldObject != null)
        {
            Destroy(currentHeldObject);
            currentHeldObject = null;
        }

        currentHeldItem = item;

        if (item == null || item.heldPrefab == null) return;

        if (handBone == null)
        {
            Debug.LogWarning("[PlayerController] No hand bone assigned.");
            return;
        }

        currentHeldObject = Instantiate(item.heldPrefab, handBone);
        currentHeldObject.transform.localPosition    = item.holdOffset;
        currentHeldObject.transform.localEulerAngles = item.holdRotation;
        currentHeldObject.transform.localScale       = item.holdScale;
        currentHeldObject.name = $"_Held_{item.itemName}";

        foreach (Rigidbody rb  in currentHeldObject.GetComponentsInChildren<Rigidbody>()) Destroy(rb);
        foreach (Collider  col in currentHeldObject.GetComponentsInChildren<Collider>())  Destroy(col);
    }

    public void ClearHeldItem() => UpdateHeldItem(null);
}
