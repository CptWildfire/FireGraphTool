using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FireGraph.Runtime;
using NUnit.Framework;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace FireGraph.Editor
{
    public class FireGraphWindowSearchProvider : ScriptableObject, ISearchWindowProvider
    {
        public FireNodeGraphView graphView;
        public VisualElement targetElement;

        public static List<SearchWindowContextElement> elements;
        
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext contextElement)
        {
            //Creat search menu tree
            List<SearchTreeEntry> tree = new List<SearchTreeEntry>();
            tree.Add(new SearchTreeGroupEntry(new GUIContent("Nodes"), 0));
            elements = new List<SearchWindowContextElement>();
            
            PopulateSearchTreeElements(ref elements);
            SortElementsByName(ref elements);
            BuildTree(ref tree, elements);
            
            return tree;
        }

        private void PopulateSearchTreeElements(ref List<SearchWindowContextElement> elements)
        {
            //Search in every assembly for type who implement NodeInfosAttribute.
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.CustomAttributes.ToList().Count > 0)
                    {
                        Attribute customAttribute = type.GetCustomAttribute(typeof(NodeInfoAttribute));
                        if (customAttribute != null)
                        {
                            //Creat node from the type who implement NodeInfosAttribute
                            NodeInfoAttribute nodeInfoAttribute = customAttribute as NodeInfoAttribute;
                            object node = Activator.CreateInstance(type);
                            
                            if (string.IsNullOrEmpty(nodeInfoAttribute.MenuItem))
                                continue;
                            
                            elements.Add(new SearchWindowContextElement(node, nodeInfoAttribute.MenuItem));
                        }
                    }
                }
            }
        }
        private void SortElementsByName(ref List<SearchWindowContextElement> elements)
        {
            elements.Sort((entry1, entry2) =>
            {
                string[] splits1 = entry1.title.Split("/");
                string[] splits2 = entry2.title.Split("/");

                for (int i = 0; i < splits1.Length; i++)
                {
                    if (i >= splits2.Length)
                        return 1;
                    
                    int value = String.Compare(splits1[i], splits2[i], StringComparison.Ordinal);

                    if (value != 0)
                    {
                        if (splits1.Length != splits2.Length && (i == splits1.Length - 1 || i == splits2.Length - 1))
                            return splits1.Length < splits2.Length ? 1 : -1;
                        return value;
                    }
                }
                
                return 0;
            });
        }
        private void BuildTree(ref List<SearchTreeEntry> tree, List<SearchWindowContextElement> elements)
        {
            List<string> subMenus = new List<string>();

            foreach (SearchWindowContextElement element in elements)
            {
                string[] title = element.title.Split("/");
                string subMenuName = "";

                for (int i = 0; i < title.Length - 1; i++)
                {
                    subMenuName += title[i];
                    if (!subMenus.Contains(subMenuName))
                    {
                        tree.Add(new SearchTreeGroupEntry(new GUIContent(title[i]), i + 1));
                        subMenus.Add(subMenuName);
                    }
                    subMenuName += "/";
                }
                
                SearchTreeEntry entry = new SearchTreeEntry(new GUIContent(title[^1]));
                entry.level = title.Length;
                entry.userData = new SearchWindowContextElement(element.target, element.title);
                tree.Add(entry);
            }
        }
        
        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext contextElement)
        {
            Vector2 windowMousePosition = graphView.ChangeCoordinatesTo(graphView, contextElement.screenMousePosition - graphView.Window.position.position);
            Vector2 graphMousePosition = graphView.contentViewContainer.WorldToLocal(windowMousePosition);
            
            SearchWindowContextElement element = (SearchWindowContextElement)SearchTreeEntry.userData;
            
            if (element.target is not GraphNode node) 
                return false;
            
            node.SetPosition(new Rect(graphMousePosition, new Vector2()));
            graphView.AddNode(node);
            return true;
        }
        
        public struct SearchWindowContextElement
        {
            public object target { get; private set; }
            public string title { get; private set; }
        
            public SearchWindowContextElement(object target, string title)
            {
                this.target = target;
                this.title = title;
            }
        }
    }
}