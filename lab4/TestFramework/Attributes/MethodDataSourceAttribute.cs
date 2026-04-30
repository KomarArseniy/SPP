using System;

namespace TestFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MethodDataSourceAttribute : Attribute
    {
        public string MethodName { get; }

        public MethodDataSourceAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}
