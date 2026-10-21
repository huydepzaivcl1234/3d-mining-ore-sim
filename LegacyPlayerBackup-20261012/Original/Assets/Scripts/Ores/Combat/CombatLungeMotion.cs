using UnityEngine;

// Integrated cosine easing gives zero speed at both ends and is independent of frame rate.
public static class CombatLungeMotion
{
    public static Vector3 TrackStep(Vector3 toTarget, float stopDistance, float requestedTravel,
        float remainingTravel, float maximumSpeed, float deltaTime)
    {
        toTarget.y = 0f;
        float gap = toTarget.magnitude;
        if (gap <= Mathf.Max(0f, stopDistance) || deltaTime <= 0f) return Vector3.zero;
        float travel = Mathf.Min(Mathf.Min(Mathf.Max(0f, requestedTravel), Mathf.Max(0f, remainingTravel)),
            Mathf.Min(Mathf.Max(0f, maximumSpeed) * deltaTime, gap - Mathf.Max(0f, stopDistance)));
        return toTarget / gap * travel;
    }

    public static float TravelBetween(float previousProgress, float progress, float distance)
    {
        float previous = Mathf.Clamp01(previousProgress);
        float current = Mathf.Clamp01(progress);
        if (current <= previous || distance <= 0f) return 0f;
        return distance * 0.5f * (Mathf.Cos(previous * Mathf.PI) - Mathf.Cos(current * Mathf.PI));
    }
}
