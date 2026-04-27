using SampleApp.Exceptions;
using SampleApp.Models;
using SampleApp.Services;
using TestFramework;
using TestFramework.Attributes;
using TestFramework.Context;
using TestFramework.Fluent;

namespace Tests.Integration
{
    [TestClass]
    [Category("Integration")]
    [Category("Critical")]
    [Category("Transaction")]
    public class RollbackTests : IUseSharedContext
    {
        private OrderService _orders;
        private InventoryService _warehouse;
        private PaymentGateway _processor;
        private NotificationService _mailer;

        public GlobalContext Context { get; set; }

        private static class ScenarioBuilder
        {
            public static List<OrderItem> SingleItem(string sku, int qty)
                => new List<OrderItem> { new OrderItem { Sku = sku, Quantity = qty } };

            public static User RiskyCustomer()   => new User { Email = "fail@test.com", LoyaltyPoints = 100 };
            public static User PremiumCustomer() => new User { Email = "vip@test.com",  LoyaltyPoints = 500 };
        }

        [TestInitialize]
        public void Prepare()
        {
            _warehouse = new InventoryService();
            _processor = new PaymentGateway();
            _mailer    = new NotificationService();
            _orders    = new OrderService(_warehouse, _processor, _mailer);

            _warehouse.AddStock("RARE", "Rare Item", 2000m, 2);
            _warehouse.AddStock("GOLD", "Gold Bar",  6000m, 10);

            Context?.SetData("RollbackTestSetup", true);
        }

        [TestCleanup]
        public void Teardown()
        {
            _warehouse = null;
            _orders    = null;
        }

        [TestMethod]
        public async Task TestStockRollbackOnPaymentDecline()
        {
            var customer = ScenarioBuilder.RiskyCustomer();
            var cart     = ScenarioBuilder.SingleItem("RARE", 2);

            var result  = await _orders.CheckoutAsync(customer, cart);
            var product = _warehouse.GetProduct("RARE");

            var soft = new SoftAssert();
            soft.AreEqual(OrderStatus.Failed, result.Status,         "Order status mismatch");
            soft.AreEqual(2,                  product.StockQuantity, "Stock Quantity mismatch (should be restored)");
            soft.IsNotNull(result.Customer,                          "Customer ref should remain");

            soft.AssertAll();
        }

        [TestMethod]
        [Timeout(500)]
        public async Task TestRollbackOnPaymentException()
        {
            var customer = ScenarioBuilder.RiskyCustomer();
            var cart     = ScenarioBuilder.SingleItem("GOLD", 1);

            await Assert.ThrowsAsync<PaymentFailedException>(async () =>
            {
                await _orders.CheckoutAsync(customer, cart);
            });

            var product = _warehouse.GetProduct("GOLD");

            FluentCheck.That(product.StockQuantity)
                .ToBe(10)
                .And
                .BeGreaterThan(0);
        }

        [TestMethod]
        [TestCase(200, 4000, 200)]
        [TestCase(0, 4000, 0)]
        public async Task TestLoyaltyPointsRollback(int initialPts, int price, int expectedPts)
        {
            _warehouse.AddStock("TEMP", "Temp Item", (decimal)price, 1);

            var customer = new User { Email = "loyalty@test.com", LoyaltyPoints = initialPts };
            var cart     = ScenarioBuilder.SingleItem("TEMP", 1);

            var result = await _orders.CheckoutAsync(customer, cart);

            Assert.AreEqual(OrderStatus.Failed, result.Status,           "Order should fail");
            Assert.AreEqual(expectedPts,        customer.LoyaltyPoints,
                $"Loyalty points should be rolled back to {expectedPts}");
        }

        [TestMethod]
        public async Task TestComplexRollbackScenario()
        {
            _warehouse.AddStock("ITEM1", "I1", 1500m, 1);
            _warehouse.AddStock("ITEM2", "I2", 1500m, 1);

            var cart = new List<OrderItem>
            {
                new OrderItem { Sku = "ITEM1", Quantity = 1 },
                new OrderItem { Sku = "ITEM2", Quantity = 1 }
            };

            await _orders.CheckoutAsync(new User(), cart);

            VerifyRestored("ITEM1", 1);
            VerifyRestored("ITEM2", 1);
        }

        private void VerifyRestored(string sku, int expected)
        {
            var p = _warehouse.GetProduct(sku);
            Assert.AreEqual(expected, p.StockQuantity, $"Stock for {sku} not restored");
        }
    }
}
