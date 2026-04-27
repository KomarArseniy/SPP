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
    [Category("OrderFlow")]
    public class OrderFlowTests : IUseSharedContext
    {
        private OrderService _orders;
        private InventoryService _warehouse;
        private PaymentGateway _processor;
        private NotificationService _mailer;

        public GlobalContext Context { get; set; }

        [TestInitialize]
        public void Prepare()
        {
            _warehouse = new InventoryService();
            _processor = new PaymentGateway();
            _mailer    = new NotificationService();
            _orders    = new OrderService(_warehouse, _processor, _mailer);

            _warehouse.AddStock("PHONE", "iPhone", 1000m, 10);
            _warehouse.AddStock("CASE",  "Case",     50m, 50);
        }

        [TestCleanup]
        public void Teardown()
        {
            _orders = null;
        }

        private User MakeUser(string email, int pts)
            => new User { Email = email, LoyaltyPoints = pts };

        [TestMethod]
        [TestCase(0, 2, 0, 10)]
        [TestCase(200, 2, 10, 190)]
        public async Task TestLoyaltyLogic(int startPts, int qty, int discountExp, int finalPtsExp)
        {
            var buyer = MakeUser("test@user.com", startPts);
            var cart  = new List<OrderItem> { new OrderItem { Sku = "CASE", Quantity = qty } };

            var order = await _orders.CheckoutAsync(buyer, cart);

            Assert.AreEqual(OrderStatus.Completed, order.Status,         "Order should be completed");
            Assert.AreEqual((decimal)discountExp,  order.DiscountApplied, "Discount mismatch");
            Assert.AreEqual(finalPtsExp,            buyer.LoyaltyPoints,   "Loyalty points mismatch");
        }

        [TestMethod]
        [DataSource("order_data.csv")]
        public async Task TestOrderTotal_FromCsv(string sku, string qtyStr, string totalStr)
        {
            int qty           = int.Parse(qtyStr);
            decimal expected  = decimal.Parse(totalStr);
            var buyer         = MakeUser($"csv_{Guid.NewGuid()}@user.com", 0);
            var cart          = new List<OrderItem> { new OrderItem { Sku = sku, Quantity = qty } };

            var order = await _orders.CheckoutAsync(buyer, cart);

            Assert.AreEqual(expected, order.TotalAmount, $"Total calculation wrong for {sku}");
        }

        [TestMethod]
        public async Task TestNotificationSent_Fluent()
        {
            string captured = null;
            _mailer.OnEmailSent += msg => captured = msg;

            var buyer = MakeUser("notify@me.com", 0);
            var cart  = new List<OrderItem> { new OrderItem { Sku = "CASE", Quantity = 1 } };

            var order = await _orders.CheckoutAsync(buyer, cart);

            FluentCheck.That(captured)
                .NotToBeNull()
                .And
                .Contain(order.Id.ToString());
        }

        [TestMethod]
        public async Task TestComplexOrderFlow_SoftAssert()
        {
            _warehouse.AddStock("LAST", "Last Item", 500m, 1);

            var buyer      = MakeUser("soft@user.com", 0);
            var cart       = new List<OrderItem> { new OrderItem { Sku = "LAST", Quantity = 1 } };
            bool emailSent = false;
            _mailer.OnEmailSent += _ => emailSent = true;

            var order = await _orders.CheckoutAsync(buyer, cart);

            var soft = new SoftAssert();
            soft.AreEqual(OrderStatus.Completed, order.Status,       "Status Check");
            soft.AreEqual(500m,                  order.TotalAmount,  "Amount Check");
            soft.IsTrue(emailSent,                                    "Email Check");

            var remaining = _warehouse.GetProduct("LAST");
            soft.AreEqual(0, remaining.StockQuantity, "Inventory Check");

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
