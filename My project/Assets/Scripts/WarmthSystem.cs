using UnityEngine;

/// Manages player warmth. Attach to the Player GameObject alongside PlayerController.
/// - Day        → warmth regenerates to full; bar shows amber
/// - Near fire  → warmth regenerates fast (overrides night drain); bar shows amber
/// - Night      → warmth drains (reduced by shelter level); bar shows icy-blue
/// - Warmth = 0 → deals cold damage per second to the player
public class WarmthSystem : MonoBehaviour
{
    public static WarmthSystem Instance { get; private set; }

    [Header("Warmth")]
    public float maxWarmth       = 100f;
    public float warmthDrainRate = 5f;    // per second at night with no nearby fire
    public float warmthRegenDay  = 10f;   // per second during daytime
    public float warmthRegenFire = 20f;   // per second near an active campfire

    [Header("Shelter Insulation")]
    [Tooltip("Each shelter level reduces nightly drain by this fraction. " +
             "e.g. 0.25 = 25% less drain per level, so level 3 = 75% reduction.")]
    public float shelterDrainReduction = 0.25f;

    [Header("Cold Damage")]
    public float coldDamageRate = 5f;     // HP/s while warmth is zero

    [Header("Performance")]
    [Tooltip("How often (seconds) to scan for nearby campfires.")]
    public float fireCheckInterval = 0.5f;

    // ── Runtime state ──────────────────────────────────────────────────────────
    private float currentWarmth;
    private float fireCheckTimer;
    private bool  nearActiveFire;
    private int   nearestShelterLevel;

    private DayNightCycle dayNight;

    // ── Lifecycle ──────────────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(this); return; }
    }

    void Start()
    {
        currentWarmth = maxWarmth;
        dayNight      = FindObjectOfType<DayNightCycle>();

        // Subscribe to player death so warmth resets on respawn
        PlayerController.OnPlayerDied += RestoreWarmth;

        UIManager.Instance?.SetWarmth(currentWarmth, maxWarmth, false);
    }

    void OnDestroy()
    {
        PlayerController.OnPlayerDied -= RestoreWarmth;
    }

    void Update()
    {
        // Periodic campfire scan (cheaper than every frame)
        fireCheckTimer -= Time.deltaTime;
        if (fireCheckTimer <= 0f) { fireCheckTimer = fireCheckInterval; ScanForFires(); }

        TickWarmth();

        // Bar value reflects warmth; bar colour reflects time of day
        UIManager.Instance?.SetWarmth(currentWarmth, maxWarmth, IsNight());
    }

    // ── Fire proximity ─────────────────────────────────────────────────────────
    void ScanForFires()
    {
        nearActiveFire      = false;
        nearestShelterLevel = 0;

        Transform player = PlayerController.Instance?.transform;
        if (player == null) return;

        foreach (CampfireInteraction fire in FindObjectsOfType<CampfireInteraction>())
        {
            float dist = Vector3.Distance(player.position, fire.transform.position);
            if (dist > fire.GetWarmthRadius()) continue;

            // Shelter insulates even when the fire has gone out
            nearestShelterLevel = Mathf.Max(nearestShelterLevel, fire.GetShelterLevel());

            // Active warmth only while fuel is burning
            if (fire.HasFuel()) nearActiveFire = true;
        }
    }

    // ── Warmth tick ───────────────────────────────────────────────────────────
    void TickWarmth()
    {
        if (nearActiveFire)
        {
            // Fire overrides everything — warm up fast regardless of time of day
            currentWarmth = Mathf.Min(maxWarmth, currentWarmth + warmthRegenFire * Time.deltaTime);
        }
        else if (!IsNight())
        {
            // Daytime with no fire — restore warmth naturally
            currentWarmth = Mathf.Min(maxWarmth, currentWarmth + warmthRegenDay * Time.deltaTime);
        }
        else
        {
            // Night with no active fire — drain, reduced by any nearby shelter
            float drain = warmthDrainRate / (1f + nearestShelterLevel * shelterDrainReduction);
            currentWarmth = Mathf.Max(0f, currentWarmth - drain * Time.deltaTime);
        }

        if (currentWarmth <= 0f)
            PlayerController.Instance?.TakeDamage(coldDamageRate * Time.deltaTime);
    }

    // ── Day / night query ─────────────────────────────────────────────────────
    bool IsNight()
    {
        if (dayNight == null) return false;
        float t = Mathf.Repeat(dayNight.timeOfDay, 1f);
        return t >= dayNight.fadeEndNight && t < dayNight.fadeStartDawn;
    }

    // ── Public API ─────────────────────────────────────────────────────────────
    public void RestoreWarmth()
    {
        currentWarmth = maxWarmth;
        UIManager.Instance?.SetWarmth(currentWarmth, maxWarmth, IsNight());
    }

    public float GetWarmthFraction() => maxWarmth > 0f ? currentWarmth / maxWarmth : 0f;
}
