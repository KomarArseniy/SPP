using System;

namespace TestFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class OrderAttribute : Attribute
    {
        public int Order { get; }
        public OrderAttribute(int order) { Order = order; }
    }
}
