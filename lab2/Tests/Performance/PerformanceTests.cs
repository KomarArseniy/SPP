using System;
using System.Threading.Tasks;
using TestFramework.Attributes;

namespace Tests.Performance
{
    [TestClass]
    [Category("perf")]
    public class LoadSimulationTests
    {
        private const int WorkDurationMs = 2000;

        [TestMethod]
        public async Task HeavyOperation_1()
        {
            Console.WriteLine("   -> Task 1 started...");
            await Task.Delay(WorkDurationMs);
            Console.WriteLine("   <- Task 1 finished.");
        }

        [TestMethod]
        public async Task HeavyOperation_2()
        {
            Console.WriteLine("   -> Task 2 started...");
            await Task.Delay(WorkDurationMs);
            Console.WriteLine("   <- Task 2 finished.");
        }

        [TestMethod]
        public async Task HeavyOperation_3()
        {
            Console.WriteLine("   -> Task 3 started...");
            await Task.Delay(WorkDurationMs);
            Console.WriteLine("   <- Task 3 finished.");
        }

        [TestMethod]
        public async Task HeavyOperation_4()
        {
            Console.WriteLine("   -> Task 4 started...");
            await Task.Delay(WorkDurationMs);
            Console.WriteLine("   <- Task 4 finished.");
        }

        [TestMethod]
        public async Task HeavyOperation_5()
        {
            Console.WriteLine("   -> Task 5 started...");
            await Task.Delay(WorkDurationMs);
            Console.WriteLine("   <- Task 5 finished.");
        }
    }
}
