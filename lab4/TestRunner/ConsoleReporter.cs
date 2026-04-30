using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TestThreadPool;

namespace TestRunner
{
    public class ConsoleReporter
    {
        private int _passCount;
        private int _failCount;
        private int _skipCount;
        private static readonly object _lock = new object();

        private CancellationTokenSource _monitorCts;
        private Task _monitorTask;
        private string _originalTitle;

        public ConsoleReporter()
        {
            Console.OutputEncoding = Encoding.UTF8;
        }

        // --- Подписка на события пула (ЛР4) ---

        public void SubscribeToPoolEvents(CustomThreadPool pool)
        {
            pool.OnWorkerStarted += name =>
                PrintPoolEvent($"[Pool] Worker {name} started", ConsoleColor.Cyan);

            pool.OnWorkerStopped += (name, reason) =>
                PrintPoolEvent($"[Pool] Worker {name} stopped. Reason: {reason}", ConsoleColor.Magenta);

            pool.OnTaskStarted += (worker, task) =>
                PrintPoolEvent($"[Pool] {worker} began executing task", ConsoleColor.DarkGray);
        }

        private void PrintPoolEvent(string message, ConsoleColor color)
        {
            lock (_lock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        // --- Мониторинг пула ---

        public void StartPoolMonitoring(CustomThreadPool pool, int totalTasks, int maxThreads)
        {
            _originalTitle = Console.Title;
            _monitorCts    = new CancellationTokenSource();
            pool.OnLogMessage += HandlePoolLogMessage;
            _monitorTask = Task.Run(() => MonitorLoop(pool, totalTasks, maxThreads, _monitorCts.Token));
        }

        public void StopPoolMonitoring(CustomThreadPool pool)
        {
            if (_monitorCts == null) return;

            _monitorCts.Cancel();
            pool.OnLogMessage -= HandlePoolLogMessage;
            try { _monitorTask?.Wait(1000); } catch { }
            _monitorCts.Dispose();
            _monitorCts = null;
            Console.Title = _originalTitle ?? "TestRunner CLI Pro";
        }

        private void HandlePoolLogMessage(string msg)
        {
            lock (_lock)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("  [Pool Event] ");

                if      (msg.Contains("UP")       || msg.Contains("Added"))   Console.ForegroundColor = ConsoleColor.Cyan;
                else if (msg.Contains("DOWN")     || msg.Contains("removed")) Console.ForegroundColor = ConsoleColor.Magenta;
                else if (msg.Contains("Watchdog") || msg.Contains("hung"))    Console.ForegroundColor = ConsoleColor.Yellow;
                else Console.ForegroundColor = ConsoleColor.Gray;

                Console.WriteLine(msg);
                Console.ResetColor();
            }
        }

        private async Task MonitorLoop(CustomThreadPool pool, int totalTasks, int maxThreads, CancellationToken token)
        {
            var lastPrint = DateTime.Now;
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var stats = pool.GetStats();
                    Console.Title = $"TR Monitor | Threads: {stats.TotalThreads}/{maxThreads} | Busy: {stats.BusyThreads} | Queue: {stats.QueueLength} | Done: {stats.CompletedTasks}/{totalTasks}";

                    if (stats.CompletedTasks >= totalTasks) break;

                    if ((DateTime.Now - lastPrint).TotalSeconds >= 1)
                    {
                        lock (_lock)
                        {
                            Console.ForegroundColor = ConsoleColor.DarkCyan;
                            Console.WriteLine($"\n  === Pool Status: {stats.BusyThreads} of {stats.TotalThreads} threads busy | Queue: {stats.QueueLength} ===\n");
                            Console.ResetColor();
                        }
                        lastPrint = DateTime.Now;
                    }

                    await Task.Delay(250, token);
                }
            }
            catch (OperationCanceledException) { }
        }

        // --- Стандартные методы результатов ---

        public void OnTestPassed(string name, long ms)
        {
            Interlocked.Increment(ref _passCount);
            PrintEntry(name, "PASS", ConsoleColor.Green, null, ms);
        }

        public void OnTestFailed(string name, string reason, long ms)
        {
            Interlocked.Increment(ref _failCount);
            PrintEntry(name, "FAIL", ConsoleColor.Red, reason, ms);
        }

        public void OnTestSkipped(string name, string reason)
        {
            Interlocked.Increment(ref _skipCount);
            PrintEntry(name, "SKIPPED", ConsoleColor.Yellow, reason, -1);
        }

        public void PrintClassHeader(string className, bool isE2E)
        {
            lock (_lock)
            {
                Console.WriteLine($"\nClass: {className} {(isE2E ? "[E2E Sequence]" : "")}");
            }
        }

        public void PrintError(string msg)
        {
            lock (_lock)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {msg}");
                Console.ResetColor();
            }
        }

        public void PrintFinalStats(long totalMs)
        {
            lock (_lock)
            {
                Console.WriteLine("--------------------------------------------------");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write($"PASSED: {_passCount}    ");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"FAILED: {_failCount}    ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"SKIPPED: {_skipCount}");
                Console.ResetColor();
                Console.WriteLine($"\nTotal Duration: {totalMs} ms");
                Console.WriteLine("--------------------------------------------------");
            }
        }

        private void PrintEntry(string name, string status, ConsoleColor color, string detail, long ms)
        {
            lock (_lock)
            {
                Console.Write($"  [{name}] ");
                Console.ForegroundColor = color;
                Console.Write(status);
                Console.ResetColor();

                if (ms >= 0)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write($" ({ms}ms)");
                    Console.ResetColor();
                }

                if (!string.IsNullOrEmpty(detail))
                    Console.Write($" - {detail}");

                Console.WriteLine();
            }
        }
    }
}
