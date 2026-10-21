using System;
using UnityEngine;

namespace Kinemation.SightsAligner
{
    [Serializable]
    public struct DynamicBone
    {
        public Transform target;
        public Transform hintTarget;
        public GameObject obj;

        public void Retarget()
        {
            if (target == null)
            {
                return;
            }

            obj.transform.position = target.position;
            obj.transform.rotation = target.rotation;
        }
    }
    
    public class CoreAnimComponent : MonoBehaviour
    {
        [Header("Rig")]
        [Tooltip("Doesn't use Target and Hint")]
        [SerializeField] private DynamicBone masterDynamic;
        [SerializeField] private DynamicBone rightHand;
        [SerializeField] private DynamicBone leftHand;

        [Tooltip("Used for mesh space calculations")] 
        [SerializeField] private Transform rootBone;

        [Header("Blending")] 
        [Range(0f, 1f)] 
        public float aimLayerAlphaLoc;
        [Range(0f, 1f)] 
        public float aimLayerAlphaRot;

        [Header("Tools")]
        public GunAimData aimData;
        [SerializeField] private Transform aimTarget;
        [SerializeField] private Animator animator;
        public bool aiming;
        
        private float _smoothAim;
        private (Vector3, Quaternion) _smoothAimPoint;

        private void Retarget()
        {
            //Master is retargeted manually as it requires non-character bone
            masterDynamic.obj.transform.position = aimData.pivotPoint.position;
            masterDynamic.obj.transform.rotation = aimData.pivotPoint.rotation;
            
            rightHand.Retarget();
            leftHand.Retarget();
        }

        private void ApplyProceduralLayer()
        {
            //Apply Aiming
            var masterTransform = masterDynamic.obj.transform;
            _smoothAim = AnimToolkitLib.GlerpLayer(_smoothAim, aiming ? 1f : 0f, aimData.aimSpeed);

            Vector3 scopeAimLoc = Vector3.zero;
            Quaternion scopeAimRot = Quaternion.identity;

            if (aimData.aimPoint != null)
            {
                scopeAimRot = Quaternion.Inverse(aimData.pivotPoint.rotation) * aimData.aimPoint.rotation;
                scopeAimLoc = -aimData.pivotPoint.InverseTransformPoint(aimData.aimPoint.position);
            }

            if (!_smoothAimPoint.Item1.Equals(scopeAimLoc))
            {
                _smoothAimPoint.Item1 = AnimToolkitLib.Glerp(_smoothAimPoint.Item1, scopeAimLoc, aimData.aimSpeed);
            }
            
            if (!_smoothAimPoint.Item2.Equals(scopeAimRot))
            {
                _smoothAimPoint.Item2 = AnimToolkitLib.Glerp(_smoothAimPoint.Item2, scopeAimRot, aimData.aimSpeed);
            }

            Vector3 addAimLoc = aimData.target.aimLoc;
            Quaternion addAimRot = aimData.target.aimRot * _smoothAimPoint.Item2;
            
            // Base Animation layer
            Vector3 baseLoc = masterTransform.position;
            Quaternion baseRot = masterTransform.rotation;

            AnimToolkitLib.MoveInBoneSpace(masterTransform, masterTransform, addAimLoc);
            masterTransform.rotation *= addAimRot;
            AnimToolkitLib.MoveInBoneSpace(masterTransform, masterTransform, _smoothAimPoint.Item1);

            addAimLoc = masterTransform.position;
            addAimRot = masterTransform.rotation;
            
            ApplyAiming(_smoothAimPoint.Item1, _smoothAimPoint.Item2);

            // Blend between Absolute and Additive
            masterTransform.position = Vector3.Lerp(masterTransform.position, addAimLoc, aimLayerAlphaLoc);
            masterTransform.rotation = Quaternion.Slerp(masterTransform.rotation, addAimRot, aimLayerAlphaRot);
            
            // Blend Between Non-Aiming and Aiming
            masterTransform.position = Vector3.Lerp(baseLoc, masterTransform.position, _smoothAim);
            masterTransform.rotation = Quaternion.Slerp(baseRot, masterTransform.rotation, _smoothAim);
        }
        
        private void ApplyAiming(Vector3 loc, Quaternion rot)
        {
            Vector3 offset = -loc;

            //1. Set master IK to the target
            //2. Then rotate
            //3. Finally applied local offset
            masterDynamic.obj.transform.position = aimTarget.position;
            masterDynamic.obj.transform.rotation = rootBone.rotation * rot; 
            AnimToolkitLib.MoveInBoneSpace(masterDynamic.obj.transform,masterDynamic.obj.transform, -offset);
        }

        private void ApplyIK()
        {
            Transform lowerBone = rightHand.target.parent;

            AnimToolkitLib.SolveTwoBoneIK(lowerBone.parent, lowerBone, rightHand.target,
                rightHand.obj.transform, rightHand.hintTarget, 1f, 1f, 1f);
            
            lowerBone = leftHand.target.parent;

            AnimToolkitLib.SolveTwoBoneIK(lowerBone.parent, lowerBone, leftHand.target,
                leftHand.obj.transform, leftHand.hintTarget, 1f, 1f, 1f);
        }

