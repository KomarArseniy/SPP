using TestFramework;
using TestFramework.Attributes;
using TestFramework.Context;
using TestFramework.Fluent;
using SampleApp.Exceptions;
using SampleApp.Models;
using SampleApp.Services;

namespace Tests.Integration
{
    [TestClass]
    [Category("Integration")]
    [Category("Critical")]
    [Category("Transaction")]
    public class RollbackTests : IUseSharedContext
    {
        private OrderService _orderService;
        private InventoryService _inventory;
        private PaymentGateway _payment;
        private NotificationService _notification;

        public GlobalContext Context { get; set; }

        private static class OrderFactory
        {
            public static List<OrderItem> CreateItems(string sku, int quantity)
            {
                return new List<OrderItem>
                {
                    new OrderItem { Sku = sku, Quantity = quantity }
                };
            }

            public static User CreateRiskyUser() => new User { Email = "fail@test.com", LoyaltyPoints = 100 };
            public static User CreateVipUser() => new User { Email = "vip@test.com", LoyaltyPoints = 500 };
        }

        [TestInitialize]
        public void Setup()
        {
            _inventory = new InventoryService();
            _payment = new PaymentGateway();
            _notification = new NotificationService();
            _orderService = new OrderService(_inventory, _payment, _notification);

            _inventory.AddStock("RARE", "Rare Item", 2000m, 2);
            _inventory.AddStock("GOLD", "Gold Bar", 6000m, 10);

            if (Context != null) Context.SetData("RollbackTestSetup", true);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _inventory = null;
            _orderService = null;
        }

        [TestMethod]
        public async Task TestStockRollbackOnPaymentDecline()
        {
            var user = OrderFactory.CreateRiskyUser();
            var items = OrderFactory.CreateItems("RARE", 2);

            var order = await _orderService.CheckoutAsync(user, items);

            var soft = new SoftAssert();
            var product = _inventory.GetProduct("RARE");

            soft.AreEqual(OrderStatus.Failed, order.Status, "Order status mismatch");
            soft.AreEqual(2, product.StockQuantity, "Stock Quantity mismatch (should be restored)");
            soft.IsNotNull(order.Customer, "Customer ref should remain");

            soft.AssertAll();
        }

        [TestMethod]
        [Timeout(500)] 
        public async Task TestRollbackOnPaymentException()
        {
            var user = OrderFactory.CreateRiskyUser();
            var items = OrderFactory.CreateItems("GOLD", 1);

            await Assert.ThrowsAsync<PaymentFailedException>(async () =>
            {
                await _orderService.CheckoutAsync(user, items);
            });

            var product = _inventory.GetProduct("GOLD");

            FluentCheck.That(product.StockQuantity)
                .ToBe(10) 
                .And
                .BeGreaterThan(0);
        }

        [TestMethod]
        [TestCase(200, 4000, 200)]
        [TestCase(0, 4000, 0)]
        public async Task TestLoyaltyPointsRollback(int initialPoints, int purchaseAmount, int expectedPoints)
        {
            _inventory.AddStock("TEMP", "Temp Item", (decimal)purchaseAmount, 1);

            var user = new User { Email = "loyalty@test.com", LoyaltyPoints = initialPoints };
            var items = OrderFactory.CreateItems("TEMP", 1);

            var order = await _orderService.CheckoutAsync(user, items);

            Assert.AreEqual(OrderStatus.Failed, order.Status, "Order should fail");

            Assert.AreEqual(expectedPoints, user.LoyaltyPoints,
                $"Loyalty points should be rolled back to {expectedPoints}");
        }

        [TestMethod]
        public async Task TestComplexRollbackScenario()
        {
            _inventory.AddStock("ITEM1", "I1", 1500m, 1);
            _inventory.AddStock("ITEM2", "I2", 1500m, 1);

            var items = new List<OrderItem>
            {
                new OrderItem { Sku = "ITEM1", Quantity = 1 },
                new OrderItem { Sku = "ITEM2", Quantity = 1 }
            };

            await _orderService.CheckoutAsync(new User(), items);

            AssertStockRestored("ITEM1", 1);
            AssertStockRestored("ITEM2", 1);
        }

        private void AssertStockRestored(string sku, int expectedQty)
        {
            var p = _inventory.GetProduct(sku);
            Assert.AreEqual(expectedQty, p.StockQuantity, $"Stock for {sku} not restored");
        }
    }
}