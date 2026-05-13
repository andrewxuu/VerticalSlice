using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Sun Light")]
    public Light sun;
    public float dayDuration = 120f;

    [HideInInspector] public float timeOfDay;
    [HideInInspector] public float timeMultiplier = 1f;

    [Header("Icon Sprites")]
    public Sprite sunSprite;
    public Sprite moonSprite;

    [Header("Crossfade Timing")]
    [Range(0f,   0.5f)] public float fadeStartDay  = 0.4f;
    [Range(0.5f, 1f)]   public float fadeEndNight   = 0.6f;
    [Range(0.5f, 1f)]   public float fadeStartDawn  = 0.9f;

    [Header("Colors")]
    public Color dayColor   = Color.white;
    public Color duskColor  = new Color(1f, 0.45f, 0.1f);
    public Color nightColor = Color.white;
    public Color dawnColor  = new Color(1f, 0.45f, 0.1f);

    void Update()
    {
        timeOfDay += Time.deltaTime / dayDuration * timeMultiplier;

        if (sun != null)
            sun.transform.localEulerAngles = new Vector3(timeOfDay * 360f, 0f, 0f);

        UpdateUI(Mathf.Repeat(timeOfDay, 1f));
    }

    void UpdateUI(float t)
    {
        if (UIManager.Instance == null) return;

        Color sunColor, moonColor;

        if (t < fadeStartDay)
        {
            sunColor  = dayColor;
            moonColor = WithAlpha(nightColor, 0f);
        }
        else if (t < fadeEndNight)
        {
            float f   = Mathf.InverseLerp(fadeStartDay, fadeEndNight, t);
            sunColor  = WithAlpha(Color.Lerp(dayColor,  duskColor,  f), 1f - f);
            moonColor = WithAlpha(Color.Lerp(duskColor, nightColor, f), f);
        }
        else if (t < fadeStartDawn)
        {
            sunColor  = WithAlpha(dayColor, 0f);
            moonColor = nightColor;
        }
        else
        {
            float f   = Mathf.InverseLerp(fadeStartDawn, 1f, t);
            sunColor  = WithAlpha(Color.Lerp(dawnColor,  dayColor,  f), f);
            moonColor = WithAlpha(Color.Lerp(nightColor, dawnColor, f), 1f - f);
        }

        UIManager.Instance.SetSunIcon(sunSprite,  sunColor);
        UIManager.Instance.SetMoonIcon(moonSprite, moonColor);
    }

    Color WithAlpha(Color c, float a)
    {
        c.a = a;
        return c;
    }
}
