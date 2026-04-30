using System;
using System.Threading;
using TestFramework.Attributes;

namespace Tests
{
    [TestClass]
    [Category("LoadTest")]
    public class ThreadPoolLoadTests
    {
        // --- SCENARIO 1: HIGH-INTENSITY BURST (50 Tests) ---
        [TestMethod]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        [TestCase(12)]
        [TestCase(13)]
        [TestCase(14)]
        [TestCase(15)]
        [TestCase(16)]
        [TestCase(17)]
        [TestCase(18)]
        [TestCase(19)]
        [TestCase(20)]
        [TestCase(21)]
        [TestCase(22)]
        [TestCase(23)]
        [TestCase(24)]
        [TestCase(25)]
        [TestCase(26)]
        [TestCase(27)]
        [TestCase(28)]
        [TestCase(29)]
        [TestCase(30)]
        [TestCase(31)]
        [TestCase(32)]
        [TestCase(33)]
        [TestCase(34)]
        [TestCase(35)]
        [TestCase(36)]
        [TestCase(37)]
        [TestCase(38)]
        [TestCase(39)]
        [TestCase(40)]
        [TestCase(41)]
        [TestCase(42)]
        [TestCase(43)]
        [TestCase(44)]
        [TestCase(45)]
        [TestCase(46)]
        [TestCase(47)]
        [TestCase(48)]
        [TestCase(49)]
        [TestCase(50)]
        public void HighIntensityLoad_PressureTest(int id)
        {
            Thread.Sleep(1500);
        }

        // --- SCENARIO 2: CPU STRESS (25 Tests) ---
        [TestMethod]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        [TestCase(12)]
        [TestCase(13)]
        [TestCase(14)]
        [TestCase(15)]
        [TestCase(16)]
        [TestCase(17)]
        [TestCase(18)]
        [TestCase(19)]
        [TestCase(20)]
        [TestCase(21)]
        [TestCase(22)]
        [TestCase(23)]
        [TestCase(24)]
        [TestCase(25)]
        public void CPU_Intensive_Calculation(int id)
        {
            long sum = 0;
            for (int i = 0; i < 50_000_000; i++)
            {
                sum += i;
            }
        }

        // --- SCENARIO 3: FLAKY TASKS (25 Tests) ---
        [TestMethod]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        [TestCase(12)]
        [TestCase(13)]
        [TestCase(14)]
        [TestCase(15)]
        [TestCase(16)]
        [TestCase(17)]
        [TestCase(18)]
        [TestCase(19)]
        [TestCase(20)]
        [TestCase(21)]
        [TestCase(22)]
        [TestCase(23)]
        [TestCase(24)]
        [TestCase(25)]
        public void RandomDuration_FlowTest(int id)
        {
            var random = new Random();
            Thread.Sleep(random.Next(100, 2000));
        }

        // --- SCENARIO 4: ERROR TOLERANCE (25 Tests) ---
        [TestMethod]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        [TestCase(12)]
        [TestCase(13)]
        [TestCase(14)]
        [TestCase(15)]
        [TestCase(16)]
        [TestCase(17)]
        [TestCase(18)]
        [TestCase(19)]
        [TestCase(20)]
        [TestCase(21)]
        [TestCase(22)]
        [TestCase(23)]
        [TestCase(24)]
        [TestCase(25)]
        public void Exception_StabilityTest(int id)
        {
            Thread.Sleep(200);
            throw new Exception("Simulated task failure");
        }

        // --- SCENARIO 5: IDLE SHRINKING (24 Tests) ---
        [TestMethod]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        [TestCase(12)]
        [TestCase(13)]
        [TestCase(14)]
        [TestCase(15)]
        [TestCase(16)]
        [TestCase(17)]
        [TestCase(18)]
        [TestCase(19)]
        [TestCase(20)]
        [TestCase(21)]
        [TestCase(22)]
        [TestCase(23)]
        [TestCase(24)]
        public void QuickTasks_ShrinkTest(int id)
        {
            Thread.Sleep(50);
        }

        // --- SCENARIO 6: CRITICAL HANG (1 Test) ---
        [TestMethod]
        [TestCase(150)]
        public void Deadlock_WatchdogTrigger(int id)
        {
            Thread.Sleep(10000);
        }
    }
}