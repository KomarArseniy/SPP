using System;
using System.Text;
using System.Threading;

namespace TestRunner
{
    public class ConsoleReporter
    {
        private int _passCount;
        private int _failCount;
        private int _skipCount;
        private static readonly object _lock = new object();

        public ConsoleReporter()
        {
            Console.OutputEncoding = Encoding.UTF8;
        }

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
                Console.WriteLine();
                Console.WriteLine($"Total Duration: {totalMs} ms");
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
