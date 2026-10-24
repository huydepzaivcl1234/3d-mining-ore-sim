using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Four solid invisible walls around an editable rectangular play area.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class MiningWorldBorder : MonoBehaviour
    {
        [SerializeField] private Vector3 size = new Vector3(198f, 40f, 198f);
        [Min(0.1f), SerializeField] private float wallThickness = 2f;
        [Min(0f), SerializeField] private float depthBelowGround = 3f;
        [SerializeField] private Color gizmoColor = new Color(0f, 0.85f, 1f, 0.8f);
        [HideInInspector, SerializeField] private BoxCollider[] walls = new BoxCollider[4];
        private bool geometryDirty;
        public Vector3 Size => size;

        public void SetSize(Vector3 value)
        {
            size = new Vector3(Mathf.Max(1f, value.x), Mathf.Max(2f, value.y), Mathf.Max(1f, value.z));
            UpdateWalls();
        }

        // Explicit authoring entry point. The Editor inspector/menu creates these with Undo.
        public void RebuildWalls()
        {
            if (walls == null || walls.Length != 4) walls = new BoxCollider[4];
            for (int i = 0; i < walls.Length; i++)
                if (walls[i] == null) walls[i] = gameObject.AddComponent<BoxCollider>();
            UpdateWalls();
        }

        private void OnEnable()
        {
            if (Application.isPlaying) RebuildWalls();
            else UpdateWalls();
        }
        private void OnDisable()
        {
            if (walls == null) return;
            foreach (var wall in walls) if (wall != null) wall.enabled = false;
        }
        private void OnValidate()
        {
            size = new Vector3(Mathf.Max(1f, size.x), Mathf.Max(2f, size.y), Mathf.Max(1f, size.z));
            wallThickness = Mathf.Max(0.1f, wallThickness);
            depthBelowGround = Mathf.Max(0f, depthBelowGround);
            geometryDirty = true;
        }
        private void Update()
        {
            if (!geometryDirty) return;
            geometryDirty = false;
            UpdateWalls();
        }
        private void UpdateWalls()
        {
            if (walls == null || walls.Length != 4) return;
            float height = size.y + depthBelowGround;
            float y = (size.y - depthBelowGround) * 0.5f;
            for (int i = 0; i < 4; i++)
            {
                var wall = walls[i];
                if (wall == null) continue;
                bool alongX = i < 2;
                float sign = i % 2 == 0 ? -1f : 1f;
                wall.center = alongX
                    ? new Vector3(sign * (size.x + wallThickness) * 0.5f, y, 0f)
                    : new Vector3(0f, y, sign * (size.z + wallThickness) * 0.5f);
                wall.size = alongX
                    ? new Vector3(wallThickness, height, size.z + wallThickness * 2f)
                    : new Vector3(size.x + wallThickness * 2f, height, wallThickness);
                wall.isTrigger = false;
                wall.enabled = isActiveAndEnabled;
            }
        }
        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(Vector3.up * size.y * 0.5f, size);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
