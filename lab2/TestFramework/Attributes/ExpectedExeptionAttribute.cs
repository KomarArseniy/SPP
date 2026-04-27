using System;

namespace TestFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ExpectedExceptionAttribute : Attribute
    {
        public Type ExceptionType { get; }

        public ExpectedExceptionAttribute(Type exceptionType)
        {
            if (!typeof(Exception).IsAssignableFrom(exceptionType))
            {
                throw new ArgumentException("Type must invoke Exception", nameof(exceptionType));
            }
            ExceptionType = exceptionType;
        }
    }
}