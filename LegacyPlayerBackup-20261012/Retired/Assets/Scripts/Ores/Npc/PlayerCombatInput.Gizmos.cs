using MiningSimulator.Ores;
using UnityEngine;

public partial class PlayerCombatInput
{
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.TransformPoint(HitOriginOffset);
        if (freeFlowEnabled)
        {
            Gizmos.color = new Color(0f, .85f, 1f, 1f);
            float range = LungeAcquireRange;
            Vector3 last = origin + Quaternion.AngleAxis(-maximumStepAngle, Vector3.up) * StrikeForward * range;
            Gizmos.DrawLine(origin, last);
            for (int i = 1; i <= 48; i++)
            {
                Vector3 point = origin + Quaternion.AngleAxis(-maximumStepAngle + maximumStepAngle * 2f * i / 48f,
                    Vector3.up) * StrikeForward * range;
                Gizmos.DrawLine(last, point);
                last = point;
            }
            Gizmos.DrawLine(origin, last);
            if (Application.isPlaying && stepTarget != null)
                Gizmos.DrawLine(origin, stepTarget.transform.position + Vector3.up);
#if UNITY_EDITOR
            UnityEditor.Handles.Label(origin + StrikeForward * range,
                $"Lunge {range:0.00}m | Hit {AttackRange:0.00}m | Step max {strikeStepDistance:0.00}m");
#endif
        }
        Gizmos.color = Application.isPlaying && wasAttacking && !hitApplied ? Color.red : Color.yellow;
        // Display the same height band used by the fist hit query.
        Gizmos.DrawLine(origin - Vector3.up * HitHalfHeight, origin + Vector3.up * HitHalfHeight);
        Vector3 previous = origin + Quaternion.AngleAxis(-AttackAngle * 0.5f, Vector3.up) * StrikeForward * AttackRange;
        Gizmos.DrawLine(origin, previous);
        Vector3 up = Vector3.up * HitHalfHeight;
        Gizmos.DrawLine(origin - up, previous - up);
        Gizmos.DrawLine(origin + up, previous + up);
        for (int i = 1; i <= 32; i++)
        {
            Vector3 point = origin + Quaternion.AngleAxis(-AttackAngle * 0.5f + AttackAngle * i / 32f,
                Vector3.up) * StrikeForward * AttackRange;
            Gizmos.DrawLine(previous, point);
            Gizmos.DrawLine(previous + up, point + up);
            Gizmos.DrawLine(previous - up, point - up);
            if (i % 8 == 0) Gizmos.DrawLine(point - up, point + up);
            previous = point;
        }
        Gizmos.DrawLine(origin, previous);
        Gizmos.DrawLine(origin - up, previous - up);
        Gizmos.DrawLine(origin + up, previous + up);
    }
}
