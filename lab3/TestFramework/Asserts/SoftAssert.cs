using System;
using System.Collections.Generic;
using System.Text;
using TestFramework.Exceptions;

namespace TestFramework
{
    public class SoftAssert
    {
        private readonly List<string> _errors = new List<string>();

        public void AreEqual(object expected, object actual, string message = "Values not equal")
        {
            if (!Equals(expected, actual))
            {
                _errors.Add($"{message}. Expected: {expected}, Actual: {actual}");
            }
        }
        public void IsTrue(bool condition, string message = "Condition is False")
        {
            if (!condition)
            {
                _errors.Add(message);
            }
        }

        public void IsNotNull(object obj, string message = "Object is null")
        {
            if (obj == null)
            {
                _errors.Add(message);
            }
        }

        public void AssertAll()
        {
            if (_errors.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Multiple failures in SoftAssert:");
                foreach (var err in _errors)
                {
                    sb.AppendLine($"- {err}");
                }
                throw new TestFailedException(sb.ToString());
            }
        }
    }
}