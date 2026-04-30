using System;

namespace TestFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class IgnoreAttribute : Attribute
    {
        public string Reason { get; }

        public IgnoreAttribute(string reason)
        {
            Reason = reason;
        }
    }
}