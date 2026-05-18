using UnityEngine;

/// Attach to a GameObject that also has a ParticleSystem.
/// The particle system should be configured in the Inspector (see setup notes).
/// This script drives the emission rate based on the day/night cycle,
/// fading snow in at dusk and out at dawn.
[RequireComponent(typeof(ParticleSystem))]
public class SnowParticleController : MonoBehaviour
{
    [Header("References")]
    public DayNightCycle dayNightCycle;
    [Tooltip("The particle emitter follows this transform (usually the player).")]
    public Transform followTarget;

    [Header("Emission")]
    [Tooltip("Particles per second at peak (full night).")]
    public float maxEmissionRate = 150f;
    [Tooltip("How fast the emission rate ramps up or down (0 = instant, 1 = gentle).")]
    [Range(0.1f, 5f)]
    public float fadeSpeed = 0.8f;

    [Header("Follow")]
    [Tooltip("How high above the follow target the emitter sits.")]
    public float heightAboveTarget = 20f;

    private ParticleSystem ps;
    private float          currentRate;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();

        // Start silent — the update loop will ramp up if it's already night
        currentRate = 0f;
        ApplyEmissionRate(0f);

        // Ensure the system is playing so particles can be emitted when rate > 0
        if (!ps.isPlaying) ps.Play();
    }

    void Update()
    {
        FollowTarget();

        float targetRate = ComputeTargetRate();

        // Smooth the rate change so emission fades in/out rather than snapping
        float step = fadeSpeed * maxEmissionRate * Time.deltaTime;
        currentRate = Mathf.MoveTowards(currentRate, targetRate, step);

        ApplyEmissionRate(currentRate);
    }

    // ── Follow ────────────────────────────────────────────────────────────────
    void FollowTarget()
    {
        if (followTarget == null) return;
        // Move only the XZ — height is fixed above target
        Vector3 pos   = followTarget.position;
        pos.y        += heightAboveTarget;
        transform.position = pos;
    }

    // ── Rate calculation ──────────────────────────────────────────────────────
    // Maps the current time of day to a [0,1] night alpha using the same
    // fadeStartDay / fadeEndNight / fadeStartDawn thresholds as DayNightCycle.
    // Day = 0, dusk ramp-up, night = 1, dawn ramp-down.
    float ComputeTargetRate()
    {
        if (dayNightCycle == null) return 0f;

        float t     = Mathf.Repeat(dayNightCycle.timeOfDay, 1f);
        float alpha = 0f;

        if (t < dayNightCycle.fadeStartDay)
        {
            // Full day — no snow
            alpha = 0f;
        }
        else if (t < dayNightCycle.fadeEndNight)
        {
            // Dusk — fade in
            alpha = Mathf.InverseLerp(dayNightCycle.fadeStartDay, dayNightCycle.fadeEndNight, t);
        }
        else if (t < dayNightCycle.fadeStartDawn)
        {
            // Full night — peak snow
            alpha = 1f;
        }
        else
        {
            // Dawn — fade out
            alpha = 1f - Mathf.InverseLerp(dayNightCycle.fadeStartDawn, 1f, t);
        }

        return alpha * maxEmissionRate;
    }

    // ── Apply ─────────────────────────────────────────────────────────────────
    void ApplyEmissionRate(float rate)
    {
        // Accessing ps.emission returns a copy of the module struct.
        // Assigning back via the local var does apply to the live system.
        var emission          = ps.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(rate);
    }

    // ── Gizmo ─────────────────────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (followTarget == null) return;
        Gizmos.color = new Color(0.7f, 0.9f, 1f, 0.4f);
        Vector3 center = followTarget.position + Vector3.up * heightAboveTarget;
        Gizmos.DrawWireCube(center, new Vector3(40f, 0.2f, 40f));
    }
}
