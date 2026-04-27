using System;

namespace TestFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class TestCaseAttribute : Attribute
    {
        public object[] Arguments { get; }

        public TestCaseAttribute(params object[] args)
        {
            Arguments = args;
        }
    }
}