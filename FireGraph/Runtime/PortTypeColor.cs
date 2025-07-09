using System;
using UnityEngine;

namespace FireGraph.Runtime
{
    public static class PortTypeColor
    {
        public const string BOOL_COLOR = "#245185";
        public const string INT_COLOR = "#ad1d1d";
        public const string FLOAT_COLOR = "#1dad95";
        public const string STRING_COLOR = "#94466a";
        public const string VECTOR2_COLOR = "#c48545";
        public const string VECTOR3_COLOR = "#8dc445";
        public const string DEFAULT_COLOR = "#5e463b";

        public static string GetColor(Type nodeType)
        {
            if (nodeType == typeof(bool))
                return BOOL_COLOR;
            if (nodeType == typeof(int))
                return INT_COLOR;
            if (nodeType == typeof(float))
                return FLOAT_COLOR;
            if (nodeType == typeof(string))
                return STRING_COLOR;
            if (nodeType == typeof(Vector2))
                return VECTOR2_COLOR;
            if (nodeType == typeof(Vector3))
                return VECTOR3_COLOR;
            
            return DEFAULT_COLOR;
        }
    }
}