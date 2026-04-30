using System;

namespace TestFramework.Exceptions
{
    public class TestFailedException : Exception
    {
        public TestFailedException(string message) : base(message)
        {
        }
    }
}