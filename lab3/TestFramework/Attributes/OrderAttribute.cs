using System;
using System.Collections.Generic;
using System.Text;

namespace TestFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class OrderAttribute : Attribute
    {
        public int Order { get; }

        public OrderAttribute(int order)
        {
            Order = order;
        }
    }
}
