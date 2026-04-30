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
        private readonly TestScheduler   _scheduler;
        private readonly TestExecutor    _executor;
        private readonly CsvDataProvider _csvProvider;
        private readonly AssemblyLoader  _loader;

        public TestEngine()
        {
            _reporter    = new ConsoleReporter();
            _scheduler   = new TestScheduler(_reporter);
            _executor    = new TestExecutor(_reporter);
            _csvProvider = new CsvDataProvider();
            _loader      = new AssemblyLoader();
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

                var classCtx = new GlobalContext();

                foreach (var suite in suites)
                    await RunSuite(suite, cfg, classCtx);
            }
            catch (Exception ex)
            {
                _reporter.PrintError($"Critical Engine Error: {ex.Message}");
            }
            finally
            {
                globalTimer.Stop();
                _reporter.PrintFinalStats(globalTimer.ElapsedMilliseconds);
            }
        }

        private async Task RunSuite(Type suiteType, TestRunOptions cfg, GlobalContext ctx)
        {
            var filterDelegate = TestFilterFactory.CreateFilter(cfg);

            var skipClass = suiteType.GetCustomAttribute<IgnoreAttribute>();
            if (skipClass != null)
            {
                _reporter.OnTestSkipped(suiteType.Name, skipClass.Reason);
                return;
            }

            InvokeStaticLifecycle<ClassInitializeAttribute>(suiteType, ctx);

            var methods = suiteType.GetMethods()
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
                .OrderBy(m => m.GetCustomAttribute<OrderAttribute>()?.Order ?? int.MaxValue)
                .ToList();

            _reporter.PrintClassHeader(suiteType.Name, suiteType.GetCustomAttribute<TestE2EAttribute>() != null);

            var jobs = new List<Func<Task>>();

            foreach (var method in methods)
            {
                if (!TestFilterFactory.ExecuteAll(filterDelegate, suiteType, method))
                    continue;

                var skipAttr = method.GetCustomAttribute<IgnoreAttribute>();
                if (skipAttr != null)
                {
                    _reporter.OnTestSkipped(method.Name, skipAttr.Reason);
                    continue;
                }

                jobs.AddRange(BuildJobsFor(suiteType, method, ctx));
            }

            if (jobs.Any())
                await _scheduler.ExecuteAsync(jobs, cfg.RunInParallel, cfg.MaxDegreeOfParallelism);

            InvokeStaticLifecycle<ClassCleanupAttribute>(suiteType, ctx);
        }

        private List<Func<Task>> BuildJobsFor(Type suiteType, MethodInfo method, GlobalContext ctx)
        {
            var jobs = new List<Func<Task>>();

            // Приоритет 1: [MethodDataSource] — генерация через yield return
            var methodSrcAttr = method.GetCustomAttribute<MethodDataSourceAttribute>();
            if (methodSrcAttr != null)
            {
                var srcMethod = suiteType.GetMethod(
                    methodSrcAttr.MethodName,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);

                if (srcMethod != null)
                {
                    object inst = srcMethod.IsStatic ? null : Activator.CreateInstance(suiteType);
                    var rows = srcMethod.Invoke(inst, null) as IEnumerable<object[]>;
                    if (rows != null)
                    {
                        foreach (var row in rows)
                            jobs.Add(() => _executor.RunSingleTest(suiteType, method, row, ctx));
                        return jobs;
                    }
                }
            }

            // Приоритет 2: [TestCase]
            var tcAttrs = method.GetCustomAttributes<TestCaseAttribute>().ToList();
            if (tcAttrs.Any())
            {
                foreach (var tc in tcAttrs)
                    jobs.Add(() => _executor.RunSingleTest(suiteType, method, tc.Arguments, ctx));
                return jobs;
            }

            // Приоритет 3: [DataSource] — CSV
            var dsAttr = method.GetCustomAttribute<DataSourceAttribute>();
            if (dsAttr != null)
            {
                var rows = _csvProvider.ReadData(method, dsAttr.FilePath);
                foreach (var row in rows)
                    jobs.Add(() => _executor.RunSingleTest(suiteType, method, row, ctx));
                return jobs;
            }

            // Без параметров
            jobs.Add(() => _executor.RunSingleTest(suiteType, method, null, ctx));
            return jobs;
        }

        private void InvokeStaticLifecycle<TAttr>(Type suiteType, GlobalContext ctx) where TAttr : Attribute
        {
            var m = suiteType.GetMethods()
                .FirstOrDefault(x => x.IsStatic && x.GetCustomAttribute<TAttr>() != null);
            if (m == null) return;

            try
            {
                bool passCtx = m.GetParameters().Any(p => p.ParameterType == typeof(GlobalContext));
                m.Invoke(null, passCtx ? new object[] { ctx } : null);
            }
            catch (Exception ex)
            {
                _reporter.PrintError(
                    $"Lifecycle {typeof(TAttr).Name} failed in {suiteType.Name}: {ex.InnerException?.Message ?? ex.Message}");
            }
        }
    }
}
