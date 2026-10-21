using UnityEditor;
using UnityEngine;

namespace Kinemation.SightsAligner.Editor
{
    [CustomEditor(typeof(CoreAnimComponent))]
    public class CoreAnimComponentEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            
            var look = (CoreAnimComponent) target;

            if (GUILayout.Button("Setup bones"))
            {
                look.SetupBones();
            }
            
            if (GUILayout.Button("Calculate aim data"))
            {
                look.CalculateAimData();
            }
        }
    }
}