        // If bone transform is the same - zero frame
        // Use cached data to prevent continuous translation/rotation
        
        private void LateUpdate()
        {
            Retarget();
            ApplyProceduralLayer();
            ApplyIK();
        }

        public void CalculateAimData()
        {
            var stateName = aimData.target.stateName.Length > 0
                ? aimData.target.stateName
                : aimData.target.staticPose.name;

            if (animator != null)
            {
                animator.Play(stateName);
                animator.Update(0f);
            }
            
            // Cache the local data, so we can apply it without issues
            aimData.target.aimLoc = aimData.pivotPoint.InverseTransformPoint(aimTarget.position);
            aimData.target.aimRot = Quaternion.Inverse(aimData.pivotPoint.rotation) * aimTarget.rotation;
        }
        
        public void SetupBones()
        {
            if (rootBone == null)
            {
                var root = transform.Find("rootBone");

                if (root != null)
                {
                    rootBone = root.transform;
                }
                else
                {
                    var bone = new GameObject("rootBone");
                    bone.transform.parent = transform;
                    rootBone = bone.transform;
                    rootBone.localPosition = Vector3.zero;
                }
            }
            
            var children = transform.GetComponentsInChildren<Transform>(true);

            bool foundRightHand = false;
            bool foundLeftHand = false;
            bool foundHead = false;

            foreach (var bone in children)
            {
                if (bone.name.ToLower().Contains("ik"))
                {
                    continue;
                }
                
                bool bMatches = bone.name.ToLower().Contains("lefthand") || bone.name.ToLower().Contains("hand_l")
                                                                         || bone.name.ToLower().Contains("hand l")
                                                                         || bone.name.ToLower().Contains("l hand")
                                                                         || bone.name.ToLower().Contains("l.hand")
                                                                         || bone.name.ToLower().Contains("hand.l");
                if (!foundLeftHand && bMatches)
                {
                    leftHand.target = bone;

                    if (leftHand.hintTarget == null)
                    {
                        leftHand.hintTarget = bone.parent;
                    }

                    foundLeftHand = true;
                    continue;
                }

                bMatches = bone.name.ToLower().Contains("righthand") || bone.name.ToLower().Contains("hand_r")
                                                                     || bone.name.ToLower().Contains("hand r")
                                                                     || bone.name.ToLower().Contains("r hand")
                                                                     || bone.name.ToLower().Contains("r.hand")
                                                                     || bone.name.ToLower().Contains("hand.r");
                if (!foundRightHand && bMatches)
                {
                    rightHand.target = bone;

                    if (rightHand.hintTarget == null)
                    {
                        rightHand.hintTarget = bone.parent;
                    }

                    foundRightHand = true;
                }
                
                if (!foundHead && bone.name.ToLower().Contains("head"))
                {
                    if (masterDynamic.obj == null)
                    {
                        var boneObject = bone.transform.Find("MasterIK");

                        if (boneObject != null)
                        {
                            masterDynamic.obj = boneObject.gameObject;
                        }
                        else
                        {
                            masterDynamic.obj = new GameObject("MasterIK");
                            masterDynamic.obj.transform.parent = bone;
                            masterDynamic.obj.transform.localPosition = Vector3.zero;
                        }
                    }
                    
                    if (rightHand.obj == null)
                    {
                        var boneObject = bone.transform.Find("RightHandIK");

                        if (boneObject != null)
                        {
                            rightHand.obj = boneObject.gameObject;
                        }
                        else
                        {
                            rightHand.obj = new GameObject("RightHandIK");
                        }

                        rightHand.obj.transform.parent = masterDynamic.obj.transform;
                        rightHand.obj.transform.localPosition = Vector3.zero;
                    }

                    if (leftHand.obj == null)
                    {
                        var boneObject = bone.transform.Find("LeftHandIK");

                        if (boneObject != null)
                        {
                            leftHand.obj = boneObject.gameObject;
                        }
                        else
                        {
                            leftHand.obj = new GameObject("LeftHandIK");
                        }
                        
                        leftHand.obj.transform.parent = masterDynamic.obj.transform;
                        leftHand.obj.transform.localPosition = Vector3.zero;
                    }

                    foundHead = true;
                }
            }

            bool bFound = foundRightHand && foundLeftHand && foundHead;
            Debug.Log(bFound ? "All bones are found!" : "Some bones are missing!");
        }

        public void Init(GunAimData data, Transform aimPoint)
        {
            aimData = data;
            aimData.aimPoint = aimPoint;
            
            _smoothAimPoint.Item2 = Quaternion.Inverse(aimData.pivotPoint.rotation) * aimData.aimPoint.rotation;
            _smoothAimPoint.Item1 = -aimData.pivotPoint.InverseTransformPoint(aimData.aimPoint.position);
        }
    }
}