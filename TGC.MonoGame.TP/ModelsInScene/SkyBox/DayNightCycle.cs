namespace TGC.MonoGame.TP.ModelsInScene;

using System;
using Microsoft.Xna.Framework;

public sealed class DayNightCycle
{
    public float TimeOfDay { get; set; } // 0.0 to 24.0 hours
    public float DaySpeed { get; set; } = 0.1f; // Hours per second

    public Vector3 SunDirection { get; private set; }
    public Vector3 SunColor { get; private set; }
    public float SunIntensity { get; private set; }
    public float NightFactor { get; private set; }

    public void Update(float deltaSeconds)
    {
        TimeOfDay = (TimeOfDay + DaySpeed * deltaSeconds) % 24.0f;

        // Angle around X/Z axes: 0h = midnight, 6h = sunrise, 12h = noon, 18h = sunset
        float solarAngle = (TimeOfDay / 24.0f) * MathF.Tau - MathF.PI * 0.5f;

        // Compute sun vector in sky
        SunDirection = Vector3.Normalize(new Vector3(
            MathF.Cos(solarAngle) * 0.8f,
            MathF.Sin(solarAngle),
            0.3f
        ));

        float elevation = SunDirection.Y;

        if (elevation > 0.2f) // Full Day
        {
            SunColor = new Vector3(1.0f, 0.98f, 0.90f);
            SunIntensity = 1.5f;
            NightFactor = 0.0f;
        }
        else if (elevation > -0.1f) // Twilight / Sunrise / Sunset
        {
            float t = (elevation - (-0.1f)) / 0.3f;
            SunColor = Vector3.Lerp(new Vector3(0.95f, 0.35f, 0.1f), new Vector3(1.0f, 0.98f, 0.90f), t);
            SunIntensity = MathHelper.Clamp(t, 0.0f, 1.5f);
            NightFactor = 1.0f - t;
        }
        else // Night
        {
            SunColor = new Vector3(0.1f, 0.15f, 0.3f);
            SunIntensity = 0.0f;
            NightFactor = 1.0f;
        }
    }
}
