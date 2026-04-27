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
    public class LoyaltyDiscountScenario : IUseSharedContext
    {
        public GlobalContext Context { get; set; }

        [ClassInitialize]
        public static void InitializeServices(GlobalContext ctx)
        {
            var warehouse = new InventoryService();
            warehouse.AddStock("PHONE", "Smartphone", 500m, 10);

            var orderSvc = new OrderService(warehouse, new PaymentGateway(), new NotificationService());

            ctx.SetData("Service_Order", orderSvc);
            ctx.SetData("Sku",           "PHONE");
        }

        [TestMethod]
        [Order(1)]
        public async Task PurchaseWithLoyaltyPoints()
        {
            Console.WriteLine("[Step 1] User with 150 points buys item...");

            var orderSvc = Context.GetData<OrderService>("Service_Order");
            var sku      = Context.GetData<string>("Sku");

            var buyer = new User { LoyaltyPoints = 150 };
            Context.SetData("User", buyer);

            var cart  = new List<OrderItem> { new OrderItem { Sku = sku, Quantity = 1 } };
            var order = await orderSvc.CheckoutAsync(buyer, cart);

            Context.SetData("ResultOrder", order);
        }

        [TestMethod]
        [Order(2)]
        public void VerifyDiscountApplied()
        {
            var order = Context.GetData<Order>("ResultOrder");

            decimal wantedDiscount = 50m;
            decimal wantedTotal    = 450m;

            Console.WriteLine($"[Step 2] Checking Discount: {order.DiscountApplied}");

            if (order.DiscountApplied != wantedDiscount)
                throw new Exception($"Discount error! Expected {wantedDiscount}, got {order.DiscountApplied}");

            if (order.TotalAmount != wantedTotal)
                throw new Exception($"Total amount error! Expected {wantedTotal}, got {order.TotalAmount}");
        }

        [TestMethod]
        [Order(3)]
        public void VerifyPointsDeducted()
        {
            var buyer = Context.GetData<User>("User");

            Console.WriteLine($"[Step 3] Checking Points: {buyer.LoyaltyPoints}");

            if (buyer.LoyaltyPoints != 140)
                throw new Exception($"Points error! Expected 140, got {buyer.LoyaltyPoints}");
        }
    }
}
