#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using MiningSimulator.Ores;

namespace MiningSimulator.Editor
{
    /// <summary>Draw the actual four perimeter strips, never a zone at the spawner Transform.</summary>
    public static class MonsterSpawnGizmos
    {
        [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
        private static void Draw(MonsterSpawnZone spawner, GizmoType type)
        {
            var settings = new SerializedObject(spawner);
            var surface = settings.FindProperty("miningSurface").objectReferenceValue as NavMeshSurface;
            if (surface == null)
            {
                var builder = Object.FindFirstObjectByType<WorldNavigationBootstrap>();
                if (builder != null) surface = builder.GetComponent<NavMeshSurface>();
            }
            if (surface == null || surface.collectObjects != CollectObjects.Volume) return;
            float min = Mathf.Max(0.5f, settings.FindProperty("minimumSpawnDistance").floatValue);
            float max = Mathf.Max(min, settings.FindProperty("maximumSpawnDistance").floatValue);
            Vector3 half = surface.size * 0.5f;
            var filter = new NavMeshQueryFilter { agentTypeID = surface.agentTypeID, areaMask = NavMesh.AllAreas };
            Color previous = Handles.color;
            for (int side = 0; side < 4; side++)
            {
                Vector3 a, b, outward;
                if (side < 2)
                {
                    float z = side == 0 ? half.z : -half.z;
                    a = new Vector3(-half.x, 0f, z); b = new Vector3(half.x, 0f, z);
                    outward = side == 0 ? Vector3.forward : Vector3.back;
                }
                else
                {
                    float x = side == 2 ? half.x : -half.x;
                    a = new Vector3(x, 0f, -half.z); b = new Vector3(x, 0f, half.z);
                    outward = side == 2 ? Vector3.right : Vector3.left;
                }
                a = surface.transform.TransformPoint(surface.center + a);
                b = surface.transform.TransformPoint(surface.center + b);
                a.y = b.y = surface.transform.position.y + 0.12f;
                outward = Vector3.ProjectOnPlane(surface.transform.TransformDirection(outward), Vector3.up).normalized;
                var points = new[] { a + outward * min, b + outward * min, b + outward * max, a + outward * max };
                Handles.color = new Color(1f, 0.58f, 0.12f, 0.12f);
                Handles.DrawAAConvexPolygon(points);
                Handles.color = new Color(1f, 0.62f, 0.12f, 0.95f);
                Handles.DrawAAPolyLine(2f, points[0], points[1], points[2], points[3], points[0]);
                // Green dots are valid against the active bake; gray dots are rejected.
                // Without an active bake, outlines remain a volume-based preview only.
                if (surface.navMeshData != null && NavMesh.SamplePosition(a, out _, 2f, filter))
                    for (int i = 0; i < 16; i++)
                    {
                        Vector3 sample = Vector3.Lerp(a, b, (i + 0.5f) / 16f) + outward * ((min + max) * 0.5f);
                        bool valid = NavMesh.SamplePosition(sample, out NavMeshHit nearest, max + 2f, filter);
                        float distance = valid ? Vector3.ProjectOnPlane(sample - nearest.position, Vector3.up).magnitude : 0f;
                        valid &= distance >= min && distance <= max;
                        Handles.color = valid ? Color.green : Color.gray;
                        Handles.DotHandleCap(0, sample, Quaternion.identity, 0.15f, EventType.Repaint);
                    }
                Handles.color = Color.white;
                Handles.Label((points[0] + points[1] + points[2] + points[3]) * 0.25f,
                    $"SPAWN {min:0.#}-{max:0.#} m outside mining bake");
            }
            Handles.color = previous;
        }
    }
}
#endif
