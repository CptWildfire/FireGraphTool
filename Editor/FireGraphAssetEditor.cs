using FireGraph.Runtime;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace FireGraph.Editor
{
    [CustomEditor(typeof(FireGraphAsset))]
    public class FireGraphAssetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open"))
            {
                FireGraphEditorWindow.Open((FireGraphAsset)target);
            }

            if (GUILayout.Button("Check Data Integrity"))
            {
                FireGraphAsset asset = (FireGraphAsset)target;

                asset.RemoveBrokenConnections();
            }
        }

        [OnOpenAsset]
        public static bool OnOpen(int instanceId, int index)
        {
            Object asset = EditorUtility.InstanceIDToObject(instanceId);
            
            if (asset.GetType() != typeof(FireGraphAsset)) 
                return false;
            
            FireGraphEditorWindow.Open((FireGraphAsset)asset);
            return true;
        }
    }
}