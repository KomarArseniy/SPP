using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestThreadPool
{
    internal class WorkItem
    {
        public Action Action { get; }
        public DateTime EnqueuedAt { get; }
        public CancellationToken CancelToken { get; }
        public TaskCompletionSource<bool> CompletionSource { get; }

        public WorkItem(Action action, CancellationToken cancelToken)
        {
            Action = action;
            CancelToken = cancelToken;
            EnqueuedAt = DateTime.UtcNow;

            // RunContinuationsAsynchronously не даст продолжениям await захватить наш рабочий поток
            CompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }
}