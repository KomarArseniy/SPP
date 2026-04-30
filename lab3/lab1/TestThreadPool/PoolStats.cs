namespace TestThreadPool
{
    public struct PoolStats
    {
        public int TotalThreads { get; set; }
        public int BusyThreads { get; set; }
        public int QueueLength { get; set; }
        public int CompletedTasks { get; set; }
    }
}