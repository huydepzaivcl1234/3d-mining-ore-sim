using UnityEngine;

/// <summary>Directional preference is independent of actor facing and physics query order.</summary>
public static class CombatTargetSelection
{
    public static bool TryScore(Vector3 intent, Vector3 toward, float maxAngle, float distance,
        float distanceWeight, out float score)
    {
        intent.y = toward.y = 0f;
        float angle = toward.sqrMagnitude > .0001f ? Vector3.Angle(intent, toward) : 0f;
        score = angle + Mathf.Max(0f,distance) * Mathf.Max(0f,distanceWeight);
        return angle <= Mathf.Clamp(maxAngle,0f,180f);
    }
}
