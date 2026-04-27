using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices; 
using System.Runtime.Loader; 
using System.Text;
using TestFramework.Attributes;
using TestFramework.Context;
using TestFramework.Exceptions;

namespace TestRunner
{
    public class TestEngine
    {
        private int _passed = 0;
        private int _failed = 0;
        private int _skipped = 0;
        private static readonly object _consoleLock = new object();

        public async Task RunTestsInAssembly(TestRunOptions options)
        {
            Console.OutputEncoding = Encoding.UTF8;

            var context = new TestAssemblyLoadContext(options.AssemblyPath);
            WeakReference alcWeakRef = new WeakReference(context, trackResurrection: true);

            try
            {
                await ExecuteTestsInContext(context, options);
            }
            finally
            {
                context.Unload();
            }

            for (int i = 0; i < 10 && alcWeakRef.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            PrintFinalStats();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task ExecuteTestsInContext(AssemblyLoadContext context, TestRunOptions options)
        {
            Assembly assembly;
            try
            {
                assembly = context.LoadFromAssemblyPath(options.AssemblyPath);
                Directory.SetCurrentDirectory(Path.GetDirectoryName(options.AssemblyPath));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when loading: {ex.Message}");
                return;
            }

            var testClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null);

            foreach (var testClassType in testClasses)
            {
                await RunTestClass(testClassType, options);
            }
        }

        private class TestAssemblyLoadContext : AssemblyLoadContext
        {
            private readonly string _resolverPath;

            public TestAssemblyLoadContext(string mainAssemblyPath) : base(isCollectible: true)
            {
                _resolverPath = Path.GetDirectoryName(mainAssemblyPath);
            }
            protected override Assembly Load(AssemblyName assemblyName)
            {
                if (assemblyName.Name == "TestFramework")
                {
                    return null;
                }

                string assemblyPath = Path.Combine(_resolverPath, assemblyName.Name + ".dll");
                if (File.Exists(assemblyPath))
                {
                    return LoadFromAssemblyPath(assemblyPath);
                }

                return null;
            }
        }
        private async Task RunTestClass(Type testClassType, TestRunOptions options)
        {
            var methods = testClassType.GetMethods()
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null)
                .ToList();

            if (!methods.Any()) return;

            var tasks = new List<Task>();
            bool headerPrinted = false;

            foreach (var method in methods)
            {
                if (!ShouldRunTest(testClassType, method, options.CategoryFilter)) continue;

                if (!headerPrinted)
                {
                    Console.WriteLine($"\nClass: {testClassType.Name}");
                    headerPrinted = true;
                }

                var ignoreAttr = method.GetCustomAttribute<IgnoreAttribute>();
                if (ignoreAttr != null)
                {
                    PrintResult(method.Name, "SKIPPED", ConsoleColor.Yellow, ignoreAttr.Reason);
                    Interlocked.Increment(ref _skipped);
                    continue;
                }

                var actions = GetTestActions(testClassType, method);

                if (options.RunInParallel)
                {
                    tasks.AddRange(actions.Select(action => Task.Run(action)));
                }
                else
                {
                    foreach (var action in actions) await action();
                }
            }

            if (options.RunInParallel && tasks.Any())
            {
                try { await Task.WhenAll(tasks); }
                catch (Exception ex) { Console.WriteLine($"Error in tasks: {ex.Message}"); }
            }
        }

        private bool ShouldRunTest(Type classType, MethodInfo method, string filter)
        {
            if (string.IsNullOrEmpty(filter)) return true;
            var classCats = classType.GetCustomAttributes<CategoryAttribute>().Select(c => c.CategoryName);
            var methodCats = method.GetCustomAttributes<CategoryAttribute>().Select(c => c.CategoryName);
            return classCats.Contains(filter) || methodCats.Contains(filter);
        }

        private List<Func<Task>> GetTestActions(Type type, MethodInfo method)
        {
            var actions = new List<Func<Task>>();
            var testCaseAttrs = method.GetCustomAttributes<TestCaseAttribute>();
            var dataSourceAttr = method.GetCustomAttribute<DataSourceAttribute>();

            if (testCaseAttrs.Any())
            {
                foreach (var attr in testCaseAttrs)
                    actions.Add(() => RunSingleTest(type, method, attr.Arguments));
            }
            else if (dataSourceAttr != null)
            {
                var data = ReadCsvData(method, dataSourceAttr.FilePath);
                foreach (var args in data)
                    actions.Add(() => RunSingleTest(type, method, args));
            }
            else
            {
                actions.Add(() => RunSingleTest(type, method, null));
            }
            return actions;
        }

