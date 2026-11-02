using UnityEngine;
using UnityEditor;

public static class FirstPersonAuthoring
{
    [MenuItem("Tools/First Person/Snap targets to selected body's current pose")]
    public static void SnapSelected(){
        var look=Selection.activeGameObject?Selection.activeGameObject.GetComponentInParent<FullBodyFirstPerson>():null;
        if(!look){Debug.LogWarning("Select the player with FullBodyFirstPerson first.");return;}
        Snap(look);
    }
    public static void Snap(FullBodyFirstPerson look){
        if(!look||!look.animator||!look.cam||!look.weapon)return;
        Undo.RecordObject(look.cam.transform,"Snap eye camera");
        var head=look.animator.GetBoneTransform(HumanBodyBones.Head);
        look.cam.transform.SetPositionAndRotation(head.position+look.bodyRoot.rotation*look.eyeOffset,look.bodyRoot.rotation);
        var anchor=look.weapon;Undo.RecordObject(anchor.transform,"Snap weapon anchor");
        anchor.transform.SetLocalPositionAndRotation(anchor.hipPosition,Quaternion.Euler(anchor.hipEuler));
        foreach(var arm in anchor.arms){
            if(!arm)continue;
            var hand=look.animator.GetBoneTransform(arm.rightArm?HumanBodyBones.RightHand:HumanBodyBones.LeftHand);
            var elbow=look.animator.GetBoneTransform(arm.rightArm?HumanBodyBones.RightLowerArm:HumanBodyBones.LeftLowerArm);
            if(arm.target){Undo.RecordObject(arm.target,"Snap hand IK target");arm.target.SetPositionAndRotation(hand.position,hand.rotation*Quaternion.Inverse(Quaternion.Euler(arm.handRotationOffset)));EditorUtility.SetDirty(arm.target);}
            if(arm.hint){Undo.RecordObject(arm.hint,"Snap elbow hint");arm.hint.position=elbow.position;EditorUtility.SetDirty(arm.hint);}
        }
        EditorUtility.SetDirty(anchor.transform);EditorUtility.SetDirty(look.cam.transform);
    }
}

[CustomEditor(typeof(FullBodyFirstPerson))]
public class FirstPersonAuthoringInspector : Editor
{
    public override void OnInspectorGUI(){DrawDefaultInspector();if(GUILayout.Button("Snap hand targets + elbow hints from current pose"))FirstPersonAuthoring.Snap((FullBodyFirstPerson)target);}
}
