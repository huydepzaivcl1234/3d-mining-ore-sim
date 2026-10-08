using UnityEngine;

// Integrated cosine easing gives zero speed at both ends and is independent of frame rate.
public static class CombatLungeMotion
{
    public static float TravelBetween(float previousProgress, float progress, float distance)
    {
        float previous = Mathf.Clamp01(previousProgress);
        float current = Mathf.Clamp01(progress);
        if (current <= previous || distance <= 0f) return 0f;
        return distance * 0.5f * (Mathf.Cos(previous * Mathf.PI) - Mathf.Cos(current * Mathf.PI));
    }
}