        private async Task RunSingleTest(Type testClassType, MethodInfo method, object[] parameters)
        {
            string testName = method.Name;
            if (parameters != null && parameters.Length > 0)
                testName += $"({string.Join(", ", parameters)})";

            bool passed = false;
            string message = "";
            Stopwatch sw = Stopwatch.StartNew();
            object instance = null;

            try
            {
                instance = Activator.CreateInstance(testClassType);
                if (instance is IUseSharedContext contextUser) contextUser.Context = new GlobalContext();

                var initMethod = testClassType.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<TestInitializeAttribute>() != null);
                initMethod?.Invoke(instance, null);

                var timeoutAttr = method.GetCustomAttribute<TimeoutAttribute>();
                int timeout = timeoutAttr?.Milliseconds ?? -1;
                var expectedExAttr = method.GetCustomAttribute<ExpectedExceptionAttribute>();

                var task = Task.Run(async () =>
                {
                    try
                    {
                        var res = method.Invoke(instance, parameters);
                        if (res is Task t) await t;
                        if (expectedExAttr != null) throw new TestFailedException($"Expected exception {expectedExAttr.ExceptionType.Name} was not thrown.");
                    }
                    catch (Exception ex)
                    {
                        var actual = (ex is TargetInvocationException tex) ? tex.InnerException : ex;
                        if (expectedExAttr != null && expectedExAttr.ExceptionType.IsAssignableFrom(actual.GetType())) return;
                        throw actual;
                    }
                });

                if (timeout > 0)
                {
                    if (await Task.WhenAny(task, Task.Delay(timeout)) != task) throw new TestFailedException($"Timeout {timeout}ms");
                }
                await task;
                passed = true;
            }
            catch (Exception ex)
            {
                passed = false;
                var actual = (ex is AggregateException ae) ? ae.InnerException : (ex is TargetInvocationException tie) ? tie.InnerException : ex;
                message = actual is TestFailedException ? actual.Message : $"{actual.GetType().Name}: {actual.Message}";
            }
            finally
            {
                sw.Stop();
                try
                {
                    if (instance != null)
                    {
                        var cleanup = testClassType.GetMethods().FirstOrDefault(m => m.GetCustomAttribute<TestCleanupAttribute>() != null);
                        cleanup?.Invoke(instance, null);
                    }
                }
                catch { }
            }

            if (passed)
            {
                PrintResult(testName, "PASS", ConsoleColor.Green, $"{sw.ElapsedMilliseconds}ms");
                Interlocked.Increment(ref _passed);
            }
            else
            {
                PrintResult(testName, "FAIL", ConsoleColor.Red, message);
                Interlocked.Increment(ref _failed);
            }
        }

        private List<object[]> ReadCsvData(MethodInfo method, string fileName)
        {
            var result = new List<object[]>();
            string assemblyDir = Path.GetDirectoryName(method.DeclaringType.Assembly.Location);
            string foundPath = Path.Combine(assemblyDir, fileName);

            if (!File.Exists(foundPath))
            {
                string projectRoot = Path.GetFullPath(Path.Combine(assemblyDir, @"..\..\.."));
                if (Directory.Exists(projectRoot))
                {
                    var files = Directory.GetFiles(projectRoot, fileName, SearchOption.AllDirectories);
                    if (files.Any()) foundPath = files.First();
                }
            }

            if (File.Exists(foundPath))
            {
                try
                {
                    using (var fs = new FileStream(foundPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var sr = new StreamReader(fs))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                                result.Add(line.Split(';').Cast<object>().ToArray());
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Error reading CSV: {ex.Message}");
                }
            }
            return result;
        }

        private void PrintResult(string name, string status, ConsoleColor color, string message)
        {
            lock (_consoleLock)
            {
                Console.Write($"  [{name}] ");
                Console.ForegroundColor = color;
                Console.Write(status);
                Console.ResetColor();
                if (!string.IsNullOrEmpty(message)) Console.Write($" - {message}");
                Console.WriteLine();
            }
        }

        private void PrintFinalStats()
        {
            Console.WriteLine("--------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"PASSED: {_passed}   ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"FAILED: {_failed}   ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"SKIPPED: {_skipped}");
            Console.ResetColor();
        }
    }
}