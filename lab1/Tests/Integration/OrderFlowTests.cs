using TestFramework;
using TestFramework.Attributes;
using TestFramework.Context;
using TestFramework.Fluent;
using SampleApp.Models;
using SampleApp.Services;

namespace Tests.Integration
{
    [TestClass]
    [Category("Integration")]
    [Category("OrderFlow")]
    public class OrderFlowTests : IUseSharedContext
    {
        private OrderService _orderService;
        private InventoryService _inventory;
        private PaymentGateway _payment;
        private NotificationService _notification;

        public GlobalContext Context { get; set; }

        [TestInitialize]
        public void Setup()
        {
            _inventory = new InventoryService();
            _payment = new PaymentGateway();
            _notification = new NotificationService();
            _orderService = new OrderService(_inventory, _payment, _notification);

            _inventory.AddStock("PHONE", "iPhone", 1000m, 10);
            _inventory.AddStock("CASE", "Case", 50m, 50);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _orderService = null;
        }

        private User CreateUser(string email, int points)
        {
            return new User { Email = email, LoyaltyPoints = points };
        }

        [TestMethod]
        [TestCase(0, 2, 0, 10)]
        [TestCase(200, 2, 10, 190)]
        public async Task TestLoyaltyLogic(int initialPoints, int quantity, int expectedDiscount, int expectedFinalPoints)
        {
            var user = CreateUser("test@user.com", initialPoints);
            var items = new List<OrderItem>
            {
                new OrderItem { Sku = "CASE", Quantity = quantity }
            };

            var order = await _orderService.CheckoutAsync(user, items);

            Assert.AreEqual(OrderStatus.Completed, order.Status, "Order should be completed");
            Assert.AreEqual((decimal)expectedDiscount, order.DiscountApplied, "Discount mismatch");
            Assert.AreEqual(expectedFinalPoints, user.LoyaltyPoints, "Loyalty points mismatch");
        }

        [TestMethod]
        [DataSource("order_data.csv")]
        public async Task TestOrderTotal_FromCsv(string sku, string quantityStr, string expectedTotalStr)
        {
            int quantity = int.Parse(quantityStr);
            decimal expectedTotal = decimal.Parse(expectedTotalStr);
            var user = CreateUser($"csv_{Guid.NewGuid()}@user.com", 0);
            var items = new List<OrderItem> { new OrderItem { Sku = sku, Quantity = quantity } };

            var order = await _orderService.CheckoutAsync(user, items);

            Assert.AreEqual(expectedTotal, order.TotalAmount, $"Total calculation wrong for {sku}");
        }

        [TestMethod]
        public async Task TestNotificationSent_Fluent()
        {
            string sentMessage = null;
            _notification.OnEmailSent += (msg) => sentMessage = msg;

            var user = CreateUser("notify@me.com", 0);
            var items = new List<OrderItem> { new OrderItem { Sku = "CASE", Quantity = 1 } };

            var order = await _orderService.CheckoutAsync(user, items);

            FluentCheck.That(sentMessage)
                .NotToBeNull()
                .And
                .Contain(order.Id.ToString());
        }

        [TestMethod]
        public async Task TestComplexOrderFlow_SoftAssert()
        {
            _inventory.AddStock("LAST", "Last Item", 500m, 1);

            var user = CreateUser("soft@user.com", 0);
            var items = new List<OrderItem> { new OrderItem { Sku = "LAST", Quantity = 1 } };
            bool emailFired = false;
            _notification.OnEmailSent += (_) => emailFired = true;

            var order = await _orderService.CheckoutAsync(user, items);

            var soft = new SoftAssert();

            soft.AreEqual(OrderStatus.Completed, order.Status, "Status Check");
            soft.AreEqual(500m, order.TotalAmount, "Amount Check");
            soft.IsTrue(emailFired, "Email Check");

            var product = _inventory.GetProduct("LAST");
            soft.AreEqual(0, product.StockQuantity, "Inventory Check");

            soft.AssertAll();
        }

        [TestMethod]
        [Ignore("Feature pending implementation")]
        public async Task TestPreOrderLogic()
        {
            await Task.Delay(1);
            Assert.Fail("Should not run");
        }
    }
}