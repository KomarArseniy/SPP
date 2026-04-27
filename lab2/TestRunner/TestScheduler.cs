using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TestRunner
{
    public class TestScheduler
    {
        public async Task ExecuteAsync(IEnumerable<Func<Task>> jobs, bool parallel, int maxThreads)
        {
            if (!parallel)
            {
                foreach (var job in jobs)
                    await job();
                return;
            }

            var pending = new List<Task>();
            using var limiter = new SemaphoreSlim(maxThreads);

            foreach (var job in jobs)
            {
                await limiter.WaitAsync();

                var t = Task.Run(async () =>
                {
                    try { await job(); }
                    finally { limiter.Release(); }
                });

                pending.Add(t);
            }

            await Task.WhenAll(pending);
        }
    }
}
