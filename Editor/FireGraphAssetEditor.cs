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
            FireGraphAsset asset = (FireGraphAsset)target;
            
            GUI.enabled = false;
            EditorGUILayout.ObjectField(asset.Executable, typeof(FireGraphObject), true);
            GUI.enabled = true;

            if (GUILayout.Button("Check Data Integrity"))
            {
                asset.RemoveBrokenConnections();
            }
        }

        
    }
}