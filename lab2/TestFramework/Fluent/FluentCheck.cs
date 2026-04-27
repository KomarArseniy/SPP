using System;
using TestFramework.Exceptions;

namespace TestFramework.Fluent
{
    public static class FluentCheck
    {
        public static FluentCheck<T> That<T>(T actual)
        {
            return new FluentCheck<T>(actual);
        }
    }

    public class FluentCheck<T>
    {
        private readonly T _actual;

        public FluentCheck(T actual)
        {
            _actual = actual;
        }

        public FluentCheck<T> And => this;

        public FluentCheck<T> ToBe(T expected)
        {
            if (!Equals(_actual, expected))
                throw new TestFailedException($"Expected: {expected}, but found: {_actual}");
            return this;
        }

        public FluentCheck<T> NotToBe(T notExpected)
        {
            if (Equals(_actual, notExpected))
                throw new TestFailedException($"Value should not be {notExpected}");
            return this;
        }

        public FluentCheck<T> ToBeNull()
        {
            if (_actual != null)
                throw new TestFailedException($"Expected null, but found: {_actual}");
            return this;
        }

        public FluentCheck<T> NotToBeNull()
        {
            if (_actual == null)
                throw new TestFailedException("Expected object not to be null.");
            return this;
        }

        public FluentCheck<T> BeGreaterThan(double value)
        {
            if (_actual == null) throw new TestFailedException("Cannot compare null value");

            try
            {
                double actualVal = Convert.ToDouble(_actual);
                if (actualVal <= value)
                    throw new TestFailedException($"Expected value to be > {value}, but was {actualVal}");
            }
            catch (InvalidCastException)
            {
                throw new TestFailedException($"Value {_actual} is not a number and cannot be compared.");
            }

            return this;
        }

        public FluentCheck<T> BeLessThan(double value)
        {
            if (_actual == null) throw new TestFailedException("Cannot compare null value");

            try
            {
                double actualVal = Convert.ToDouble(_actual);
                if (actualVal >= value)
                    throw new TestFailedException($"Expected value to be < {value}, but was {actualVal}");
            }
            catch (InvalidCastException)
            {
                throw new TestFailedException($"Value {_actual} is not a number and cannot be compared.");
            }

            return this;
        }

        public FluentCheck<T> Contain(string substring)
        {
            if (_actual == null) throw new TestFailedException("Value is null");

            string actualStr = _actual.ToString();
            if (!actualStr.Contains(substring))
                throw new TestFailedException($"Expected string to contain '{substring}', but was '{actualStr}'");

            return this;
        }
    }
}