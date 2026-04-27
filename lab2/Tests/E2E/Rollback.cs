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
    public class PaymentFailureRollbackScenario : IUseSharedContext
    {
        public GlobalContext Context { get; set; }

        [ClassInitialize]
        public static void InitializeServices(GlobalContext ctx)
        {
            var warehouse = new InventoryService();
            warehouse.AddStock("GOLD_BAR", "Gold", 2000m, 5);

            var orderSvc = new OrderService(warehouse, new PaymentGateway(), new NotificationService());

            ctx.SetData("Service_Inventory", warehouse);
            ctx.SetData("Service_Order",     orderSvc);
            ctx.SetData("Sku",               "GOLD_BAR");
        }

        [TestMethod]
        [Order(1)]
        public async Task AttemptExpensivePurchase()
        {
            Console.WriteLine("[Step 1] Attempting to buy expensive item (> $1000)...");

            var orderSvc = Context.GetData<OrderService>("Service_Order");
            var sku      = Context.GetData<string>("Sku");

            var buyer = new User { Name = "Rich Guy", Email = "rich@test.com", LoyaltyPoints = 50 };
            Context.SetData("User", buyer);

            var cart  = new List<OrderItem> { new OrderItem { Sku = sku, Quantity = 1 } };
            var order = await orderSvc.CheckoutAsync(buyer, cart);

            Context.SetData("ResultOrder", order);
        }

        [TestMethod]
        [Order(2)]
        public void VerifyOrderFailed()
        {
            var order = Context.GetData<Order>("ResultOrder");
            Console.WriteLine($"[Step 2] Order Status: {order.Status}");

            if (order.Status != OrderStatus.Failed)
                throw new Exception("Order should have failed due to payment limit!");
        }

        [TestMethod]
        [Order(3)]
        public void VerifyStockRolledBack()
        {
            Console.WriteLine("[Step 3] Verifying stock was returned...");

            var warehouse = Context.GetData<InventoryService>("Service_Inventory");
            var sku       = Context.GetData<string>("Sku");
            var product   = warehouse.GetProduct(sku);

            if (product.StockQuantity != 5)
                throw new Exception($"Rollback failed! Expected 5 items, found {product.StockQuantity}");
        }

        [TestMethod]
        [Order(4)]
        public void VerifyPointsNotChanged()
        {
            var buyer = Context.GetData<User>("User");

            if (buyer.LoyaltyPoints != 50)
                throw new Exception($"Loyalty points corrupted! Expected 50, got {buyer.LoyaltyPoints}");
        }
    }
}
