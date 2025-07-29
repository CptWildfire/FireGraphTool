using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FireGraph.Runtime
{
    public class FireGraphObject : MonoBehaviour
    {
        [SerializeField]
        private FireGraphAsset graphAsset;
        public bool executeOnEnable = false;

        [HideInInspector]
        public List<ComponentCallEntry> ComponentCallData = new();
        
        public FireGraphAsset GraphAsset => graphAsset;
        
        private void OnEnable()
        {
            if (executeOnEnable) 
                Execute();
        }

        private void OnValidate()
        {
            if (!graphAsset.Executable || graphAsset.Executable != this)
                graphAsset.Executable = this;
            
            if (ComponentCallData == null)
                Debug.LogWarning("ComponentCallData is null");
        }

        public void Execute()
        {
            //Execute a copy of the graph asset to avoid modify some value during process.
            if (graphAsset) 
                ExecuteGraph(Instantiate(graphAsset), this);
            else
                Debug.LogWarning("No graph asset selected.");
        }
        
        public void SetComponentCallValue(string key, string value)
        {
            var entry = ComponentCallData.FirstOrDefault(e => e.key == key);
            if (entry != null)
                entry.value = value;
            else
                ComponentCallData.Add(new ComponentCallEntry { key = key, value = value });
        }

        public bool ContainComponentCallKey(string key)
        {
            return ComponentCallData.Any(e => e.key == key);
        }

        public string GetComponentCallValue(string key)
        {
            return ComponentCallData.FirstOrDefault(e => e.key == key)?.value;
        }


        private void ExecuteGraph(FireGraphAsset graph, FireGraphObject fireGraphObject)
        {
            graph.Init(fireGraphObject);
            
            GraphNode startNode = graph.GetStartNode();

            if (startNode == null)
                throw new NullReferenceException("Cannot execute a graph with no StartNode.");
            
            ExecuteToNextNode(graph, startNode);
        }

        private void ExecuteToNextNode(FireGraphAsset graphInstance, GraphNode currentNode)
        {
            if (currentNode == null)
                throw new NullReferenceException($"Error while executing {graphAsset.name} on {gameObject.name}.\nCurrent node is null.");
            
            string nextNodeId = currentNode.Execute(graphInstance);

            if (!string.IsNullOrEmpty(nextNodeId))
            {
                GraphNode nextNode = graphInstance.GetNode(nextNodeId);
                ExecuteToNextNode(graphInstance, nextNode);
            }
        }
        
        [Serializable]
        public class ComponentCallEntry
        {
            public string key;
            public string value;
        }
    }
}