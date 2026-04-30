using System;
using System.Collections.Generic;
using TestFramework;
using TestFramework.Attributes;

namespace Tests.NewFeatures
{
    [TestClass]
    [Category("Yield")]
    public class YieldParameterTests
    {
        public static IEnumerable<object[]> GetMathData()
        {
            yield return new object[] { 10,  5,   15  };
            yield return new object[] { 20,  30,  50  };
            yield return new object[] { -1,  1,   0   };
            yield return new object[] { 100, 200, 300 };
        }

        [TestMethod]
        [MethodDataSource(nameof(GetMathData))]
        public void AdditionTest_YieldDriven(int a, int b, int expected)
        {
            Assert.AreEqual(expected, a + b);
        }

        public static IEnumerable<object[]> GetLoginData()
        {
            yield return new object[] { "admin", "12345",    true  };
            yield return new object[] { "guest", "password", false };
            yield return new object[] { "root",  "toor",     true  };
        }

        [TestMethod]
        [MethodDataSource(nameof(GetLoginData))]
        public void LoginValidation_YieldDriven(string user, string pass, bool shouldAccess)
        {
            bool actualAccess = pass.Length > 3;
            Assert.AreEqual(shouldAccess, actualAccess, $"Access mismatch for user '{user}'");
        }
    }
}
