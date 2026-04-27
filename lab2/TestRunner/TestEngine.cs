using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TestFramework.Attributes;
using TestFramework.Context;

namespace TestRunner
{
    public class TestEngine
    {
        private readonly ConsoleReporter _reporter;
        private readonly TestScheduler _scheduler;
        private readonly TestExecutor _executor;
        private readonly CsvDataProvider _csvProvider;
        private readonly AssemblyLoader _loader;

        public TestEngine()
        {
            _reporter   = new ConsoleReporter();
            _scheduler  = new TestScheduler();
            _executor   = new TestExecutor(_reporter);
            _csvProvider = new CsvDataProvider();
            _loader     = new AssemblyLoader();
        }

        public async Task RunTestsInAssembly(TestRunOptions cfg)
        {
            var globalTimer = Stopwatch.StartNew();

            try
            {
                var asm = _loader.Load(cfg.AssemblyPath);
                Directory.SetCurrentDirectory(Path.GetDirectoryName(cfg.AssemblyPath));

                var suites = asm.GetTypes()
                    .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null)
                    .ToList();

                foreach (var suite in suites)
                    await RunSuite(suite, cfg);
            }
            catch (Exception ex)
            {
                _reporter.PrintError($"Critical Engine Error: {ex.Message}");
            }
            finally
            {
                _loader.Unload();
            }

            for (int i = 0; i < 10 && _loader.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            globalTimer.Stop();
            _reporter.PrintFinalStats(globalTimer.ElapsedMilliseconds);
        }

        private async Task RunSuite(Type suiteType, TestRunOptions cfg)
        {
            var methods = suiteType.GetMethods()
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
                .ToList();

            if (!methods.Any()) return;

            bool isSequential = suiteType.GetCustomAttribute<TestE2EAttribute>() != null;
            if (isSequential)
                methods = methods.OrderBy(m => m.GetCustomAttribute<OrderAttribute>()?.Order ?? int.MaxValue).ToList();

            _reporter.PrintClassHeader(suiteType.Name, isSequential);

            var classCtx = new GlobalContext();
            InvokeStaticLifecycle<ClassInitializeAttribute>(suiteType, classCtx);

            bool runParallel = cfg.RunInParallel && !isSequential;
            var jobs = new List<Func<Task>>();

            foreach (var method in methods)
            {
                if (!MatchesFilter(suiteType, method, cfg.CategoryFilter)) continue;

                var skipAttr = method.GetCustomAttribute<IgnoreAttribute>();
                if (skipAttr != null)
                {
                    _reporter.OnTestSkipped(method.Name, skipAttr.Reason);
                    continue;
                }

                jobs.AddRange(BuildJobsFor(suiteType, method, classCtx));
            }

            if (jobs.Any())
                await _scheduler.ExecuteAsync(jobs, runParallel, cfg.MaxDegreeOfParallelism);

            InvokeStaticLifecycle<ClassCleanupAttribute>(suiteType, classCtx);
        }

        private List<Func<Task>> BuildJobsFor(Type suiteType, MethodInfo method, GlobalContext ctx)
        {
            var jobs = new List<Func<Task>>();
            var tcAttrs = method.GetCustomAttributes<TestCaseAttribute>().ToList();
            var dsAttr  = method.GetCustomAttribute<DataSourceAttribute>();

            if (tcAttrs.Any())
            {
                foreach (var tc in tcAttrs)
                    jobs.Add(() => _executor.RunSingleTest(suiteType, method, tc.Arguments, ctx));
            }
            else if (dsAttr != null)
            {
                var rows = _csvProvider.ReadData(method, dsAttr.FilePath);
                foreach (var row in rows)
                    jobs.Add(() => _executor.RunSingleTest(suiteType, method, row, ctx));
            }
            else
            {
                jobs.Add(() => _executor.RunSingleTest(suiteType, method, null, ctx));
            }

            return jobs;
        }

        private void InvokeStaticLifecycle<TAttr>(Type suiteType, GlobalContext ctx) where TAttr : Attribute
        {
            var m = suiteType.GetMethods()
                .FirstOrDefault(x => x.IsStatic && x.GetCustomAttribute<TAttr>() != null);
            if (m == null) return;

            try
            {
                var passCtx = m.GetParameters().Any(p => p.ParameterType == typeof(GlobalContext));
                m.Invoke(null, passCtx ? new object[] { ctx } : null);
            }
            catch (Exception ex)
            {
                _reporter.PrintError(
                    $"Lifecycle {typeof(TAttr).Name} failed: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private bool MatchesFilter(Type suiteType, MethodInfo method, string filter)
        {
            if (string.IsNullOrEmpty(filter)) return true;

            var classCategories  = suiteType.GetCustomAttributes<CategoryAttribute>().Select(c => c.CategoryName);
            var methodCategories = method.GetCustomAttributes<CategoryAttribute>().Select(c => c.CategoryName);

            return classCategories.Contains(filter) || methodCategories.Contains(filter);
        }
    }
}
