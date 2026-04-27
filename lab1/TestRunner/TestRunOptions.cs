namespace TestRunner
{
    public class TestRunOptions
    {
        public string AssemblyPath { get; set; }
        public bool RunInParallel { get; set; } = false;
        public string CategoryFilter { get; set; } = null; 
    }
}