using System;
using UnityEngine;

namespace FireGraph.Runtime
{
    [Serializable]
    public struct FireGraphConnection : IEquatable<FireGraphConnection>
    {
        public FireGraphConnectionPort inputConnection;
        public FireGraphConnectionPort outputConnection;

        public FireGraphConnection(FireGraphConnectionPort inputConnection, FireGraphConnectionPort outputConnection)
        {
            this.inputConnection = inputConnection;
            this.outputConnection = outputConnection;
        }

        public FireGraphConnection(string inputId, int inputIndex, string outputId, int outputIndex)
        {
            this.inputConnection = new FireGraphConnectionPort(inputId, inputIndex);
            this.outputConnection = new FireGraphConnectionPort(outputId, outputIndex);
        }

        public bool Equals(FireGraphConnection other)
        {
            return inputConnection.Equals(other.inputConnection) && outputConnection.Equals(other.outputConnection);
        }

        public override bool Equals(object obj)
        {
            return obj is FireGraphConnection other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(inputConnection, outputConnection);
        }
    }
    
    [Serializable]
    public struct FireGraphConnectionPort : IEquatable<FireGraphConnectionPort>
    {
        public string nodeId;
        public int portIndex;

        public FireGraphConnectionPort(string nodeId, int portIndex)
        {
            this.nodeId = nodeId;
            this.portIndex = portIndex;
        }

        public bool Equals(FireGraphConnectionPort other)
        {
            return nodeId == other.nodeId && portIndex == other.portIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is FireGraphConnectionPort other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(nodeId, portIndex);
        }
    }
}