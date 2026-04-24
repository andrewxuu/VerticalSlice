using UnityEngine;

public class DayNightLighting : MonoBehaviour
{
    [Header("Lighting")]
    public Light sun; 

    [Header("Timing")]
    public float dayDuration = 120f; 

    [HideInInspector]
    public float timeOfDay = 0f; // 0 = dawn, 0.5 = dusk, 1 = end of day

    void Update()
    {
        timeOfDay += Time.deltaTime / dayDuration;

        if (sun != null)
            sun.transform.localEulerAngles = new Vector3(timeOfDay * 360f, 0f, 0f);
    }
}