using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using SampleApp.Interfaces;
using SampleApp.Models;
using SampleApp.Services;
using TestFramework.Attributes;
using TestFramework.Context;

namespace Tests.Performance
{
    [TestClass]
    [Category("perf")]
    public class PaymentPerformanceTests : IUseSharedContext
    {
        public GlobalContext Context { get; set; }

        private const int BankLatencyMs = 500;

        private OrderService BuildSlowService()
        {
            var warehouse = new InventoryService();
            warehouse.AddStock("SLOW_ITEM", "Heavy Processing Item", 100m, 1000);
            return new OrderService(warehouse, new DelayedGateway(BankLatencyMs), new NotificationService());
        }

        private class DelayedGateway : IPaymentGateway
        {
            private readonly int _latency;
            public DelayedGateway(int latency) { _latency = latency; }

            public async Task<bool> ChargeAsync(string email, decimal amount)
            {
                await Task.Delay(_latency);
                return true;
            }
        }

        [TestMethod] public async Task ProcessOrder_1()  => await RunSingleOrder("User 1");
        [TestMethod] public async Task ProcessOrder_2()  => await RunSingleOrder("User 2");
        [TestMethod] public async Task ProcessOrder_3()  => await RunSingleOrder("User 3");
        [TestMethod] public async Task ProcessOrder_4()  => await RunSingleOrder("User 4");
        [TestMethod] public async Task ProcessOrder_5()  => await RunSingleOrder("User 5");
        [TestMethod] public async Task ProcessOrder_6()  => await RunSingleOrder("User 6");
        [TestMethod] public async Task ProcessOrder_7()  => await RunSingleOrder("User 7");
        [TestMethod] public async Task ProcessOrder_8()  => await RunSingleOrder("User 8");
        [TestMethod] public async Task ProcessOrder_9()  => await RunSingleOrder("User 9");
        [TestMethod] public async Task ProcessOrder_10() => await RunSingleOrder("User 10");

        private async Task RunSingleOrder(string name)
        {
            var svc   = BuildSlowService();
            var buyer = new User { Email = $"{name.Replace(" ", "")}@test.com" };
            var cart  = new List<OrderItem> { new OrderItem { Sku = "SLOW_ITEM", Quantity = 1 } };

            Console.WriteLine($"   -> {name}: Sending payment request to bank...");

            var sw = Stopwatch.StartNew();
            await svc.CheckoutAsync(buyer, cart);
            sw.Stop();

            Console.WriteLine($"   <- {name}: Payment approved in {sw.ElapsedMilliseconds}ms");
        }
    }
}
