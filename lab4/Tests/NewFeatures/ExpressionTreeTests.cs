using System;
using TestFramework;
using TestFramework.Attributes;

namespace Tests.NewFeatures
{
    [TestClass]
    [Category("ExpressionTree")]
    public class ExpressionTreeTests
    {
        // --- Тесты, которые ПРОХОДЯТ ---

        [TestMethod]
        public void SimpleComparison_Pass()
        {
            int score = 85;
            Assert.That(() => score > 50);
        }

        // --- Тесты, которые намеренно ПАДАЮТ, демонстрируя детальный вывод ---

        [TestMethod]
        public void SpeedLimit_Exceeded_Fail()
        {
            int speed = 120;
            int limit = 90;
            Assert.That(() => speed <= limit, "Speeding detected!");
        }

        [TestMethod]
        public void MathCalculation_WrongResult_Fail()
        {
            int a = 5, b = 10, c = 2, target = 100;
            Assert.That(() => (a + b) * c >= target, "Calculation error in business logic");
        }

        [TestMethod]
        public void LogicalOperators_AccessDenied_Fail()
        {
            bool isRegistered    = true;
            bool hasSubscription = false;
            bool isAdmin         = false;
            Assert.That(() => (isRegistered && hasSubscription) || isAdmin, "Access Denied: User has no rights");
        }

        [TestMethod]
        public void StringComparison_StatusMismatch_Fail()
        {
            string status   = "Pending";
            string expected = "Completed";
            Assert.That(() => status == expected, "Order status mismatch");
        }

        [TestMethod]
        public void DeepNesting_SumTooLow_Fail()
        {
            int x = 1, y = 2, z = 3, w = 4;
            Assert.That(() => ((x + y) + z) + w > 50, "Sum is too low");
        }
    }
}
