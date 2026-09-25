using System.Collections.Generic;
using UnityEngine;

namespace MiningSimulator.Ores
{
    /// <summary>Miner gear fitted to the game's original animated Humanoid mesh.</summary>
    [DisallowMultipleComponent]
    public sealed class LowPolyMinerSkin : MonoBehaviour
    {
        [SerializeField] private Material jacket;
        [SerializeField] private Material trousers;
        [SerializeField] private Material skin;
        [SerializeField] private Material helmet;
        [SerializeField] private Material boots;
        [SerializeField] private Material lamp;

        private struct Gear
        {
            public Transform bone;
            public Transform visual;
            public Vector3 offset;
            public Vector3 size;
        }

        private Animator animator;
        private Transform visualRoot;
        private readonly List<Gear> gear = new();

        private void OnEnable()
        {
            animator = GetComponentInChildren<Animator>(true);
            if (animator == null || !animator.isHuman ||
                animator.GetBoneTransform(HumanBodyBones.Head) == null)
            {
                Debug.LogWarning("Miner gear needs the original Humanoid rig and Avatar.", this);
                return;
            }

            if (visualRoot == null) BuildGear();
            visualRoot.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            if (visualRoot != null) visualRoot.gameObject.SetActive(false);
        }

        private void BuildGear()
        {
            // Keep the original skinned mesh visible: wrists, fingers, ankles, body
            // and pickaxe remain connected to the same animated skeleton.
            visualRoot = new GameObject("Miner Gear (Original Rig)").transform;
            visualRoot.SetParent(transform, false);

            Transform head = Bone(HumanBodyBones.Head);
            Transform leftFoot = Bone(HumanBodyBones.LeftFoot);
            Transform rightFoot = Bone(HumanBodyBones.RightFoot);
            float height = 1.7f;
            if (head != null && leftFoot != null && rightFoot != null)
                height = Mathf.Clamp(head.position.y - (leftFoot.position.y + rightFoot.position.y) * .5f,
                    .8f, 3f);
            float unit = height / 1.7f;

            Add("Work Vest", HumanBodyBones.Chest, jacket, new Vector3(0f, -.07f, -.025f),
                new Vector3(.35f, .33f, .20f), unit);
            Add("Utility Belt", HumanBodyBones.Hips, trousers, Vector3.zero,
                new Vector3(.36f, .075f, .24f), unit);
            Add("Left Sleeve", HumanBodyBones.LeftUpperArm, jacket, Vector3.zero,
                new Vector3(.15f, .21f, .15f), unit);
            Add("Right Sleeve", HumanBodyBones.RightUpperArm, jacket, Vector3.zero,
                new Vector3(.15f, .21f, .15f), unit);
            Add("Left Glove", HumanBodyBones.LeftHand, skin, Vector3.zero,
                new Vector3(.11f, .11f, .12f), unit);
            Add("Right Glove", HumanBodyBones.RightHand, skin, Vector3.zero,
                new Vector3(.11f, .11f, .12f), unit);
            Add("Left Boot", HumanBodyBones.LeftFoot, boots, new Vector3(0f, -.015f, .045f),
                new Vector3(.16f, .13f, .27f), unit);
            Add("Right Boot", HumanBodyBones.RightFoot, boots, new Vector3(0f, -.015f, .045f),
                new Vector3(.16f, .13f, .27f), unit);
            Add("Hard Hat", HumanBodyBones.Head, helmet, new Vector3(0f, .19f, 0f),
                new Vector3(.34f, .13f, .32f), unit);
            Add("Hat Brim", HumanBodyBones.Head, helmet, new Vector3(0f, .12f, .11f),
                new Vector3(.39f, .035f, .20f), unit);
            Add("Headlamp", HumanBodyBones.Head, lamp, new Vector3(0f, .20f, .18f),
                new Vector3(.09f, .08f, .06f), unit);
        }

        private Transform Bone(HumanBodyBones id) => animator.GetBoneTransform(id);

        private void Add(string name, HumanBodyBones id, Material material, Vector3 offset,
            Vector3 dimensions, float unit)
        {
            Transform bone = Bone(id);
            if (bone == null || material == null) return;
            GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(visualRoot, false);
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            gear.Add(new Gear { bone = bone, visual = part.transform,
                offset = offset * unit, size = dimensions * unit });
        }

        private void LateUpdate()
        {
            foreach (Gear item in gear)
            {
                Transform part = item.visual;
                part.SetPositionAndRotation(item.bone.TransformPoint(item.offset), item.bone.rotation);
                Vector3 scale = visualRoot.lossyScale;
                part.localScale = new Vector3(item.size.x / Mathf.Max(.001f, Mathf.Abs(scale.x)),
                    item.size.y / Mathf.Max(.001f, Mathf.Abs(scale.y)),
                    item.size.z / Mathf.Max(.001f, Mathf.Abs(scale.z)));
            }
        }
    }
}
