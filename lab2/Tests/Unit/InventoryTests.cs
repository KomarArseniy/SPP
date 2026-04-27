using SampleApp.Services;
using TestFramework;
using TestFramework.Attributes;
using TestFramework.Context;
using TestFramework.Fluent;

namespace Tests.Unit
{
    [TestClass]
    [Category("Unit")]
    [Category("Inventory")]
    public class InventoryTests : IUseSharedContext
    {
        private InventoryService _store;

        public GlobalContext Context { get; set; }

        [TestInitialize]
        public void Prepare()
        {
            _store = new InventoryService();
            FillTestData(_store);
        }

        [TestCleanup]
        public void Teardown()
        {
            _store = null;
            Context?.SetData("LastTestTime", DateTime.Now);
        }

        private static void FillTestData(InventoryService svc)
        {
            svc.AddStock("ITEM-1", "Base Item",  100m, 10);
            svc.AddStock("ITEM-2", "Spare Part",  50m,  5);
            svc.AddStock("X-99",   "Rare Item",  999m,  1);
        }

        private static class StockCalc
        {
            public static int ReorderQty(int onHand, int target) => target - onHand;
        }

        [TestMethod]
        public void TestEqualityAndBoolean()
        {
            bool reserved = _store.TryReserve("ITEM-1", 5);
            Assert.IsTrue(reserved,                                    "Should be able to reserve 5 items");
            Assert.IsFalse(_store.TryReserve("ITEM-1", 100),          "Should not be able to reserve more than stock");

            var item = _store.GetProduct("ITEM-1");
            Assert.AreEqual(5,    item.StockQuantity, "Stock should be reduced to 5");
            Assert.AreNotEqual(0, item.StockQuantity, "Stock should not be zero");
        }

        [TestMethod]
        public void TestNullabilityAndReferences()
        {
            var found   = _store.GetProduct("ITEM-1");
            Assert.IsNotNull(found, "Product should exist");

            var missing = _store.GetProduct("NON-EXISTENT");
            Assert.IsNull(missing, "Product should not exist");

            var sameRef = _store.GetProduct("ITEM-1");
            Assert.AreSame(found, sameRef, "Should return the exact same object instance from dictionary");

            var freshObj = new SampleApp.Models.Product { Sku = "ITEM-1" };
            Assert.AreNotSame(found, freshObj, "Different instances should not be the same even if data matches");
        }

        [TestMethod]
        public void TestCollectionContains()
        {
            var skus = new List<string> { "ITEM-1", "ITEM-2", "X-99" };
            Assert.Contains(skus, "X-99", "Collection should contain rare item");
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
                _store.GetProduct("ITEM-1");
            }, "GetProduct should be safe");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestExpectedExceptionAttribute()
        {
            string badSku = null;
            if (badSku == null) throw new ArgumentNullException(nameof(badSku));
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
            var product = _store.GetProduct("ITEM-2");

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
            var soft    = new SoftAssert();
            var product = _store.GetProduct("ITEM-1");

            soft.IsNotNull(product,                   "Product Check");
            soft.AreEqual("ITEM-1", product.Sku,      "SKU Check");
            soft.IsTrue(product.Price > 0,            "Price Check");

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

            bool fail = false;
            if (fail)
                Assert.Fail("Something went terribly wrong manually");
        }
    }
}
