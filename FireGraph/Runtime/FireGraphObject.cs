using System;
using UnityEngine;

namespace FireGraph.Runtime
{
    public class FireGraphObject : MonoBehaviour
    {
        [SerializeField]
        private FireGraphAsset graphAsset;
        
        private void OnEnable()
        {
            Execute();   
        }

        public void Execute()
        {
            //Execute a copy of the graph asset to avoid modify some value during process.
            ExecuteGraph(Instantiate(graphAsset));
        }

        private void ExecuteGraph(FireGraphAsset graph)
        {
            graph.Init();
            
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
    }
}