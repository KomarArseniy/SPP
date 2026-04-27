using SampleApp.Exceptions;
using SampleApp.Models;
using SampleApp.Services;
using TestFramework;
using TestFramework.Attributes;
using TestFramework.Fluent;

namespace Tests.Unit
{
    [TestClass]
    [Category("Demo")]
    [Category("Negative")]
    public class DemonstrationTests
    {
        private InventoryService _stock;
        private PaymentGateway _billing;
        private NotificationService _alerts;
        private OrderService _orders;

        [TestInitialize]
        public void Prepare()
        {
            _stock   = new InventoryService();
            _billing = new PaymentGateway();
            _alerts  = new NotificationService();
            _orders  = new OrderService(_stock, _billing, _alerts);

            _stock.AddStock("DEMO_ITEM", "Buggy Item", 100m, 50);
        }

        [TestMethod]
        public void TestSoftAssert_MultipleFailures()
        {
            var checker  = new SoftAssert();
            int qty      = 50;
            string state = "Pending";

            checker.IsTrue(qty > 0,           "Stock check passed");
            checker.AreEqual(999, qty,         "Stock count mismatch");
            checker.AreEqual("Completed", state, "Status mismatch");

            checker.AssertAll();
        }

        [TestMethod]
        public void TestFluent_ChainFailure()
        {
            decimal amount = 100m;

            FluentCheck.That(amount)
                .BeGreaterThan(50)
                .And
                .BeLessThan(10);
        }

        [TestMethod]
        public async Task TestRealOrderCalculation_Fail()
        {
            var buyer = new User { Email = "fail@logic.com", LoyaltyPoints = 0 };
            var cart  = new List<OrderItem> { new OrderItem { Sku = "DEMO_ITEM", Quantity = 1 } };
            var order = await _orders.CheckoutAsync(buyer, cart);

            Assert.AreEqual(9999m, order.TotalAmount, "Critical error in pricing engine!");
        }

        [TestMethod]
        public async Task TestAppException_NegativePayment()
        {
            await _billing.ChargeAsync("hacker@test.com", -500m);
        }

        [TestMethod]
        [Timeout(1)]
        public async Task TestPaymentGateway_TooSlow()
        {
            await _billing.ChargeAsync("slow@user.com", 100m);
        }

        [TestMethod]
        [ExpectedException(typeof(PaymentFailedException))]
        public async Task TestBankLimit_NotReached()
        {
            await _billing.ChargeAsync("rich@user.com", 100m);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task TestInventory_MissingItem()
        {
            var buyer = new User { Email = "ghost@user.com" };
            var cart  = new List<OrderItem> { new OrderItem { Sku = "NON_EXISTENT_SKU", Quantity = 1 } };
            await _orders.CheckoutAsync(buyer, cart);
        }
    }
}
