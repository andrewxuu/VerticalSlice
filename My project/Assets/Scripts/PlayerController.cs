using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;

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

    // ── Snow ──────────────────────────────────────────────────────────────────
    [Header("Snow")]
    [Tooltip("If true, snow depth slows walkSpeed/runSpeed via SnowManager.MovementMultiplier.")]
    public bool useSnowSlowdown = true;

    // Base values captured at Start so we can rescale live without losing the original
    private float baseWalkSpeed;
    private float baseRunSpeed;

    // ── Item holding ──────────────────────────────────────────────────────────
    [Header("Item Holding")]
    [Tooltip("Drag the RightHand (or equivalent) bone from the Hierarchy here.")]
    public Transform handBone;

    // ── Health ────────────────────────────────────────────────────────────────
    [Header("Health")]
    public float maxHealth = 100f;
    private float currentHealth;

    // ── Death ─────────────────────────────────────────────────────────────────
    [Header("Death")]
    [Tooltip("How long to wait after the death animation starts before showing the You Died screen. " +
             "Match this to the length of your death animation clip.")]
    public float deathAnimationLength = 2f;

    [Tooltip("Name of the Trigger parameter on your Animator that plays the death clip.")]
    public string deathTriggerName = "Death";

    private bool isDead;

    public static PlayerController Instance { get; private set; }

    /// Fired the moment the player dies. WarmthSystem subscribes here.
    public static event System.Action OnPlayerDied;

    private CharacterController controller;
    private ScriptMachine       vsGraph;
    private Animator            animator;
    private float               velocityY;
    private Vector3             lastPosition;

    private GameObject currentHeldObject;
    private ItemData   currentHeldItem;

    // ── Lifecycle ──────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(this);
    }

    void Start()
    {
        controller    = GetComponent<CharacterController>();
        vsGraph       = GetComponent<ScriptMachine>();
        animator      = GetComponentInChildren<Animator>();
        lastPosition  = transform.position;
        currentHealth = maxHealth;
        baseWalkSpeed = walkSpeed;
        baseRunSpeed  = runSpeed;
        UIManager.Instance?.SetHealth(currentHealth, maxHealth);
    }

    void Update()
    {
        // No input or movement while dead
        if (isDead) return;

        UpdateSnowSpeeds();

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

    // ── Snow slowdown ─────────────────────────────────────────────────────────
    void UpdateSnowSpeeds()
    {
        float m = (useSnowSlowdown && SnowManager.Instance != null)
            ? SnowManager.Instance.MovementMultiplier
            : 1f;

        walkSpeed = baseWalkSpeed * m;
        runSpeed  = baseRunSpeed  * m;
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

    // ── Health ────────────────────────────────────────────────────────────────
    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        UIManager.Instance?.SetHealth(currentHealth, maxHealth);
        if (currentHealth <= 0f) OnDeath();
    }

    void OnDeath()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("[Player] Died.");

        // Stop movement and VS graph immediately
        if (vsGraph != null) vsGraph.enabled = false;
        animator.SetFloat("speed",      0f);
        animator.SetFloat("DirectionZ", 0f);
        animator.SetBool("isChopping",  false);

        // Play death animation
        if (!string.IsNullOrEmpty(deathTriggerName))
            animator.SetTrigger(deathTriggerName);

        // Drop any held item visually
        ClearHeldItem();

        // Notify other systems (WarmthSystem resets via this event)
        OnPlayerDied?.Invoke();

        // Wait for animation then show the You Died screen
        StartCoroutine(DeathSequence());
    }

    System.Collections.IEnumerator DeathSequence()
    {
        // Lock cursor so the UI is usable
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible   = true;

        yield return new WaitForSeconds(deathAnimationLength);

        UIManager.Instance?.ShowDeathScreen();
    }
}