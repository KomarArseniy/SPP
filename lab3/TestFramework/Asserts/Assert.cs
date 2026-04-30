using System;
using TestFramework.Exceptions;

namespace TestFramework
{
    public static class Assert
    {
        public static void AreEqual(object expected, object actual, string message = "Values are not equal")
        {
            if (!Equals(expected, actual))
            {
                throw new TestFailedException($"{message}. Expected: {expected}, Actual: {actual}");
            }
        }

        public static void AreNotEqual(object notExpected, object actual, string message = "Values are equal but shouldn't be")
        {
            if (Equals(notExpected, actual))
            {
                throw new TestFailedException($"{message}. Value {notExpected} was not expected.");
            }
        }

        public static void IsTrue(bool condition, string message = "Condition is False")
        {
            if (!condition)
            {
                throw new TestFailedException(message);
            }
        }

        public static void IsFalse(bool condition, string message = "Condition is True")
        {
            if (condition)
            {
                throw new TestFailedException(message);
            }
        }

        public static void IsNull(object obj, string message = "Object is not null")
        {
            if (obj != null)
            {
                throw new TestFailedException(message);
            }
        }

        public static void IsNotNull(object obj, string message = "Object is null")
        {
            if (obj == null)
            {
                throw new TestFailedException(message);
            }
        }

        public static void AreSame(object expected, object actual, string message = "References are not the same")
        {
            if (!ReferenceEquals(expected, actual))
            {
                throw new TestFailedException(message);
            }
        }

        public static void AreNotSame(object notExpected, object actual, string message = "References are the same")
        {
            if (ReferenceEquals(notExpected, actual))
            {
                throw new TestFailedException(message);
            }
        }

        public static T Throws<T>(Action action, string message = "Expected exception was not thrown") where T : Exception
        {
            try
            {
                action();
            }
            catch (T ex)
            {
                return ex; 
            }
            catch (Exception ex)
            {
                throw new TestFailedException($"Wrong exception type thrown. Expected: {typeof(T).Name}, Actual: {ex.GetType().Name}");
            }

            throw new TestFailedException(message);
        }

        public static void DoesNotThrow(Action action, string message = "Exception was thrown")
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                throw new TestFailedException($"{message}. Exception: {ex.GetType().Name} - {ex.Message}");
            }
        }

        public static void Fail(string message = "Test failed manually")
        {
            throw new TestFailedException(message);
        }

        public static async Task<T> ThrowsAsync<T>(Func<Task> action, string message = "Expected async exception was not thrown")
    where T : Exception
        {
            try
            {
                await action(); 
            }
            catch (T ex)
            {
                return ex; 
            }
            catch (Exception ex)
            {
                throw new TestFailedException($"Wrong exception type in async method. Expected: {typeof(T).Name}, Actual: {ex.GetType().Name}");
            }

            throw new TestFailedException(message);
        }

        public static void Contains<T>(IEnumerable<T> collection, T expected, string message = "Collection does not contain expected item")
        {
            bool found = false;
            foreach (var item in collection)
            {
                if (Equals(item, expected))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                throw new TestFailedException($"{message}. Item {expected} not found.");
            }
        }
    }
}