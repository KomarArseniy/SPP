using System;

namespace TestFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class CategoryAttribute : Attribute
    {
        public string CategoryName { get; }

        public CategoryAttribute(string categoryName)
        {
            CategoryName = categoryName;
        }
    }
}