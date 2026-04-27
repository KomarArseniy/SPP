using TestFramework;
using TestFramework.Attributes;
using TestFramework.Fluent; 
using SampleApp.Services;
using SampleApp.Models;
using SampleApp.Exceptions;

namespace Tests.Unit
{
    [TestClass]
    [Category("Demo")]
    [Category("Negative")] 
    public class DemonstrationTests
    {
        private InventoryService _inventory;
        private PaymentGateway _payment;
        private NotificationService _notification;
        private OrderService _orderService;

        [TestInitialize]
        public void Setup()
        {
            _inventory = new InventoryService();
            _payment = new PaymentGateway();
            _notification = new NotificationService();
            _orderService = new OrderService(_inventory, _payment, _notification);

            _inventory.AddStock("DEMO_ITEM", "Buggy Item", 100m, 50);
        }

        [TestMethod]
        public void TestSoftAssert_MultipleFailures()
        {
            var soft = new SoftAssert();
            int currentStock = 50;
            string status = "Pending";

            soft.IsTrue(currentStock > 0, "Stock check passed");

            soft.AreEqual(999, currentStock, "Stock count mismatch");

            soft.AreEqual("Completed", status, "Status mismatch");

            soft.AssertAll();
        }


        [TestMethod]
        public void TestFluent_ChainFailure()
        {
            decimal price = 100m;

            FluentCheck.That(price)
                .BeGreaterThan(50) 
                .And
                .BeLessThan(10);  
        }

        [TestMethod]
        public async Task TestRealOrderCalculation_Fail()
        {
            var user = new User { Email = "fail@logic.com", LoyaltyPoints = 0 };
            var items = new List<OrderItem> { new OrderItem { Sku = "DEMO_ITEM", Quantity = 1 } };
            var order = await _orderService.CheckoutAsync(user, items);

            Assert.AreEqual(9999m, order.TotalAmount, "Critical error in pricing engine!");
        }

        [TestMethod]
        public async Task TestAppException_NegativePayment()
        {
            await _payment.ChargeAsync("hacker@test.com", -500m);
        }

        [TestMethod]
        [Timeout(1)]
        public async Task TestPaymentGateway_TooSlow()
        {
            await _payment.ChargeAsync("slow@user.com", 100m);
        }

        [TestMethod]
        [ExpectedException(typeof(PaymentFailedException))]
        public async Task TestBankLimit_NotReached()
        {
            await _payment.ChargeAsync("rich@user.com", 100m);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task TestInventory_MissingItem()
        {
            var user = new User { Email = "ghost@user.com" };
            var items = new List<OrderItem> { new OrderItem { Sku = "NON_EXISTENT_SKU", Quantity = 1 } };
            await _orderService.CheckoutAsync(user, items);
        }
    }
}