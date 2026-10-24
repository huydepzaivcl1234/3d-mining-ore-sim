using System;
using UnityEngine;

namespace GameData.Player.Script
{
    /// <summary>Camera-space arms only. No input, Animator, gameplay loop or mesh creation.</summary>
    [DisallowMultipleComponent]
    public sealed class FirstPersonArmsRig : MonoBehaviour
    {
        [Serializable] public struct Bone { public HumanBodyBones id; public Transform transform; }
        [SerializeField] private Transform viewRoot;
        [SerializeField] private SkinnedMeshRenderer armsRenderer;
        [SerializeField] private SkinnedMeshRenderer bodyRenderer;
        [SerializeField] private Mesh fullBodyMesh, bodyOnlyMesh;
        [SerializeField] private GameObject worldGun;
        [SerializeField] private Bone[] bones = Array.Empty<Bone>();
        [NonSerialized] private Transform[] lookup;
        public bool Ready => viewRoot && armsRenderer && bodyRenderer && fullBodyMesh;
        public Transform ViewRoot => viewRoot;
        public Transform GetBone(HumanBodyBones id)
        {
            int index=(int)id;
            if(index<0 || index>=(int)HumanBodyBones.LastBone)return null;
            if (lookup == null || !lookup[index])
            {
                lookup = new Transform[(int)HumanBodyBones.LastBone];
                foreach(var bone in bones) if((int)bone.id>=0 && (int)bone.id<lookup.Length) lookup[(int)bone.id]=bone.transform;
            }
            return lookup[index];
        }
        public void SetVisible(bool visible)
        {
            if(!Ready)return;
            if(armsRenderer.enabled!=visible)armsRenderer.enabled=visible;
            // Keep the world character whole; the local camera filters its layer.
            if(bodyRenderer.sharedMesh!=fullBodyMesh)bodyRenderer.sharedMesh=fullBodyMesh;
            if(worldGun && worldGun.activeSelf!=visible)worldGun.SetActive(visible);
        }
        private void OnDisable() => SetVisible(false);
    }
}
