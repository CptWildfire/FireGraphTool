using UnityEngine;

namespace FireGraph.Runtime
{
    public class MyComponent : MonoBehaviour
    {
        [GraphCallable]
        public void Test()
        {
            Debug.Log("Test");
        }
        
        [GraphCallable]
        public void Test2()
        {
            Debug.Log("Test2");
        }
    }
}