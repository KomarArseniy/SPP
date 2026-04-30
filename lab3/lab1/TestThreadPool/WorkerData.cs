using System;
using System.Threading;

namespace TestThreadPool
{
    internal class WorkerData
    {
        public Thread Thread { get; set; }
        public bool IsAlive { get; set; } = true;
        public DateTime? CurrentTaskStartTime { get; set; }
        public WorkItem CurrentWorkItem { get; set; }
    }
}