using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using TestFramework.Attributes;
using TestFramework.Context;
using TestFramework.Exceptions;

namespace TestRunner
{
    public class TestExecutor
    {
        private readonly ConsoleReporter _reporter;

        public TestExecutor(ConsoleReporter reporter)
        {
            _reporter = reporter;
        }

        public async Task RunSingleTest(Type suiteType, MethodInfo method, object[] args, GlobalContext sharedCtx)
        {
            string label = method.Name;
            if (args != null && args.Length > 0)
                label += $"({string.Join(", ", args)})";

            var timer = Stopwatch.StartNew();
            object instance = null;
            bool passed = false;
            string failMsg = "";

            try
            {
                instance = Activator.CreateInstance(suiteType);

                if (instance is IUseSharedContext ctxUser)
                    ctxUser.Context = sharedCtx;

                var beforeEach = suiteType.GetMethods()
                    .FirstOrDefault(m => m.GetCustomAttribute<TestInitializeAttribute>() != null);
                beforeEach?.Invoke(instance, null);

                var limitAttr = method.GetCustomAttribute<TimeoutAttribute>();
                int limitMs = limitAttr?.Milliseconds ?? -1;
                var expectEx = method.GetCustomAttribute<ExpectedExceptionAttribute>();

                var execution = Task.Run(async () =>
                {
                    try
                    {
                        var ret = method.Invoke(instance, args);
                        if (ret is Task t) await t;
                        if (expectEx != null)
                            throw new TestFailedException($"Expected exception {expectEx.ExceptionType.Name} was not thrown.");
                    }
                    catch (Exception ex)
                    {
                        var real = ex is TargetInvocationException tie ? tie.InnerException : ex;
                        if (expectEx != null && expectEx.ExceptionType.IsAssignableFrom(real.GetType())) return;
                        throw real;
                    }
                });

                if (limitMs > 0 && await Task.WhenAny(execution, Task.Delay(limitMs)) != execution)
                    throw new TestFailedException($"Timeout {limitMs}ms");

                await execution;
                passed = true;
            }
            catch (Exception ex)
            {
                var real = ex is AggregateException ae ? ae.InnerException
                         : ex is TargetInvocationException tie ? tie.InnerException
                         : ex;
                failMsg = real is TestFailedException ? real.Message : $"{real.GetType().Name}: {real.Message}";
            }
            finally
            {
                timer.Stop();
                try
                {
                    if (instance != null)
                    {
                        var afterEach = suiteType.GetMethods()
                            .FirstOrDefault(m => m.GetCustomAttribute<TestCleanupAttribute>() != null);
                        afterEach?.Invoke(instance, null);
                    }
                }
                catch { }
            }

            if (passed)
                _reporter.OnTestPassed(label, timer.ElapsedMilliseconds);
            else
                _reporter.OnTestFailed(label, failMsg, timer.ElapsedMilliseconds);
        }
    }
}
