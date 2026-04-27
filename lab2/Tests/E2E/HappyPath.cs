using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SampleApp.Models;
using SampleApp.Services;
using TestFramework.Attributes;
using TestFramework.Context;

namespace Tests.E2E
{
    [TestClass]
    [TestE2E]
    [Category("e2e")]
    public class SuccessfulPurchaseScenario : IUseSharedContext
    {
        public GlobalContext Context { get; set; }

        [ClassInitialize]
        public static void InitializeServices(GlobalContext ctx)
        {
            Console.WriteLine(">>> [Setup] Initializing Real Services...");

            var warehouse = new InventoryService();
            var billing   = new PaymentGateway();
            var alerts    = new NotificationService();
            var checkout  = new OrderService(warehouse, billing, alerts);

            warehouse.AddStock("LAPTOP", "Gaming Laptop", 100m, 10);

            ctx.SetData("Service_Inventory", warehouse);
            ctx.SetData("Service_Order",     checkout);
            ctx.SetData("Sku",               "LAPTOP");
            ctx.SetData("InitialStock",       10);
        }

        [TestMethod]
        [Order(1)]
        public void CreateCustomer()
        {
            Console.WriteLine("[Step 1] Creating Customer...");
            var buyer = new User
            {
                Name          = "John Doe",
                Email         = "john@example.com",
                LoyaltyPoints = 0
            };
            Context.SetData("User", buyer);
        }

        [TestMethod]
        [Order(2)]
        public async Task ProcessCheckout()
        {
            Console.WriteLine("[Step 2] Processing Checkout...");

            var orderSvc = Context.GetData<OrderService>("Service_Order");
            var buyer    = Context.GetData<User>("User");
            var sku      = Context.GetData<string>("Sku");

            var cart = new List<OrderItem>
            {
                new OrderItem { Sku = sku, Quantity = 2 }
            };

            var placed = await orderSvc.CheckoutAsync(buyer, cart);
            Context.SetData("ResultOrder", placed);
        }

        [TestMethod]
        [Order(3)]
        public void VerifyOrderStatus()
        {
            Console.WriteLine("[Step 3] Verifying Order Status...");
            var order = Context.GetData<Order>("ResultOrder");

            if (order.Status != OrderStatus.Completed)
                throw new Exception($"Expected Completed, but got {order.Status}");

            if (order.TotalAmount != 200m)
                throw new Exception($"Expected Total 200, but got {order.TotalAmount}");
        }

        [TestMethod]
        [Order(4)]
        public void VerifyInventoryDecreased()
        {
            Console.WriteLine("[Step 4] Verifying Inventory...");

            var warehouse = Context.GetData<InventoryService>("Service_Inventory");
            var sku       = Context.GetData<string>("Sku");
            var product   = warehouse.GetProduct(sku);

            if (product.StockQuantity != 8)
                throw new Exception($"Stock error! Expected 8, got {product.StockQuantity}");
        }

        [TestMethod]
        [Order(5)]
        public void VerifyLoyaltyPointsAdded()
        {
            Console.WriteLine("[Step 5] Verifying Loyalty Points...");
            var buyer = Context.GetData<User>("User");

            if (buyer.LoyaltyPoints != 20)
                throw new Exception($"Loyalty error! Expected 20 points, got {buyer.LoyaltyPoints}");
        }
    }
}
