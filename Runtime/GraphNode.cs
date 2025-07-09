using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace FireGraph.Runtime
{
    [Serializable]
    public class GraphNode
    {
        [SerializeField] private string guid;
        [SerializeField] private Rect position;
        
        public string typeName;
        
        public string Id => guid;
        public Rect Position => position;

        protected Dictionary<int, object> dataOutputPorts = new Dictionary<int, object>();
        
        public GraphNode()
        {
            guid = Guid.NewGuid().ToString();
        }
        
        public bool TryGetInputValue<T>(int portIndex, out T value)
        {
            value = default(T);

            if (dataOutputPorts.TryGetValue(portIndex, out object inputValue))
            {
                //Check for same type
                if (inputValue is T typedValue)
                {
                    value = typedValue;
                    return true;
                }

                //Try to convert
                try
                {
                    value = (T)Convert.ChangeType(inputValue, typeof(T));
                    return true;
                }
                catch
                {
                    //Can't convert, return false
                    return false;
                }
            }

            return false;
        }
        public virtual string Execute(FireGraphAsset executedGraph, int outPutIndex = 1)
        {
            return executedGraph.GetOutputNode(this, outPutIndex)?.Id;
        }
        public void SetPosition(Rect newPosition)
        {
            position = newPosition;
        }
        
        public static bool TryStringToVector2(string value, out Vector2 vector)
        {
            vector = Vector2.zero;
            
            if (string.IsNullOrEmpty(value))
                return false;
            
            string[] parts = value.Trim('(', ')').Split(',');
            if (parts.Length != 2)
                return false;
            
            //Todo : check float.TryParse for each one to be sure
            vector = new Vector2(float.Parse(parts[0], CultureInfo.InvariantCulture), float.Parse(parts[1], CultureInfo.InvariantCulture));
            return true;
        }

        public static bool TryStringToVector3(string value, out Vector3 vector)
        {
            vector = Vector3.zero;
            
            if (string.IsNullOrEmpty(value))
                return false;
            
            string[] parts = value.Trim('(', ')').Split(',');
            if (parts.Length != 3)
                return false;
            
            //Todo : check float.TryParse for each one to be sure
            vector = new Vector3(float.Parse(parts[0], CultureInfo.InvariantCulture), float.Parse(parts[1], CultureInfo.InvariantCulture), float.Parse(parts[2], CultureInfo.InvariantCulture));
            return true;
        }
    }

    #region Process Nodes

    [NodeInfo("Start", "#bf8506", "Process/Start", false)]
    public class StartNode : GraphNode
    {
        [FlowOutPort("Out", 1)]
        public string outPort;
        
        public override string Execute(FireGraphAsset executedGraph, int outPutIndex = 1)
        {
            Debug.Log($"Start {executedGraph}");
            
            return base.Execute(executedGraph, 0); //force 0 cause no input as 0
        }
    }
    
    [NodeInfo("Branch", "", "Process/Branch")]
    public class BranchNode : GraphNode
    {
        [FlowOutPort("True", 1)]
        public string truePort;
        [FlowOutPort("False", 2)]
        public string falsePort;
        
        [DataPort(DataPortAttribute.NodeDirection.In, 0, typeof(bool))]
        public bool condition;
        
        public override string Execute(FireGraphAsset executedGraph, int outPutIndex = 1)
        {
            if (executedGraph.TryGetInputData(this, 0, out bool value)) 
                condition = value;
            
            int portIndex = condition ? 1 : 2;
            return executedGraph.GetOutputNode(this, portIndex)?.Id;
        }
    }
    
    [NodeInfo("Get Variable", "#40556b", "Process/Get Variable")]
    public class GetVariableNode : GraphNode
    {
        [FlowOutPort("Out", 1)]
        public string outPort;
        
        [NodeProperty, GraphVariable]
        public string varName;
        
        [DataPort(DataPortAttribute.NodeDirection.Out, 0, typeof(string), false)]
        public string value;
        
        public override string Execute(FireGraphAsset executedGraph, int outPutIndex = 1)
        {
            value = executedGraph.GraphVariables.FirstOrDefault(v => v.name == varName)?.value;
            
            dataOutputPorts.Add(0, value);
            return base.Execute(executedGraph, outPutIndex);
        }
    }

    #endregion
    
    #region Logic
    [NodeInfo("Set Bool", PortTypeColor.BOOL_COLOR, "Logic/SetBool")]
    public class SetBoolNode : GraphNode
    {
        [FlowOutPort("Out", 1)]
        public string outPort;
        
        [DataPort(DataPortAttribute.NodeDirection.Out, 0, typeof(bool))]
        public bool value;

        public override string Execute(FireGraphAsset executedGraph, int outPutIndex = 1)
        {
            //add output data pot in this dictionary to get it from other nodes.
            dataOutputPorts.Add(0, value);
            return base.Execute(executedGraph, outPutIndex);
        }
    }
    #endregion
    
    #region Math

    public static class MathNode
    {
        public static readonly string[] Operations = new string[8]
        {
            "+", "-", "*", "/", "%", "^", "min", "max"
        };
        
        public static readonly string[] Compare = new string[6]
        {
            "=", "!=", ">", ">=", "<", "<="
        };
    }

    [NodeInfo("IntToFloat", "#30852d", "Math/Convert/IntToFloat")]
    public class IntToFloatNode : GraphNode
    {
        [FlowOutPort("Out", 1)]
        public string outPort;
        
        [DataPort(DataPortAttribute.NodeDirection.In, 0, typeof(int))]
        public int intValue;
        
        [DataPort(DataPortAttribute.NodeDirection.Out, 1, typeof(float), false)]
        public float floatValue;
        
        public override string Execute(FireGraphAsset executedGraph, int outPutIndex = 1)
        {
            floatValue = (float)intValue;
            
            dataOutputPorts.Add(1, floatValue);
            return base.Execute(executedGraph, outPutIndex);
        }
    }
    [NodeInfo("FloatToInt", "#30852d", "Math/Convert/FloatToInt")]
    public class FloatToIntNode : GraphNode
    {
        [FlowOutPort("Out", 1)]
        public string outPort;
        
        [DataPort(DataPortAttribute.NodeDirection.In, 0, typeof(float))]
        public float floatValue;
        
        [DataPort(DataPortAttribute.NodeDirection.Out, 1, typeof(int), false)]
        public int intValue;
        
        public override string Execute(FireGraphAsset executedGraph, int outPutIndex = 1)
        {
            intValue = Mathf.RoundToInt(floatValue);
            
            dataOutputPorts.Add(1, intValue);
            return base.Execute(executedGraph, outPutIndex);
        }
    }
    
    #region Float
    [NodeInfo("Set Float", "#30852d", "Math/Float/SetFloat")]
    public class SetFloatNode : GraphNode
    {
        [FlowOutPort("Out", 1)]
        public string outPort;
        
        [DataPort(DataPortAttribute.NodeDirection.Out, 0, typeof(float))]
        public float value;
        
        public override string Execute(FireGraphAsset executedGraph, int outPutIndex = 1)
        {
            dataOutputPorts.Add(0, value);
            return base.Execute(executedGraph, outPutIndex);
        }
    }
    [NodeInfo("Float Operation", "#30852d", "Math/Float/Operation")]
    public class FloatOperationNode : GraphNode
    {
        [FlowOutPort("Out", 1)]
        public string outPort;
        
        [DataPort(DataPortAttribute.NodeDirection.In, 0, typeof(float))]
        public float valueA;
        [DataPort(DataPortAttribute.NodeDirection.In, 1, typeof(float))]
        public float valueB;
        
        [MathOperation][NodeProperty]
        public string operation;
        
        [DataPort(DataPortAttribute.NodeDirection.Out, 2, typeof(float), false)]
        public float result;
        
        public override string Execute(FireGraphAsset executedGraph, int outPutIndex = 1)
        {
            if (executedGraph.TryGetInputData(this, 0, out float varA)) 
                valueA = varA;
            if (executedGraph.TryGetInputData(this, 1, out float varB)) 
                valueB = varB;

            result = operation switch
            {
                "+" => Add(valueA, valueB),
                "-" => Sub(valueA, valueB),
                "*" => Multiply(valueA, valueB),
                "/" => Divide(valueA, valueB),
                "%" => Modulo(valueA, valueB),
                "^" => Pow(valueA, valueB),
                "min" => Min(valueA, valueB),
                "max" => Max(valueA, valueB),
                
                _ => throw new ArgumentOutOfRangeException()
            };
            
            dataOutputPorts.Add(2, result);
            
            return base.Execute(executedGraph, outPutIndex);
        }
        
        private float Add(float a, float b) => a + b;
        private float Sub(float a, float b) => a - b;
        private float Multiply(float a, float b) => a * b;
        private float Divide(float a, float b) => a / b;
        private float Modulo(float a, float b) => a % b;
        private float Pow(float a, float b) => (float)Math.Pow(a, b);
        private float Min(float a, float b) => a < b ? a : b;
        private float Max(float a, float b) => a > b ? a : b;
    }
    [NodeInfo("Float Compare", "#30852d", "Math/Float/Compare")]
    public class FloatCompare : GraphNode
    {
        [FlowOutPort("Out", 1)]
        public string outPort;
        
        [DataPort(DataPortAttribute.NodeDirection.In, 0, typeof(float))]
        public float valueA;
        [DataPort(DataPortAttribute.NodeDirection.In, 1, typeof(float))]
        public float valueB;
        
        [MathCompare][NodeProperty]
        public string operation;
        
        [DataPort(DataPortAttribute.NodeDirection.Out, 2, typeof(bool), false)]
        public bool result;

        public override string Execute(FireGraphAsset executedGraph, int outPutIndex = 1)
        {
            if (executedGraph.TryGetInputData(this, 0, out float varA)) 
                valueA = varA;
            if (executedGraph.TryGetInputData(this, 1, out float varB)) 
                valueB = varB;
            
            result = operation switch
            {
                "=" => Equal(valueA, valueB),
                "!=" => NotEqual(valueA, valueB),
                ">" => GreaterThan(valueA, valueB),
                "<" => LessThan(valueA, valueB),
                ">=" => GreaterThanOrEqual(valueA, valueB),
                "<=" => LessThanOrEqual(valueA, valueB),
                
                _ => throw new ArgumentOutOfRangeException()
            };
            
            dataOutputPorts.Add(2, result);
            
            return base.Execute(executedGraph, outPutIndex);
        }
        
        private bool Equal(float a, float b) => Mathf.Approximately(a, b);
        private bool NotEqual(float a, float b) => !Mathf.Approximately(a, b);
        private bool GreaterThan(float a, float b) => a > b;
        private bool LessThan(float a, float b) => a < b;
        private bool GreaterThanOrEqual(float a, float b) => a >= b;
        private bool LessThanOrEqual(float a, float b) => a <= b;
    }
    #endregion

    #region Int
    [NodeInfo("Set Int", "#30852d", "Math/Int/SetInt")]
    public class SetIntNode : GraphNode
    {
        [FlowOutPort("Out", 1)]
        public string outPort;
        
        [DataPort(DataPortAttribute.NodeDirection.Out, 0, typeof(int))]
        public int value;
        
        public override string Execute(FireGraphAsset executedGraph, int outPutIndex = 1)
        {
            dataOutputPorts.Add(0, value);
            return base.Execute(executedGraph, outPutIndex);
        }
    }
    [NodeInfo("Int Operation", "#30852d", "Math/Int/Operation")]
    public class IntOperationNode : GraphNode
    {
        [FlowOutPort("Out", 1)]
        public string outPort;
        
        [DataPort(DataPortAttribute.NodeDirection.In, 0, typeof(int))]
        public int valueA;
        [DataPort(DataPortAttribute.NodeDirection.In, 1, typeof(int))]
        public int valueB;
        
        [MathOperation][NodeProperty]
        public string operation;
        
        [DataPort(DataPortAttribute.NodeDirection.Out, 2, typeof(int), false)]
        public int result;
        
        public override string Execute(FireGraphAsset executedGraph, int outPutIndex = 1)
        {
            if (executedGraph.TryGetInputData(this, 0, out int varA)) 
                valueA = varA;
            if (executedGraph.TryGetInputData(this, 1, out int varB)) 
                valueB = varB;

            result = operation switch
            {
                "+" => Add(valueA, valueB),
                "-" => Sub(valueA, valueB),
                "*" => Multiply(valueA, valueB),
                "/" => Divide(valueA, valueB),
                "%" => Modulo(valueA, valueB),
                "^" => Pow(valueA, valueB),
                "min" => Min(valueA, valueB),
                "max" => Max(valueA, valueB),
                
                _ => throw new ArgumentOutOfRangeException()
            };
            
            dataOutputPorts.Add(2, result);
            
            return base.Execute(executedGraph, outPutIndex);
        }
        
        private int Add(int a, int b) => a + b;
        private int Sub(int a, int b) => a - b;
        private int Multiply(int a, int b) => a * b;
        private int Divide(int a, int b) => a / b;
        private int Modulo(int a, int b) => a % b;
        private int Pow(int a, int b) => (int)Math.Pow(a, b);
        private int Min(int a, int b) => a < b ? a : b;
        private int Max(int a, int b) => a > b ? a : b;
    }
    [NodeInfo("Int Compare", "#30852d", "Math/Int/Compare")]
    public class IntCompare : GraphNode
    {
        [FlowOutPort("Out", 1)]
        public string outPort;
        
        [DataPort(DataPortAttribute.NodeDirection.In, 0, typeof(int))]
        public int valueA;
        [DataPort(DataPortAttribute.NodeDirection.In, 1, typeof(int))]
        public int valueB;
        
        [MathCompare][NodeProperty]
        public string operation;
        
        [DataPort(DataPortAttribute.NodeDirection.Out, 2, typeof(bool), false)]
        public bool result;

        public override string Execute(FireGraphAsset executedGraph, int outPutIndex = 1)
        {
            if (executedGraph.TryGetInputData(this, 0, out int varA)) 
                valueA = varA;
            if (executedGraph.TryGetInputData(this, 1, out int varB)) 
                valueB = varB;
            
            result = operation switch
            {
                "=" => Equal(valueA, valueB),
                "!=" => NotEqual(valueA, valueB),
                ">" => GreaterThan(valueA, valueB),
                "<" => LessThan(valueA, valueB),
                ">=" => GreaterThanOrEqual(valueA, valueB),
                "<=" => LessThanOrEqual(valueA, valueB),
                
                _ => throw new ArgumentOutOfRangeException()
            };
            
            dataOutputPorts.Add(2, result);
            
            return base.Execute(executedGraph, outPutIndex);
        }
        
        private bool Equal(int a, int b) => a == b;
        private bool NotEqual(int a, int b) => a != b;
        private bool GreaterThan(int a, int b) => a > b;
        private bool LessThan(int a, int b) => a < b;
        private bool GreaterThanOrEqual(int a, int b) => a >= b;
        private bool LessThanOrEqual(int a, int b) => a <= b;
    }
    #endregion
    #endregion
    
    #region String

    [NodeInfo("Set String", PortTypeColor.STRING_COLOR, "Strings/SetString")]
    public class SetStringNode : GraphNode
    {
        [FlowOutPort("Out", 1)]
        public string outPort;
        
        [DataPort(DataPortAttribute.NodeDirection.Out, 0, typeof(string))]
        public string value;

        
        public override string Execute(FireGraphAsset executedGraph, int outPutIndex = 1)
        {
            //add output data pot in this dictionary to get it from other nodes.
            dataOutputPorts.Add(0, value);
            return base.Execute(executedGraph, outPutIndex);
        }
    }
    [NodeInfo("String Equal", PortTypeColor.STRING_COLOR, "Strings/Equal")]
    public class StringEqualNode : GraphNode
    {
        [FlowOutPort("Out", 1)]
        public string outPort;
        
        [DataPort(DataPortAttribute.NodeDirection.In, 0, typeof(string))]
        public string stringValueA;
        [DataPort(DataPortAttribute.NodeDirection.In, 1, typeof(string))]
        public string stringValueB;
        [DataPort(DataPortAttribute.NodeDirection.Out, 2, typeof(bool), false)]
        public bool value;
        
        public override string Execute(FireGraphAsset executedGraph, int outPutIndex = 1)
        {
            if (executedGraph.TryGetInputData(this, 0, out string varA)) 
                stringValueA = varA;
            if (executedGraph.TryGetInputData(this, 1, out string varB)) 
                stringValueB = varB;
            
            value = string.Equals(stringValueA, stringValueB, StringComparison.InvariantCulture);
            
            dataOutputPorts.Add(2, value);
            return base.Execute(executedGraph, outPutIndex);
        }
    }
    [NodeInfo("String To Bool", PortTypeColor.STRING_COLOR, "Strings/StringToBool")]
    public class StringToBoolNode : GraphNode
    {
        [FlowOutPort("Out", 1)]
        public string outPort;
        
        [DataPort(DataPortAttribute.NodeDirection.In, 0, typeof(string))]
        public string stringValue;
        
        [DataPort(DataPortAttribute.NodeDirection.Out, 1, typeof(bool), false)]
        public bool boolValue;
        [DataPort(DataPortAttribute.NodeDirection.Out, 2, typeof(bool), false)]
        public bool succeed;

        
        public override string Execute(FireGraphAsset executedGraph, int outPutIndex = 1)
        {
            if (executedGraph.TryGetInputData(this, 0, out string value)) 
                stringValue = value;
            
            succeed = bool.TryParse(stringValue, out boolValue);
            
            dataOutputPorts.Add(1, boolValue);
            dataOutputPorts.Add(2, succeed);
            return base.Execute(executedGraph, outPutIndex);
        }
    }
    [NodeInfo("String To Int", PortTypeColor.STRING_COLOR, "Strings/StringToInt")]
    public class StringToIntNode : GraphNode
    {
        [FlowOutPort("Out", 1)]
        public string outPort;
        
        [DataPort(DataPortAttribute.NodeDirection.In, 0, typeof(string))]
        public string stringValue;
        
        [DataPort(DataPortAttribute.NodeDirection.Out, 1, typeof(int), false)]
        public int intValue;
        [DataPort(DataPortAttribute.NodeDirection.Out, 2, typeof(bool), false)]
        public bool succeed;

        
        public override string Execute(FireGraphAsset executedGraph, int outPutIndex = 1)
        {
            if (executedGraph.TryGetInputData(this, 0, out string value)) 
                stringValue = value;
            
            succeed = int.TryParse(stringValue, out intValue);
            
            dataOutputPorts.Add(1, intValue);
            dataOutputPorts.Add(2, succeed);
            return base.Execute(executedGraph, outPutIndex);
        }
    }
    [NodeInfo("String To Float", PortTypeColor.STRING_COLOR, "Strings/StringToFloat")]
    public class StringToFloatNode : GraphNode
    {
        [FlowOutPort("Out", 1)]
        public string outPort;
        
        [DataPort(DataPortAttribute.NodeDirection.In, 0, typeof(string))]
        public string stringValue;
        
        [DataPort(DataPortAttribute.NodeDirection.Out, 1, typeof(float), false)]
        public float floatValue;
        [DataPort(DataPortAttribute.NodeDirection.Out, 2, typeof(bool), false)]
        public bool succeed;
        
        public override string Execute(FireGraphAsset executedGraph, int outPutIndex = 1)
        {
            if (executedGraph.TryGetInputData(this, 0, out string value)) 
                stringValue = value;
            
            succeed = float.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out floatValue);
            
            dataOutputPorts.Add(1, floatValue);
            dataOutputPorts.Add(2, succeed);
            return base.Execute(executedGraph, outPutIndex);
        }
    }
    [NodeInfo("String To Vector2", PortTypeColor.STRING_COLOR, "Strings/StringToVector2")]
    public class StringToV2Node : GraphNode
    {
        [FlowOutPort("Out", 1)]
        public string outPort;
        
        [DataPort(DataPortAttribute.NodeDirection.In, 0, typeof(string))]
        public string stringValue;
        
        [DataPort(DataPortAttribute.NodeDirection.Out, 1, typeof(Vector2), false)]
        public Vector2 vector2Value;
        [DataPort(DataPortAttribute.NodeDirection.Out, 2, typeof(bool), false)]
        public bool succeed;

        
        public override string Execute(FireGraphAsset executedGraph, int outPutIndex = 1)
        {
            if (executedGraph.TryGetInputData(this, 0, out string value)) 
                stringValue = value;
            
            succeed = TryStringToVector2(stringValue, out vector2Value);
            
            dataOutputPorts.Add(1, vector2Value);
            dataOutputPorts.Add(2, succeed);
            return base.Execute(executedGraph, outPutIndex);
        }
    }
    [NodeInfo("String To Vector3", PortTypeColor.STRING_COLOR, "Strings/StringToVector3")]
    public class StringToV3Node : GraphNode
    {
        [FlowOutPort("Out", 1)]
        public string outPort;
        
        [DataPort(DataPortAttribute.NodeDirection.In, 0, typeof(string))]
        public string stringValue;
        
        [DataPort(DataPortAttribute.NodeDirection.Out, 1, typeof(Vector3), false)]
        public Vector3 vector3Value;
        [DataPort(DataPortAttribute.NodeDirection.Out, 2, typeof(bool), false)]
        public bool succeed;
        
        public override string Execute(FireGraphAsset executedGraph, int outPutIndex = 1)
        {
            if (executedGraph.TryGetInputData(this, 0, out string value)) 
                stringValue = value;
            
            succeed =  TryStringToVector3(stringValue, out vector3Value);
            
            dataOutputPorts.Add(1, vector3Value);
            dataOutputPorts.Add(2, succeed);
            return base.Execute(executedGraph, outPutIndex);
        }
    }
    #endregion
    
    #region Debug
    [NodeInfo("DebugLog", "#7636ad", "Debug/Log")]
    public class DebugLogNode : GraphNode
    {
        [FlowOutPort("Out", 1)]
        public string outPort;
        
        [DataPort(DataPortAttribute.NodeDirection.In, 0, typeof(string))]
        public string message;
        [NodeProperty]
        public LogType logType;
        
        public override string Execute(FireGraphAsset executedGraph, int outPutIndex = 1)
        {
            if (executedGraph.TryGetInputData(this, 0, out string value))
                message = value;
            
            switch (logType)
            {
                case LogType.Log: Debug.Log(message);
                    break;
                case LogType.Warning: Debug.LogWarning(message);
                    break;
                case LogType.Error: Debug.LogError(message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            return base.Execute(executedGraph, 1);
        }
        
        [Serializable]
        public enum LogType
        {
            Log,
            Warning,
            Error,
        }
    }
    #endregion
}