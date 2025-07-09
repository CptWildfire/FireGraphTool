using System;
using UnityEngine;

namespace FireGraph.Runtime
{
    public class DataPortAttribute : Attribute
    {
        private NodeDirection direction;
        private Type type;
        private int index;
        private bool showProperty;

        public NodeDirection Direction => direction;
        public Type Type => type;
        public int Index => index;
        public bool ShowProperty => showProperty;
        
        public string FieldAttributeName { get; set; }
        
        public DataPortAttribute(NodeDirection direction, int index, Type type, bool showProperty = true)
        {
            this.direction = direction;
            this.type = type;
            this.index = index;
            this.showProperty = showProperty;
        }
        
        [Serializable]
        public enum NodeDirection
        {
            In,
            Out
        }
    }
}