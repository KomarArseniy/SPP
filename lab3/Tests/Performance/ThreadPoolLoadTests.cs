using System;
using System.Threading;
using TestFramework.Attributes;

namespace Tests.Performance
{
    [TestClass]
    [Category("LoadTest")]
    public class ThreadPoolLoadTests
    {
        // --- СЦЕНАРИЙ 1: ПИКОВАЯ НАГРУЗКА (50 тестов) ---
        // Все задачи подаются одновременно, пул масштабируется вверх

        [TestMethod]
        [TestCase(1)]  [TestCase(2)]  [TestCase(3)]  [TestCase(4)]  [TestCase(5)]
        [TestCase(6)]  [TestCase(7)]  [TestCase(8)]  [TestCase(9)]  [TestCase(10)]
        [TestCase(11)] [TestCase(12)] [TestCase(13)] [TestCase(14)] [TestCase(15)]
        [TestCase(16)] [TestCase(17)] [TestCase(18)] [TestCase(19)] [TestCase(20)]
        [TestCase(21)] [TestCase(22)] [TestCase(23)] [TestCase(24)] [TestCase(25)]
        [TestCase(26)] [TestCase(27)] [TestCase(28)] [TestCase(29)] [TestCase(30)]
        [TestCase(31)] [TestCase(32)] [TestCase(33)] [TestCase(34)] [TestCase(35)]
        [TestCase(36)] [TestCase(37)] [TestCase(38)] [TestCase(39)] [TestCase(40)]
        [TestCase(41)] [TestCase(42)] [TestCase(43)] [TestCase(44)] [TestCase(45)]
        [TestCase(46)] [TestCase(47)] [TestCase(48)] [TestCase(49)] [TestCase(50)]
        public void BurstLoad_ScaleUpTest(int id)
        {
            Thread.Sleep(1500);
        }

        // --- СЦЕНАРИЙ 2: CPU-НАГРУЗКА (25 тестов) ---
        // Вычислительно-тяжёлые задачи без ожидания I/O

        [TestMethod]
        [TestCase(1)]  [TestCase(2)]  [TestCase(3)]  [TestCase(4)]  [TestCase(5)]
        [TestCase(6)]  [TestCase(7)]  [TestCase(8)]  [TestCase(9)]  [TestCase(10)]
        [TestCase(11)] [TestCase(12)] [TestCase(13)] [TestCase(14)] [TestCase(15)]
        [TestCase(16)] [TestCase(17)] [TestCase(18)] [TestCase(19)] [TestCase(20)]
        [TestCase(21)] [TestCase(22)] [TestCase(23)] [TestCase(24)] [TestCase(25)]
        public void CpuBound_HeavyCalculation(int id)
        {
            long sum = 0;
            for (int i = 0; i < 50_000_000; i++)
                sum += i;
        }

        // --- СЦЕНАРИЙ 3: ПЕРЕМЕННАЯ ЗАДЕРЖКА (25 тестов) ---
        // Задачи с разным временем выполнения — проверка корректной обработки неравномерной нагрузки

        [TestMethod]
        [TestCase(1)]  [TestCase(2)]  [TestCase(3)]  [TestCase(4)]  [TestCase(5)]
        [TestCase(6)]  [TestCase(7)]  [TestCase(8)]  [TestCase(9)]  [TestCase(10)]
        [TestCase(11)] [TestCase(12)] [TestCase(13)] [TestCase(14)] [TestCase(15)]
        [TestCase(16)] [TestCase(17)] [TestCase(18)] [TestCase(19)] [TestCase(20)]
        [TestCase(21)] [TestCase(22)] [TestCase(23)] [TestCase(24)] [TestCase(25)]
        public void VariableLatency_UniformFlowTest(int id)
        {
            var rng = new Random();
            Thread.Sleep(rng.Next(100, 2000));
        }

        // --- СЦЕНАРИЙ 4: ОТКАЗОУСТОЙЧИВОСТЬ (25 тестов) ---
        // Задачи бросают исключения — проверяем что пул продолжает работу

        [TestMethod]
        [TestCase(1)]  [TestCase(2)]  [TestCase(3)]  [TestCase(4)]  [TestCase(5)]
        [TestCase(6)]  [TestCase(7)]  [TestCase(8)]  [TestCase(9)]  [TestCase(10)]
        [TestCase(11)] [TestCase(12)] [TestCase(13)] [TestCase(14)] [TestCase(15)]
        [TestCase(16)] [TestCase(17)] [TestCase(18)] [TestCase(19)] [TestCase(20)]
        [TestCase(21)] [TestCase(22)] [TestCase(23)] [TestCase(24)] [TestCase(25)]
        public void FaultTolerance_ExceptionStabilityTest(int id)
        {
            Thread.Sleep(200);
            throw new Exception("Simulated task failure for fault-tolerance test");
        }

        // --- СЦЕНАРИЙ 5: АДАПТИВНОЕ СЖАТИЕ (24 теста) ---
        // Быстрые задачи → пул масштабируется вниз после бездействия

        [TestMethod]
        [TestCase(1)]  [TestCase(2)]  [TestCase(3)]  [TestCase(4)]  [TestCase(5)]
        [TestCase(6)]  [TestCase(7)]  [TestCase(8)]  [TestCase(9)]  [TestCase(10)]
        [TestCase(11)] [TestCase(12)] [TestCase(13)] [TestCase(14)] [TestCase(15)]
        [TestCase(16)] [TestCase(17)] [TestCase(18)] [TestCase(19)] [TestCase(20)]
        [TestCase(21)] [TestCase(22)] [TestCase(23)] [TestCase(24)]
        public void QuickTasks_AdaptiveShrinkTest(int id)
        {
            Thread.Sleep(50);
        }

        // --- СЦЕНАРИЙ 6: ЗАВИСШИЙ ПОТОК (1 тест) ---
        // Задача зависает на 10 секунд → Watchdog должен завершить её и создать новый поток

        [TestMethod]
        [TestCase(1)]
        public void HangDetection_WatchdogTriggerTest(int id)
        {
            Thread.Sleep(10_000);
        }
    }
}
