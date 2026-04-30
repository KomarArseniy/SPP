using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestThreadPool;

namespace TestRunner
{
    public class TestScheduler
    {
        private readonly ConsoleReporter _reporter;

        public TestScheduler(ConsoleReporter reporter)
        {
            _reporter = reporter;
        }

        public async Task ExecuteAsync(IEnumerable<Func<Task>> jobs, bool parallel, int maxThreads)
        {
            var jobList = jobs.ToList();

            if (!parallel || jobList.Count == 0)
            {
                foreach (var job in jobList)
                    await job();
                return;
            }

            using var pool = new CustomThreadPool(
                minThreads: 2,
                maxThreads: maxThreads,
                idleTimeoutMs: 3000,
                hangTimeoutMs: 5000);

            _reporter.StartPoolMonitoring(pool, jobList.Count, maxThreads);

            var pending = new List<Task>();

            foreach (var job in jobList)
            {
                var poolTask = pool.Enqueue(() => job().GetAwaiter().GetResult());
                pending.Add(poolTask);
            }

            try { await Task.WhenAll(pending); }
            catch { }

            pool.WaitForIdle();

            _reporter.StopPoolMonitoring(pool);
        }
    }
}
