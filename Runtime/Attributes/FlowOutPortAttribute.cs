using System;
using UnityEngine;

namespace FireGraph.Runtime
{
    public class FlowOutPortAttribute : Attribute
    {
        private string name;
        private int index;

        public string PortName => name;
        public int PortIndex => index;
        
        public FlowOutPortAttribute(string name, int index)
        {
            this.name = name;
            this.index = index;
        }
    }
}