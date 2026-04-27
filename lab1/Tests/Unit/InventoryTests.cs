using TestFramework; 
using TestFramework.Attributes; 
using TestFramework.Context; 
using TestFramework.Fluent; 
using SampleApp.Services;

namespace Tests.Unit
{
    [TestClass]
    [Category("Unit")]
    [Category("Inventory")]
    public class InventoryTests : IUseSharedContext
    {
        private InventoryService _inventory;

        public GlobalContext Context { get; set; }

        [TestInitialize]
        public void Setup()
        {
            _inventory = new InventoryService();
            SeedInitialData(_inventory);
        }

        [TestCleanup]
        public void Teardown()
        {
            _inventory = null;
            if (Context != null) Context.SetData("LastTestTime", DateTime.Now);
        }

        private void SeedInitialData(InventoryService service)
        {
            service.AddStock("ITEM-1", "Base Item", 100m, 10);
            service.AddStock("ITEM-2", "Spare Part", 50m, 5);
            service.AddStock("X-99", "Rare Item", 999m, 1);
        }

        private class StockCalculator
        {
            public static int CalculateRestock(int current, int target) => target - current;
        }

        [TestMethod]
        public void TestEqualityAndBoolean()
        {
            bool reserveResult = _inventory.TryReserve("ITEM-1", 5);
            Assert.IsTrue(reserveResult, "Should be able to reserve 5 items");
            Assert.IsFalse(_inventory.TryReserve("ITEM-1", 100), "Should not be able to reserve more than stock");

            var product = _inventory.GetProduct("ITEM-1");
            Assert.AreEqual(5, product.StockQuantity, "Stock should be reduced to 5");
            Assert.AreNotEqual(0, product.StockQuantity, "Stock should not be zero");
        }

        [TestMethod]
        public void TestNullabilityAndReferences()
        {
            var p1 = _inventory.GetProduct("ITEM-1");
            Assert.IsNotNull(p1, "Product should exist");

            var p2 = _inventory.GetProduct("NON-EXISTENT");
            Assert.IsNull(p2, "Product should not exist");

            var p1_ref = _inventory.GetProduct("ITEM-1");
            Assert.AreSame(p1, p1_ref, "Should return the exact same object instance from dictionary");

            var newObj = new SampleApp.Models.Product { Sku = "ITEM-1" };
            Assert.AreNotSame(p1, newObj, "Different instances should not be the same even if data matches");
        }

        [TestMethod]
        public void TestCollectionContains()
        {
            List<string> skuList = new List<string> { "ITEM-1", "ITEM-2", "X-99" };

            Assert.Contains(skuList, "X-99", "Collection should contain rare item");
        }

        [TestMethod]
        public void TestExceptionsAsserts()
        {
            Assert.Throws<Exception>(() =>
            {
                throw new InvalidOperationException("Test error");
            }, "Should throw specific exception");

            Assert.DoesNotThrow(() =>
            {
                _inventory.GetProduct("ITEM-1");
            }, "GetProduct should be safe");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))] 
        public void TestExpectedExceptionAttribute()
        {
            string invalidSku = null;
            if (invalidSku == null) throw new ArgumentNullException(nameof(invalidSku));
        }

        [TestMethod]
        public async Task TestAsyncOperations()
        {
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await Task.Delay(10);
                throw new TaskCanceledException("Async fail");
            });

            await Task.Delay(50);
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestFluentStyle()
        {
            var product = _inventory.GetProduct("ITEM-2");

            FluentCheck.That(product)
                .NotToBeNull()
                .And
                .NotToBe(null); 

            FluentCheck.That(product.Price)
                .BeGreaterThan(40)
                .And
                .BeLessThan(60);

            FluentCheck.That(product.StockQuantity).ToBe(5);
        }

        [TestMethod]
        public void TestSoftAsserts()
        {
            var soft = new SoftAssert();
            var product = _inventory.GetProduct("ITEM-1");

            soft.IsNotNull(product, "Product Check");
            soft.AreEqual("ITEM-1", product.Sku, "SKU Check");
            soft.IsTrue(product.Price > 0, "Price Check");

            soft.AssertAll();
        }

        [TestMethod]
        [Ignore("Demonstration of skipped test")] 
        public void TestIgnoredMethod()
        {
            Assert.Fail("This test should never run");
        }

        [TestMethod]
        [Timeout(200)] 
        public async Task TestWithTimeout()
        {
            await Task.Delay(100); 
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void TestManualFailAndContext()
        {
            Assert.IsNotNull(Context, "Shared Context should be injected by Runner");
            Context.SetData("InventoryTested", true);

            bool catastrophe = false;
            if (catastrophe)
            {
                Assert.Fail("Something went terribly wrong manually");
            }
        }
    }
}