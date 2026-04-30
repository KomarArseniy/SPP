using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TestThreadPool
{
    public class CustomThreadPool : IDisposable
    {
        private readonly int _minThreads;
        private readonly int _maxThreads;
        private readonly int _idleTimeoutMs;
        private readonly int _hangTimeoutMs;
        private readonly int _maxQueueWaitTimeMs = 500;

        private readonly Queue<WorkItem> _queue = new Queue<WorkItem>();
        private readonly List<WorkerData> _workers = new List<WorkerData>();
        private readonly object _lock = new object();

        private bool _isDisposed = false;
        private int _busyCount = 0;
        private int _completedCount = 0;

        private readonly Thread _watchdogThread;
        public event Action<string> OnLogMessage;

        public CustomThreadPool(int minThreads, int maxThreads, int idleTimeoutMs = 5000, int hangTimeoutMs = 10000)
        {
            _minThreads = minThreads;
            _maxThreads = maxThreads;
            _idleTimeoutMs = idleTimeoutMs;
            _hangTimeoutMs = hangTimeoutMs;

            for (int i = 0; i < _minThreads; i++) StartNewWorker();

            _watchdogThread = new Thread(WatchdogLoop) { IsBackground = true, Name = "ThreadPool_Watchdog" };
            _watchdogThread.Start();
        }

        public Task Enqueue(Action action, CancellationToken cancelToken = default)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(CustomThreadPool));

            var item = new WorkItem(action, cancelToken);

            lock (_lock)
            {
                _queue.Enqueue(item);
                Monitor.Pulse(_lock);

                if (_busyCount >= _workers.Count && _workers.Count < _maxThreads)
                {
                    OnLogMessage?.Invoke($"[Scale UP] All threads busy. Created new thread. Total: {_workers.Count + 1}");
                    StartNewWorker();
                }
            }

            return item.CompletionSource.Task;
        }

        public void WaitForIdle()
        {
            lock (_lock)
            {
                while (_queue.Count > 0 || _busyCount > 0)
                {
                    Monitor.Wait(_lock);
                }
            }
        }

        private void StartNewWorker()
        {
            var workerData = new WorkerData();
            var thread = new Thread(() => WorkerLoop(workerData))
            {
                IsBackground = true,
                Name = $"Worker_{Guid.NewGuid().ToString().Substring(0, 4)}"
            };

            workerData.Thread = thread;
            lock (_lock) _workers.Add(workerData);
            thread.Start();
        }

        private void WorkerLoop(WorkerData workerData)
        {
            while (true)
            {
                WorkItem workItem = null;

                lock (_lock)
                {
                    while (_queue.Count == 0 && !_isDisposed && workerData.IsAlive)
                    {
                        bool signaled = Monitor.Wait(_lock, _idleTimeoutMs);

                        if (!signaled && _workers.Count > _minThreads)
                        {
                            OnLogMessage?.Invoke($"[Scale DOWN] Thread {workerData.Thread.Name} removed (idle).");
                            _workers.Remove(workerData);
                            return;
                        }
                    }

                    if (_isDisposed || !workerData.IsAlive)
                    {
                        _workers.Remove(workerData);
                        return;
                    }

                    workItem = _queue.Dequeue();
                    _busyCount++;
                    workerData.CurrentTaskStartTime = DateTime.UtcNow;
                    workerData.CurrentWorkItem = workItem; 
                }

                try
                {
                    if (workItem.CancelToken.IsCancellationRequested)
                    {
                        workItem.CompletionSource.TrySetCanceled(workItem.CancelToken);
                    }
                    else
                    {
                        workItem.Action?.Invoke();
                        workItem.CompletionSource.TrySetResult(true);
                    }
                }
                catch (Exception ex)
                {
                    workItem.CompletionSource.TrySetException(ex);
                }
                finally
                {
                    lock (_lock)
                    {
                        _busyCount--;
                        _completedCount++;
                        workerData.CurrentTaskStartTime = null;
                        workerData.CurrentWorkItem = null; 

                        if (_queue.Count == 0 && _busyCount == 0)
                        {
                            Monitor.PulseAll(_lock);
                        }
                    }
                }
            }
        }

        private void WatchdogLoop()
        {
            while (!_isDisposed)
            {
                Thread.Sleep(1000);

                lock (_lock)
                {
                    if (_queue.Count > 0)
                    {
                        var oldestItem = _queue.Peek();
                        if ((DateTime.UtcNow - oldestItem.EnqueuedAt).TotalMilliseconds > _maxQueueWaitTimeMs)
                        {
                            if (_workers.Count < _maxThreads)
                            {
                                OnLogMessage?.Invoke($"[Scale UP] Queue bottleneck (> {_maxQueueWaitTimeMs}ms). Added thread.");
                                StartNewWorker();
                            }
                        }
                    }

                    for (int i = _workers.Count - 1; i >= 0; i--)
                    {
                        var worker = _workers[i];
                        if (worker.CurrentTaskStartTime.HasValue &&
                           (DateTime.UtcNow - worker.CurrentTaskStartTime.Value).TotalMilliseconds > _hangTimeoutMs)
                        {
                            OnLogMessage?.Invoke($"[Watchdog] Thread {worker.Thread.Name} hung! Killing task and replacing thread.");

                            worker.CurrentWorkItem?.CompletionSource.TrySetException(
                                new TimeoutException($"Task execution exceeded {_hangTimeoutMs}ms and was terminated by Watchdog."));

                            worker.IsAlive = false;
                            worker.CurrentTaskStartTime = null;

                            _busyCount--;
                            _completedCount++;
                            _workers.RemoveAt(i);

                            if (_queue.Count == 0 && _busyCount == 0)
                            {
                                Monitor.PulseAll(_lock);
                            }

                            StartNewWorker(); 
                        }
                    }
                }
            }
        }

        public PoolStats GetStats()
        {
            lock (_lock)
            {
                return new PoolStats
                {
                    TotalThreads = _workers.Count,
                    BusyThreads = _busyCount,
                    QueueLength = _queue.Count,
                    CompletedTasks = _completedCount
                };
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _isDisposed = true;
                Monitor.PulseAll(_lock);
            }
            _watchdogThread?.Join(1000);
        }
    }
}