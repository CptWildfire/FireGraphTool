using System;
using FireGraph.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FireGraph.Editor
{
    [CustomEditor(typeof(FireGraphObject))]
    public class FireGraphObjectEditor : UnityEditor.Editor
    {
        FireGraphObject graphObject;
        
        private void OnEnable()
        {
            graphObject = (FireGraphObject)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (GUILayout.Button("Open"))
            {
                graphObject.GraphAsset.Executable = graphObject;
                FireGraphEditorWindow.Open(graphObject.GraphAsset);
            }
        }
    }
}