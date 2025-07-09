using System;
using FireGraph.Runtime;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Serialization;

namespace FireGraph.Editor
{
    public class FireGraphEditorWindow : UnityEditor.EditorWindow
    {
        [SerializeField] private FireGraphAsset fireGraphAsset;
        [SerializeField] private FireNodeGraphView graphView;
        [SerializeField] private SerializedObject serializedObject;

        public FireGraphAsset FireGraphAsset => fireGraphAsset;
        
        public static void Open(FireGraphAsset fireGraphAsset)
        {
            //GetAll currently opened window
            FireGraphEditorWindow[] windows = Resources.FindObjectsOfTypeAll<FireGraphEditorWindow>();

            foreach (FireGraphEditorWindow window in windows)
            {
                //if on opened window already contain our asset, we can focus on it instead of creating a new one
                if (window.FireGraphAsset == fireGraphAsset)
                {
                    window.Focus();
                    return;
                }
            }
            
            CreateNewWindow(fireGraphAsset);
        }
        
        private static void CreateNewWindow(FireGraphAsset fireGraphAsset)
        {
            if (fireGraphAsset == null)
            {
                throw new System.ArgumentNullException("Can't create new window for a null asset.");
            }
            
            FireGraphEditorWindow newWindow = CreateWindow<FireGraphEditorWindow>(typeof(FireGraphEditorWindow),typeof(SceneView));
            newWindow.titleContent = new GUIContent($"{fireGraphAsset.name}", EditorGUIUtility.ObjectContent(null, typeof(FireGraphAsset)).image);
            newWindow.Load(fireGraphAsset);
        }

        private void OnGUI()
        {
            if (fireGraphAsset)
            {
                hasUnsavedChanges = EditorUtility.IsDirty(fireGraphAsset);
            }
        }
        
        private void OnEnable()
        {
            if (graphView == null && fireGraphAsset != null)
            {
                DrawGraphView();
            }
        }

        private void Load(FireGraphAsset fireGraphAsset)
        {
            this.fireGraphAsset = fireGraphAsset;
            DrawGraphView();
        }

        private GraphViewChange OnCurrentGraphChanged(GraphViewChange changes)
        {
            EditorUtility.SetDirty(fireGraphAsset);
            return changes;
        }

        private void DrawGraphView()
        {
            serializedObject = new SerializedObject(fireGraphAsset);
            graphView = new FireNodeGraphView(serializedObject, this);
            rootVisualElement.Add(graphView);
            
            graphView.graphViewChanged += OnCurrentGraphChanged;
        }
    }
}