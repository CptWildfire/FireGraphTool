using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FireGraph.Runtime;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace FireGraph.Editor
{
    public class FireGraphEditorNode : Node
    {
        private GraphNode graphNode;
        
        private SerializedObject serializedObject;
        private SerializedProperty serializedNodeProperty;
        private FireGraphAsset assetParent;
        
        private Port outputPort;
        private List<Port> flowPorts;
        private List<Port> dataPorts;

        private const string GRAPH_VARIABLE_CLASS = "graph-variable-property";
        private const string NODE_PROPERTY_CLASS = "custom-node-property";
        
        private Dictionary<int, VisualElement> dataPortProperties;
        
        public GraphNode GraphNode => graphNode;
        public List<Port> FlowPorts => flowPorts;
        public List<Port> DataPorts => dataPorts;
        
        public FireGraphEditorNode(GraphNode graphNode, SerializedObject serializedObject, FireGraphAsset asset)
        {
            if (!TryGetAttribute(graphNode, out NodeInfoAttribute attribute))
                return;
            
            assetParent = asset;
            this.serializedObject = serializedObject;
            this.graphNode = graphNode;
            
            InitNodeEditorData(attribute);
            CreatePorts(attribute);
            ShowNodeProperties(graphNode.GetType());
        }

        public void UpdatePosition()
        {
            graphNode.SetPosition(GetPosition());
        }

        public void RefreshProperties()
        {
            List<VisualElement> elementsToRemove = new List<VisualElement>();
            
            //elementsToRemove.AddRange(extensionContainer.Query(className: NODE_PROPERTY_CLASS).ToList());
            elementsToRemove.AddRange(extensionContainer.Query(className: GRAPH_VARIABLE_CLASS).ToList());
            
            foreach (VisualElement visualElement in elementsToRemove)
                extensionContainer.Remove(visualElement);
            
            ShowNodeProperties(graphNode.GetType());
        }

        private bool TryGetAttribute(GraphNode graphNode, out NodeInfoAttribute attribute)
        {
            attribute = null;
            
            if (graphNode == null)
            {
                Debug.LogError(new ArgumentNullException(nameof(graphNode)));
                return false;
            }
            
            Type typeInfos = graphNode.GetType();
            attribute = typeInfos.GetCustomAttribute<NodeInfoAttribute>();

            if (attribute == null)
            {
                Debug.LogError(new ArgumentNullException(nameof(attribute)));
                return false;
            }
            
            return true;
        }

        private void InitNodeEditorData(NodeInfoAttribute attribute)
        {
            AddToClassList("fire-graph-node");
            name = graphNode.GetType().Name;
            title = attribute.Title;
            titleContainer.style.unityTextOutlineColor = new StyleColor(Color.white);
            titleContainer.style.unityTextOutlineWidth = new StyleFloat(0.2f);
            
            if (ColorUtility.TryParseHtmlString(attribute.HeadColor, out Color titleColor))
                titleContainer.style.backgroundColor = titleColor;
            
            if (ColorUtility.TryParseHtmlString("#3F3F3F", out Color extensionColor)) 
                extensionContainer.style.backgroundColor = extensionColor;
            
            flowPorts = new List<Port>();
            dataPorts = new List<Port>();
            dataPortProperties = new Dictionary<int, VisualElement>();
            
            string[] depths = attribute.MenuItem.Split('/');
            foreach (string depth in depths)
            {
                AddToClassList(depth.ToLower().Replace(" ", "-"));
            }
        }

        private void CreatePorts(NodeInfoAttribute attribute)
        { 
            if (attribute.HasFlowInput)
                CreateInputFlowPort(attribute);
            
            CreateOutputFlowPorts(graphNode.GetType());
            CreateDataPorts(graphNode.GetType());
        }

        private void CreateOutputFlowPorts(Type typeInfos)
        {
            List<FlowOutPortAttribute> portAttributes = new List<FlowOutPortAttribute>();
            foreach (FieldInfo fieldInfos in typeInfos.GetFields())
            {
                if (fieldInfos.GetCustomAttribute<FlowOutPortAttribute>() is { } attribute)
                {
                    portAttributes.Add(attribute);
                }
            }

            portAttributes = portAttributes.OrderBy(a => a.PortIndex).ToList();

            foreach (FlowOutPortAttribute outPortAttribute in portAttributes)
            {
                CreateOutputFlowPort(outPortAttribute);
            }
        }

        private void CreateInputFlowPort(NodeInfoAttribute attribute)
        {
            Port inputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(FireGraphPortTypes.FlowPort));
            inputPort.portName = "In";
            inputPort.tooltip = "Flow input port.";
            
            flowPorts.Add(inputPort);
            inputContainer.Add(inputPort);
        }

        private void CreateOutputFlowPort(FlowOutPortAttribute attribute)
        {
            outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(FireGraphPortTypes.FlowPort));
            outputPort.portName = attribute.PortName;
            outputPort.tooltip = "Flow output port.";
            
            flowPorts.Add(outputPort);
            outputContainer.Add(outputPort);
        }

        private void CreateDataPorts(Type typeInfos)
        {
            List<DataPortAttribute> portAttributes = new List<DataPortAttribute>();
            foreach (FieldInfo fieldInfos in typeInfos.GetFields())
            {
                if (fieldInfos.GetCustomAttribute<DataPortAttribute>() is { } attribute)
                {
                    attribute.FieldAttributeName = fieldInfos.Name;
                    portAttributes.Add(attribute);
                }
            }
            
            portAttributes = portAttributes.OrderBy(a => a.Index).ToList();
            
            foreach (DataPortAttribute outPortAttribute in portAttributes)
            {
                CreateDataPort(outPortAttribute);
            }
        }

        private void CreateDataPort(DataPortAttribute attribute)
        {
            Direction direction = attribute.Direction == DataPortAttribute.NodeDirection.In ? Direction.Input : Direction.Output;
            Port.Capacity capacity = direction == Direction.Input ? Port.Capacity.Single : Port.Capacity.Multi;
            
            Port dataPort = InstantiatePort(Orientation.Horizontal, direction, capacity, typeof(FireGraphPortTypes.DataPort));
            dataPort.portName = "";
            
            if (ColorUtility.TryParseHtmlString(PortTypeColor.GetColor(attribute.Type), out Color color)) 
                dataPort.portColor = color;
            
            if (serializedNodeProperty == null)
                FetchNodeProperty();
            
            PropertyField propertyField = GetPropertyField(attribute.FieldAttributeName);
            
            VisualElement horizontalContainer = new VisualElement
            {
                style =
                {
                    flexDirection = direction == Direction.Output ? FlexDirection.RowReverse : FlexDirection.Row,
                    alignItems = Align.Center,
                }
            };
            
            horizontalContainer.Add(dataPort);

            if (attribute.ShowProperty)
            {
                horizontalContainer.Add(propertyField);
                dataPortProperties.Add(attribute.Index, propertyField);
            }
            else
            {
                dataPort.portName = attribute.FieldAttributeName;
            }
            
            dataPorts.Add(dataPort);
            extensionContainer.Add(horizontalContainer);
        }
        
        private void ShowNodeProperties(Type typeInfos)
        {
            if (typeInfos == typeof(ComponentCallNode))
                ShowComponentCallNodeProperties(typeInfos);
            else
                ShowGraphNodeProperties(typeInfos);
            
            RefreshExpandedState();
        }
        
        private void FetchNodeProperty()
        {
            SerializedProperty nodes = serializedObject.FindProperty("graphs");
            if (nodes.isArray && nodes.arraySize > 0)
            {
               int size = nodes.arraySize;
               for (int i = 0; i < size; i++)
               {
                   SerializedProperty nodeProperty = nodes.GetArrayElementAtIndex(i);
                   SerializedProperty nodeIdProperty = nodeProperty.FindPropertyRelative("guid");

                   if (nodeIdProperty.stringValue == graphNode.Id)
                   {
                       serializedNodeProperty = nodeProperty;
                   }
               }
            }
        }

        private void DrawGraphVariableProperty(FieldInfo field)
        {
            string currentValue = field.GetValue(graphNode) as string;
            var variableNames = assetParent.GraphVariables.Select(v => v.name).ToList();
            
            if (variableNames.Count == 0)
                variableNames.Add("<No Variable>");

            if (!variableNames.Contains(currentValue))
            {
                currentValue = variableNames[0];
                field.SetValue(graphNode, currentValue);
                EditorUtility.SetDirty(assetParent);
            }
            
            var popup = new PopupField<string>(ObjectNames.NicifyVariableName(field.Name), variableNames, currentValue);
            popup.AddToClassList(GRAPH_VARIABLE_CLASS);

            popup.RegisterValueChangedCallback(evt =>
            {
                field.SetValue(graphNode, evt.newValue);
                EditorUtility.SetDirty(assetParent);
            });
            extensionContainer.Add(popup);
        }

        private PropertyField DrawProperty(string propertyName)
        {
            if (serializedNodeProperty == null)
                FetchNodeProperty();
            
            if (serializedNodeProperty == null)
                return null;
            
            PropertyField propertyField = GetPropertyField(propertyName);
            
            propertyField.AddToClassList(NODE_PROPERTY_CLASS);
            extensionContainer.Add(propertyField);
            
            return propertyField;
        }

        private PropertyField GetPropertyField(string propertyName)
        {
            SerializedProperty property = serializedNodeProperty.FindPropertyRelative(propertyName);
            PropertyField propertyField = new PropertyField(property)
            {
                bindingPath = property.propertyPath
            };
            return propertyField;
        }

        public void ToggleDataPortProperty(bool toggle, int portIndex)
        {
            if (dataPortProperties.TryGetValue(portIndex, out VisualElement property))
            {
                property.style.opacity = toggle ? 1 : 0;
                property.style.visibility = toggle ? Visibility.Visible : Visibility.Hidden;
            }
            
            RefreshExpandedState();
        }

        private void ShowGraphNodeProperties(Type typeInfos)
        {
            foreach (FieldInfo fieldInfos in typeInfos.GetFields())
            {
                if (fieldInfos.GetCustomAttribute<NodePropertyAttribute>() is not null)
                {
                    if (fieldInfos.GetCustomAttribute<GraphVariableAttribute>() is {} graphVariable)
                        DrawGraphVariableProperty(fieldInfos);
                    else 
                        DrawProperty(fieldInfos.Name);
                }
            }
        }
        private void ShowComponentCallNodeProperties(Type typeInfos)
        {
            GameObject gameObject = assetParent.Executable.gameObject;
            FireGraphObject graphObject = assetParent.Executable;
            
            var objectField = new ObjectField("FireGraphObject")
            {
                value = gameObject,
                objectType = typeof(FireGraphObject),
                allowSceneObjects = true
            };
            objectField.SetEnabled(false);
            extensionContainer.Add(objectField);
            
            if (!gameObject)
                return;
            
            Component[] components = gameObject.GetComponents<Component>();
            List<string> methodOptions = new() { "<None>" };

            foreach (var component in components)
            {
                var type = component.GetType();
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var method in methods)
                {
                    if (method.GetCustomAttribute<GraphCallableAttribute>() != null)
                    {
                        methodOptions.Add($"{type.Name}/{method.Name}");
                    }
                }
            }

            if (graphNode is not ComponentCallNode callNode)
                return;
            
            string defaultSelection = "<None>";
            
            if (!graphObject.ContainComponentCallKey(graphNode.Id))
                graphObject.SetComponentCallValue(graphNode.Id, defaultSelection);
            
            if (string.IsNullOrEmpty(graphObject.GetComponentCallValue(graphNode.Id)) || !methodOptions.Contains(graphObject.GetComponentCallValue(graphNode.Id)))
                graphObject.SetComponentCallValue(graphNode.Id, defaultSelection);
            
            var popup = new PopupField<string>("Method:", methodOptions, graphObject.GetComponentCallValue(graphNode.Id));

            popup.RegisterValueChangedCallback(evt =>
            {
                graphObject.SetComponentCallValue(graphNode.Id, evt.newValue);
                EditorUtility.SetDirty(assetParent);
            });

            extensionContainer.Add(popup);
        }
    }
}