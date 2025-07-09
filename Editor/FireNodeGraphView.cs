using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FireGraph.Runtime;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FireGraph.Editor
{
    //Can't be renamed cause of Unity doing stuff with it name ??? Sorry, this one will still be call FireNode instead of FireGraph :(
    public class FireNodeGraphView : GraphView
    {
        private SerializedObject serializedObject;
        private FireGraphEditorWindow window;
        private FireGraphAsset graphAsset;

        private FireGraphWindowSearchProvider searchProvider;

        private List<FireGraphEditorNode> graphNodes = new();
        private Dictionary<Edge, FireGraphConnection> connectionsDictionary = new();
        private Dictionary<string, FireGraphEditorNode> nodesDictionary = new();

        public FireGraphEditorWindow Window => window;

        public FireNodeGraphView(SerializedObject serializedObject, FireGraphEditorWindow window)
        {
            this.serializedObject = serializedObject;
            this.window = window;

            AddBackground();
            
            graphAsset = (FireGraphAsset)serializedObject.targetObject;
            searchProvider = ScriptableObject.CreateInstance<FireGraphWindowSearchProvider>();
            searchProvider.graphView = this;
            
            this.StretchToParentSize();

            this.nodeCreationRequest = ShowSearchWindow;
            
            AddManipulators();
            AddBlackboard();

            AddExistingNodes();
            AddExistingConnections();

            //Keep it last to avoid infinite update cycle
            graphViewChanged += OnGraphViewChanged;
        }

        public void AddNode(GraphNode node)
        {
            AddNodeData(node);
            AddOnGraph(node);

            BindNodes();
        }

        public FireGraphEditorNode GetNode(string guid)
        {
            return nodesDictionary.GetValueOrDefault(guid);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> allPorts = new List<Port>();
            List<Port> compatiblePorts = new List<Port>();

            foreach (FireGraphEditorNode editorNode in graphNodes)
            {
                allPorts.AddRange(editorNode.FlowPorts);
                allPorts.AddRange(editorNode.DataPorts);
            }

            foreach (Port currentPort in allPorts)
            {
                if (IsPortCompatible(startPort, currentPort))
                    compatiblePorts.Add(currentPort);
            }

            return compatiblePorts;
        }

        private void AddBackground()
        {
            //!! be careful, maybe this ref can be lost depending how the user will install the package. !!
            StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/FireGraph/Editor/USS/FireGraphEditor.uss");
            styleSheets.Add(styleSheet);

            GridBackground background = new GridBackground { name = "GridBackground" };
            Add(background);
            background.SendToBack();
        }

        private void AddManipulators()
        {
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new ClickSelector());
            this.AddManipulator(new ContentZoomer());
        }

        private void AddExistingNodes()
        {
            foreach (GraphNode graphNode in graphAsset.Graphs)
            {
                AddOnGraph(graphNode);
            }

            BindNodes();
        }

        private void AddExistingConnections()
        {
            foreach (FireGraphConnection connection in graphAsset.FlowConnections)
            {
                DrawEdge(connection, true);
            }

            foreach (FireGraphConnection connection in graphAsset.DataConnections)
            {
                DrawEdge(connection, false);
            }
        }
        
        private void AddBlackboard()
        {
            Blackboard blackboard = new Blackboard(this)
            {
                scrollable = true
            };
            blackboard.Add(new BlackboardSection { title = "Variables" });

            blackboard.addItemRequested = _ => AddVariable();
            blackboard.editTextRequested = (bb, element, newName) => RenameVariable(element, newName);
            blackboard.SetPosition(new Rect(10, 30, 200, 300));

            Add(blackboard);
            AddExistingVariables();
        }

        private void AddExistingVariables()
        {
            foreach (FireGraphAsset.FireGraphVariable variable in graphAsset.GraphVariables)
            {
                AddExistingVariable(variable);
            }
        }

        private void AddExistingVariable(FireGraphAsset.FireGraphVariable variable)
        {
            AddBlackboardVariable(variable);
        }
        
        private void AddVariable()
        {
            GenericMenu menu = new GenericMenu();

            string[] types = new[] { "bool", "int", "float", "string", "Vector2", "Vector3" };

            foreach (string type in types)
            {
                menu.AddItem(new GUIContent(type), false, () =>
                {
                    CreateVariable(GetDefaultVariableName(type), type);
                });
            }

            menu.ShowAsContext();
        }

        private void RemoveVariable(BlackboardRow row)
        {
            string variableName = row.Q<BlackboardField>().userData.ToString();

            var variable = graphAsset.GraphVariables.Find(v => v.name == variableName);
            if (variable != null)
            {
                graphAsset.GraphVariables.Remove(variable);
                EditorUtility.SetDirty(graphAsset);
                AssetDatabase.SaveAssets();
            }

            row.RemoveFromHierarchy();
            
            foreach (FireGraphEditorNode node in graphNodes)
                node.RefreshProperties();
        }

        private string GetDefaultVariableName(string type)
        {
            string varName = $"new{char.ToUpper(type[0]) + type.Substring(1)}";
            return GetAvailableVariableName(varName);
        }

        private string GetAvailableVariableName(string varName)
        {
            int occurence = 1;
            string newVarName = varName;
            
            while (graphAsset.GraphVariables.Any(v => v.name == newVarName))
            {
                newVarName = varName + occurence;
                occurence++;
            }
            
            return newVarName;
        }
        
        private void CreateVariable(string varName, string type)
        {
            varName = GetAvailableVariableName(varName);
            
            FireGraphAsset.FireGraphVariable newVariable = new FireGraphAsset.FireGraphVariable()
            {
                name = varName,
                type = type,
                value = GetDefaultValueType(type)
            };
            
            graphAsset.GraphVariables.Add(newVariable);

            AddBlackboardVariable(newVariable);
        }

        private void AddBlackboardVariable(FireGraphAsset.FireGraphVariable variable)
        {
            BlackboardField field = new BlackboardField
            {
                text = variable.name,
                typeText = variable.type,
                userData = variable.name,
            };
            
            VisualElement inputField = CreateInputFieldForType(variable);
            BlackboardRow row = new BlackboardRow(field, inputField)
            {
                userData = variable.name
            };
            
            Button deleteButton = new Button(() => RemoveVariable(row))
            {
                text = "X",
                style =
                {
                    width = 16,
                    height = 16,
                    marginLeft = 4,
                    alignSelf = Align.Center,
                    marginTop = 0,
                    marginBottom = 0,
                }
            };
            
            field.Add(deleteButton);
            this.Q<BlackboardSection>().Add(row);

            foreach (FireGraphEditorNode node in graphNodes)
                node.RefreshProperties();
        }
        
        private VisualElement CreateInputFieldForType(FireGraphAsset.FireGraphVariable variable)
        {
            switch (variable.type)
            {
                case "bool":
                    var toggle = new Toggle { value = bool.Parse(variable.value) };
                    toggle.RegisterValueChangedCallback(evt => SetVariableValue(variable, evt.newValue.ToString()));
                    return toggle;

                case "int":
                    var intField = new IntegerField { value = int.Parse(variable.value) };
                    intField.RegisterValueChangedCallback(evt => SetVariableValue(variable, evt.newValue.ToString()));
                    return intField;

                case "float":
                    var floatField = new FloatField { value = float.Parse(variable.value, CultureInfo.InvariantCulture) };
                    floatField.RegisterValueChangedCallback(evt => SetVariableValue(variable, evt.newValue.ToString(CultureInfo.InvariantCulture)));
                    return floatField;

                case "string":
                    var textField = new TextField { value = variable.value };
                    textField.RegisterValueChangedCallback(evt => SetVariableValue(variable, evt.newValue.ToString()));
                    return textField;

                case "Vector2":
                    var vec2 = StringToVector2(variable.value);
                    var vec2Field = new Vector2Field { value = vec2 };
                    vec2Field.RegisterValueChangedCallback(evt => SetVariableValue(variable, evt.newValue.ToString()));
                    return vec2Field;

                case "Vector3":
                    var vec3 = StringToVector3(variable.value);
                    var vec3Field = new Vector3Field { value = vec3 };
                    vec3Field.RegisterValueChangedCallback(evt => SetVariableValue(variable, evt.newValue.ToString()));
                    return vec3Field;

                default:
                    return new Label("Unsupported type");
            }
        }

        private string GetDefaultValueType(string type)
        {
            return type switch
            {
                "bool" => false.ToString(),
                "int" => "0",
                "float" => "0",
                "string" => "",
                "Vector2" => Vector2.zero.ToString(),
                "Vector3" => Vector3.zero.ToString(),
                _ => null
            };
        }

        private void SetVariableValue(FireGraphAsset.FireGraphVariable variable, string value)
        {
            variable.value = value.ToString();
            EditorUtility.SetDirty(graphAsset);
        }
        
        private Vector2 StringToVector2(string value)
        {
            var parts = value.Trim('(', ')').Split(',');
            return new Vector2(float.Parse(parts[0], CultureInfo.InvariantCulture), float.Parse(parts[1], CultureInfo.InvariantCulture));
        }

        private Vector3 StringToVector3(string value)
        {
            var parts = value.Trim('(', ')').Split(',');
            return new Vector3(float.Parse(parts[0], CultureInfo.InvariantCulture), float.Parse(parts[1], CultureInfo.InvariantCulture), float.Parse(parts[2], CultureInfo.InvariantCulture));
        }
        
        private void RenameVariable(VisualElement element, string newName)
        {
            string oldName = element.userData.ToString();
            newName = GetAvailableVariableName(newName);
            
            FireGraphAsset.FireGraphVariable variable = graphAsset.GraphVariables.Find(x => x.name == oldName);
            if (variable != null)
            {
                variable.name = newName;
                
                if (element is BlackboardField field)
                {
                    field.text = newName;
                    field.name = newName;
                }

                element.userData = newName;
            }
            
            EditorUtility.SetDirty(graphAsset);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange changes)
        {
            if (changes.movedElements != null)
                OnElementMoved(changes);

            if (changes.elementsToRemove != null)
                OnElementRemoved(changes);

            if (changes.edgesToCreate != null)
                OnElementConnected(changes);

            return changes;
        }

        private void OnElementMoved(GraphViewChange changes)
        {
            Undo.RecordObject(serializedObject.targetObject, "Move Nodes");

            foreach (FireGraphEditorNode editorNode in changes.movedElements.OfType<FireGraphEditorNode>())
            {
                editorNode.UpdatePosition();
            }
        }

        private void OnElementRemoved(GraphViewChange changes)
        {
            Undo.RecordObject(serializedObject.targetObject, "Remove Nodes");

            List<FireGraphEditorNode> nodesToRemove = changes.elementsToRemove.OfType<FireGraphEditorNode>().ToList();
            if (nodesToRemove.Any())
                RemoveNodes(nodesToRemove);

            List<Edge> edgeToRemove = changes.elementsToRemove.OfType<Edge>().ToList();
            if (edgeToRemove.Any())
                RemoveConnections(edgeToRemove);
        }

        private void OnElementConnected(GraphViewChange changes)
        {
            Undo.RecordObject(serializedObject.targetObject, "Add Connections");
            foreach (Edge edge in changes.edgesToCreate)
            {
                CreateConnection(edge);
            }
        }

        private void ShowSearchWindow(NodeCreationContext context)
        {
            searchProvider.targetElement = (VisualElement)focusController.focusedElement;
            SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchProvider);
        }

        private void AddNodeData(GraphNode node)
        {
            Undo.RecordObject(serializedObject.targetObject, "Add Node");
            graphAsset.Graphs.Add(node);
            serializedObject.Update();
        }

        private void AddOnGraph(GraphNode node)
        {
            node.typeName = node.GetType().AssemblyQualifiedName;

            FireGraphEditorNode graphEditorNode = new FireGraphEditorNode(node, serializedObject, graphAsset);
            graphEditorNode.SetPosition(node.Position);

            graphNodes.Add(graphEditorNode);
            nodesDictionary.Add(node.Id, graphEditorNode);

            AddElement(graphEditorNode);
        }

        private void CreateConnection(Edge edge)
        {
            if (edge.input.node is not FireGraphEditorNode inputNode || edge.output.node is not FireGraphEditorNode outputNode)
                return;

            int inputIndex = -1;
            int outputIndex = -1;

            bool isFlow = false;

            if (inputNode.FlowPorts.Contains(edge.input))
            {
                inputIndex = inputNode.FlowPorts.IndexOf(edge.input);
                outputIndex = outputNode.FlowPorts.IndexOf(edge.output);
                isFlow = true;
            }
            else if (inputNode.DataPorts.Contains(edge.input))
            {
                inputIndex = inputNode.DataPorts.IndexOf(edge.input);
                outputIndex = outputNode.DataPorts.IndexOf(edge.output);
                
                inputNode.ToggleDataPortProperty(false, inputIndex);
                
                isFlow = false;
            }

            FireGraphConnection connection = new FireGraphConnection(inputNode.GraphNode.Id, inputIndex, outputNode.GraphNode.Id, outputIndex);
            if (isFlow)
                graphAsset.FlowConnections.Add(connection);
            else
                graphAsset.DataConnections.Add(connection);

            connectionsDictionary.Add(edge, connection);
        }

        private void RemoveConnections(List<Edge> edgeToRemove)
        {
            for (int i = edgeToRemove.Count - 1; i >= 0; i--)
            {
                RemoveConnection(edgeToRemove[i]);
            }
        }

        private void RemoveConnection(Edge edge)
        {
            if (connectionsDictionary.TryGetValue(edge, out FireGraphConnection connection))
            {
                if (graphAsset.FlowConnections.Contains(connection))
                    graphAsset.FlowConnections.Remove(connection);
                
                if (graphAsset.DataConnections.Contains(connection))
                {
                    graphAsset.DataConnections.Remove(connection);
                    if (edge.input.node is FireGraphEditorNode inputNode)
                        inputNode.ToggleDataPortProperty(true, connection.inputConnection.portIndex);
                }

                connectionsDictionary.Remove(edge);
            }
        }

        private void DrawEdge(FireGraphConnection connection, bool isFlow)
        {
            FireGraphEditorNode inputNode = GetNode(connection.inputConnection.nodeId);
            FireGraphEditorNode outputNode = GetNode(connection.outputConnection.nodeId);

            if (inputNode == null || outputNode == null)
            {
                Debug.LogError($"Cannot draw connection because one or both connections are invalid. (cf: input(${inputNode}), output(${outputNode}))");
                return;
            }

            Port inputPort;
            Port outputPort;

            if (isFlow)
            {
                inputPort = inputNode.FlowPorts[connection.inputConnection.portIndex];
                outputPort = outputNode.FlowPorts[connection.outputConnection.portIndex];
            }
            else
            {
                inputPort = inputNode.DataPorts[connection.inputConnection.portIndex];
                outputPort = outputNode.DataPorts[connection.outputConnection.portIndex];
                
                inputNode.ToggleDataPortProperty(false, connection.inputConnection.portIndex);
            }


            Edge edge = inputPort.ConnectTo(outputPort);
            AddElement(edge);
            connectionsDictionary.TryAdd(edge, connection);
        }

        private void RemoveNodes(List<FireGraphEditorNode> nodesToRemove)
        {
            for (int i = nodesToRemove.Count - 1; i >= 0; i--)
            {
                RemoveNode(nodesToRemove[i]);
            }
        }

        private void RemoveNode(FireGraphEditorNode nodeEditor)
        {
            graphAsset.Graphs.Remove(nodeEditor.GraphNode);
            graphNodes.Remove(nodeEditor);
            nodesDictionary.Remove(nodeEditor.GraphNode.Id);

            serializedObject.Update();
        }

        private void BindNodes()
        {
            serializedObject.Update();
            this.Bind(serializedObject);
        }

        private bool IsPortCompatible(Port startPort, Port endPort)
        {
            if (endPort == startPort)
                return false;
            if (endPort.node == startPort.node)
                return false;
            if (endPort.direction == startPort.direction)
                return false;
            if (endPort.portType != startPort.portType)
                return false;

            return true;
        }
    }
}