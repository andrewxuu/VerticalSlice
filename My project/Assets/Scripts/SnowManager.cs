using UnityEngine;

public class SnowManager : MonoBehaviour
{
    public static SnowManager Instance { get; private set; }

    [Header("References")]
    public DayNightCycle dayNightCycle;

    [Header("Accumulation")]
    [Tooltip("How fast snow builds up during night (depth units per second).")]
    public float accumulationRate = 0.05f;
    [Tooltip("How fast snow melts during day (depth units per second).")]
    public float meltRate = 0.02f;
    [Tooltip("Maximum snow depth (1 = full accumulation).")]
    public float maxDepth = 1f;
    [Tooltip("Snow never melts below this value — partial melt only.")]
    [Range(0f, 1f)] public float minDepthAfterMelt = 0.15f;
    [Tooltip("Snow depth at scene start.")]
    [Range(0f, 1f)] public float startingDepth = 0f;

    [Header("Player Slowdown")]
    [Tooltip("Movement multiplier when snow is at maxDepth. 1 = no slowdown, 0.3 = 70% slower.")]
    [Range(0.1f, 1f)] public float minMovementMultiplier = 0.4f;

    [Header("Debug")]
    public bool logPhaseChanges = false;

    public float CurrentDepth { get; private set; }
    public float MovementMultiplier { get; private set; } = 1f;
    public TimePhase CurrentPhase { get; private set; }

    public enum TimePhase { Day, Transition, Night }

    static readonly int SnowDepthID = Shader.PropertyToID("_SnowDepth");

    private TimePhase lastLoggedPhase;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CurrentDepth = startingDepth;
        Shader.SetGlobalFloat(SnowDepthID, CurrentDepth);
    }

    void Update()
    {
        CurrentPhase = GetCurrentPhase();

        switch (CurrentPhase)
        {
            case TimePhase.Night:
                CurrentDepth = Mathf.Min(CurrentDepth + accumulationRate * Time.deltaTime, maxDepth);
                break;
            case TimePhase.Day:
                CurrentDepth = Mathf.Max(CurrentDepth - meltRate * Time.deltaTime, minDepthAfterMelt);
                break;
            case TimePhase.Transition:
                // Hold steady during dusk/dawn
                break;
        }

        Shader.SetGlobalFloat(SnowDepthID, CurrentDepth);

        float t = Mathf.Clamp01(CurrentDepth / Mathf.Max(maxDepth, 0.0001f));
        MovementMultiplier = Mathf.Lerp(1f, minMovementMultiplier, t);

        if (logPhaseChanges && CurrentPhase != lastLoggedPhase)
        {
            Debug.Log($"[Snow] Phase: {CurrentPhase}  Depth: {CurrentDepth:F2}");
            lastLoggedPhase = CurrentPhase;
        }
    }

    TimePhase GetCurrentPhase()
    {
        if (dayNightCycle == null) return TimePhase.Day;

        float t = Mathf.Repeat(dayNightCycle.timeOfDay, 1f);

        // Full night: between fadeEndNight (~0.6) and fadeStartDawn (~0.9)
        if (t >= dayNightCycle.fadeEndNight && t < dayNightCycle.fadeStartDawn)
            return TimePhase.Night;

        // Full day: before fadeStartDay (~0.4)
        if (t < dayNightCycle.fadeStartDay)
            return TimePhase.Day;

        // Everything else = dusk or dawn transition
        return TimePhase.Transition;
    }
}
