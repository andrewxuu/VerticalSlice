using UnityEngine;
using UnityEngine.UI;

public class DayNightUI : MonoBehaviour
{
    [Header("References")]
    public DayNightLighting dayNightLighting;
    public Image sunImage;
    public Image moonImage;

    [Header("Crossfade Timing")]
    [Range(0f, 0.5f)] public float fadeStartDay  = 0.4f;
    [Range(0.5f, 1f)] public float fadeEndNight   = 0.6f;
    [Range(0.5f, 1f)] public float fadeStartDawn  = 0.9f;

    [Header("Colors")]
    public Color dayColor   = Color.white;
    public Color duskColor  = new Color(1f, 0.45f, 0.1f); 
    public Color nightColor = Color.white;
    public Color dawnColor  = new Color(1f, 0.45f, 0.1f); 

    void Update()
    {
        if (dayNightLighting == null) return;

        float t = Mathf.Repeat(dayNightLighting.timeOfDay, 1f);
        float sunAlpha, moonAlpha;
        Color sunColor, moonColor;

        if (t < fadeStartDay)
        {
            sunAlpha  = 1f;  moonAlpha  = 0f;
            sunColor  = dayColor; moonColor = nightColor;
        }
        else if (t < fadeEndNight)
        {
            float f   = Mathf.InverseLerp(fadeStartDay, fadeEndNight, t);
            sunAlpha  = 1f - f; moonAlpha  = f;
            sunColor  = Color.Lerp(dayColor,  duskColor,  f);
            moonColor = Color.Lerp(duskColor, nightColor, f);
        }
        else if (t < fadeStartDawn)
        {
            sunAlpha  = 0f;  moonAlpha  = 1f;
            sunColor  = dayColor; moonColor = nightColor;
        }
        else
        {
            float f   = Mathf.InverseLerp(fadeStartDawn, 1f, t);
            sunAlpha  = f;   moonAlpha  = 1f - f;
            sunColor  = Color.Lerp(dawnColor, dayColor,   f);
            moonColor = Color.Lerp(nightColor, dawnColor, f);
        }

        SetColor(sunImage,  sunColor,  sunAlpha);
        SetColor(moonImage, moonColor, moonAlpha);
    }

    void SetColor(Image img, Color color, float alpha)
    {
        if (img == null) return;
        color.a = alpha;
        img.color = color;
    }
}