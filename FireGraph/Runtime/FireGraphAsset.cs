using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphs;
using UnityEngine;
using UnityEngine.Serialization;

namespace FireGraph.Runtime
{
    [CreateAssetMenu(fileName = "FireGraphAsset", menuName = "FireGraph", order = 0)]
    public class FireGraphAsset : ScriptableObject
    {
        [SerializeReference] private List<GraphNode> graphs = new();
        [SerializeField] private List<FireGraphConnection> flowConnections = new();
        [SerializeField] private List<FireGraphConnection> dataConnections = new();
        [SerializeField] private List<FireGraphVariable> graphVariables = new();

        private Dictionary<string, GraphNode> nodes = new();
        
        public List<GraphNode> Graphs => graphs;
        public List<FireGraphConnection> FlowConnections => flowConnections;
        public List<FireGraphConnection> DataConnections => dataConnections;
        public List<FireGraphVariable> GraphVariables => graphVariables;

        public void Init()
        {
            foreach (GraphNode node in graphs)
            {
                nodes.TryAdd(node.Id, node);
            }
        }

        public GraphNode GetStartNode()
        {
            return graphs.OfType<StartNode>().FirstOrDefault();
        }

        public GraphNode GetNode(string nextNodeId)
        {
            return nodes.GetValueOrDefault(nextNodeId);
        }

        public bool NodeExist(string nextNodeId)
        {
            return nodes.ContainsKey(nextNodeId);
        }

        public GraphNode GetOutputNode(GraphNode currentNode, int portIndex)
        {
            foreach (FireGraphConnection graphConnection in flowConnections)
            {
                //get current node output port with right index
                if (graphConnection.outputConnection.nodeId == currentNode.Id && graphConnection.outputConnection.portIndex == portIndex)
                {
                    string connectedNodeId = graphConnection.inputConnection.nodeId;
                    return nodes.GetValueOrDefault(connectedNodeId);
                }
            }
            return null;
        }

        public bool TryGetInputData<T>(GraphNode currentNode, int dataPortIndex, out T value)
        {
            value = default(T);
            
            foreach (FireGraphConnection graphConnection in dataConnections)
            {
                //get current node output port with right index
                if (graphConnection.inputConnection.nodeId == currentNode.Id && graphConnection.inputConnection.portIndex == dataPortIndex)
                {
                    string connectedNodeId = graphConnection.outputConnection.nodeId;
                    if (nodes.TryGetValue(connectedNodeId, out GraphNode node)) 
                        return node.TryGetInputValue(graphConnection.outputConnection.portIndex, out value);
                }
            }
            
            return false;
        }
        
        public void RemoveBrokenConnections()
        {
            if (nodes.Count == 0)
                Init();
            
            RemoveBrokenConnection(ref flowConnections);
            RemoveBrokenConnection(ref dataConnections);
        }

        private void RemoveBrokenConnection(ref List<FireGraphConnection> connections)
        {
            int removedConnections = 0;
            
            for (int i = connections.Count- 1; i >= 0; i--)
            {
                if (!NodeExist(connections[i].inputConnection.nodeId) || !NodeExist(connections[i].outputConnection.nodeId))
                {
                    connections.RemoveAt(i);
                    removedConnections++;
                }
            }
            
            Debug.LogWarning($"{removedConnections} broken connections removed.");
        }
        
        [Serializable]
        public class FireGraphVariable
        {
            public string name;
            public string type;
            public string value;
        }
    }
}