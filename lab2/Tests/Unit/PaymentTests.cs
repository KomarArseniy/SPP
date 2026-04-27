using SampleApp.Exceptions;
using SampleApp.Services;
using TestFramework;
using TestFramework.Attributes;
using TestFramework.Context;
using TestFramework.Fluent;

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
        public void Prepare()
        {
            _gateway = new PaymentGateway();
            Context?.SetData("PaymentTestStarted", DateTime.Now);
        }

        [TestCleanup]
        public void Teardown()
        {
            _gateway = null;
        }

        private static class Samples
        {
            public static string ValidEmail()      => "user@example.com";
            public static decimal SmallCharge()    => 100m;
            public static decimal DeclinedCharge() => 2500m;
            public static decimal ExceedingCharge() => 6000m;
        }

        [TestMethod]
        [TestCase(500, true)]
        [TestCase(2000, false)]
        public async Task TestPaymentBoundaries(int charge, bool expectedOutcome)
        {
            bool outcome = await _gateway.ChargeAsync(Samples.ValidEmail(), charge);
            Assert.AreEqual(expectedOutcome, outcome, $"Result for amount {charge} was incorrect");
        }

        [TestMethod]
        public async Task TestPaymentThrowsException_Imperative()
        {
            var ex = await Assert.ThrowsAsync<PaymentFailedException>(async () =>
            {
                await _gateway.ChargeAsync("user@test.com", Samples.ExceedingCharge());
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
            var soft    = new SoftAssert();
            bool outcome = await _gateway.ChargeAsync("soft@test.com", 500);

            soft.IsNotNull(_gateway, "Gateway instance check");
            soft.IsTrue(outcome,     "Payment result check");
            soft.IsNotNull(Context,  "Context injection check");

            soft.AssertAll();
        }

        [TestMethod]
        public void TestGatewayInstances()
        {
            var ref1  = _gateway;
            var ref2  = _gateway;
            var fresh = new PaymentGateway();

            Assert.AreSame(ref1, ref2,   "References should point to same object");
            Assert.AreNotSame(ref1, fresh, "New instance should be different");
        }

        [TestMethod]
        [Ignore("Skipping integration test with real bank")]
        public void TestRealBankConnection()
        {
            Assert.Fail("This test should be skipped and never fail");
        }
    }
}
