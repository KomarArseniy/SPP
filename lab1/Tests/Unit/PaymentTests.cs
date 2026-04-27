using TestFramework;
using TestFramework.Attributes;
using TestFramework.Context;
using TestFramework.Fluent;
using SampleApp.Exceptions;
using SampleApp.Services;

namespace Tests.Unit
{
    [TestClass]
    [Category("Unit")]
    [Category("Payment")]
    public class PaymentTests : IUseSharedContext
    {
        private PaymentGateway _gateway;

        public GlobalContext Context { get; set; }


        [TestInitialize]
        public void Setup()
        {
            _gateway = new PaymentGateway();

            if (Context != null)
            {
                Context.SetData("PaymentTestStarted", DateTime.Now);
            }
        }

        [TestCleanup]
        public void Teardown()
        {
            _gateway = null;
        }

        private static class TestDataGenerator
        {
            public static string GetValidEmail() => "user@example.com";
            public static decimal GetSmallAmount() => 100m;
            public static decimal GetDeclinedAmount() => 2500m;
            public static decimal GetCriticalAmount() => 6000m;
        }

        private void AssertGatewayIsHealthy()
        {
            Assert.IsNotNull(_gateway, "Gateway should be initialized");
        }

        [TestMethod]
        [TestCase(500, true)]
        [TestCase(2000, false)]
        public async Task TestPaymentBoundaries(int amount, bool expectedResult)
        {
            bool result = await _gateway.ChargeAsync(TestDataGenerator.GetValidEmail(), amount);

            Assert.AreEqual(expectedResult, result, $"Result for amount {amount} was incorrect");
        }

        [TestMethod]
        public async Task TestPaymentThrowsException_Imperative()
        {
            var ex = await Assert.ThrowsAsync<PaymentFailedException>(async () =>
            {
                await _gateway.ChargeAsync("user@test.com", TestDataGenerator.GetCriticalAmount());
            });

            FluentCheck.That(ex.Message)
                .NotToBeNull()
                .And
                .ToBe("Payment failed: Bank limit exceeded");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))] 
        public async Task TestNegativeAmount_Attribute()
        {
            await _gateway.ChargeAsync("user@test.com", -100);
        }

        [TestMethod]
        [Timeout(200)] 
        public async Task TestPaymentPerformance()
        {
            await _gateway.ChargeAsync("fast@test.com", 100);
        }

        [TestMethod]
        public async Task TestPaymentSoftAsserts()
        {
            var soft = new SoftAssert();

            bool result = await _gateway.ChargeAsync("soft@test.com", 500);

            soft.IsNotNull(_gateway, "Gateway instance check");
            soft.IsTrue(result, "Payment result check");

            soft.IsNotNull(Context, "Context injection check");

            soft.AssertAll();
        }

        [TestMethod]
        public void TestGatewayInstances()
        {
            var gateway1 = _gateway;
            var gateway2 = _gateway;
            var newGateway = new PaymentGateway();

            Assert.AreSame(gateway1, gateway2, "References should point to same object");
            Assert.AreNotSame(gateway1, newGateway, "New instance should be different");
        }

        [TestMethod]
        [Ignore("Skipping integration test with real bank")]
        public void TestRealBankConnection()
        {
            Assert.Fail("This test should be skipped and never fail");
        }
    }
}