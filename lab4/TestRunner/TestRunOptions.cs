namespace TestRunner
{
    public class TestRunOptions
    {
        public string AssemblyPath           { get; set; }
        public bool   RunInParallel          { get; set; } = false;
        public int    MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
        public string CategoryFilter         { get; set; } = null;
        public string AuthorFilter           { get; set; } = null;
        public int?   MinPriority            { get; set; } = null;
    }
}
