using System;
using UnityEditor.Graphs;
using UnityEngine;

namespace FireGraph.Runtime
{
    public class NodeInfoAttribute : Attribute
    {
        private string nodeTitle;
        private string menuItem;

        private string headColor;
        
        private bool hasFlowInput;

        private const string defaultColor = "#3F3F3F";
        
        public string Title => nodeTitle;
        public string MenuItem => menuItem;
        
        public string HeadColor => headColor;
        
        public bool HasFlowInput => hasFlowInput;

        public NodeInfoAttribute(string nodeTitle, string hexColor = "#3F3F3F" , string menuItem = "", bool hasFlowInput = true)
        {
            this.nodeTitle = nodeTitle;
            this.menuItem = menuItem;
            
            this.headColor = string.IsNullOrEmpty(hexColor) ? defaultColor : hexColor;
            
            this.hasFlowInput = hasFlowInput;
        }
    }
}