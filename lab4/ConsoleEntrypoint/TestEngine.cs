using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
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

        public async Task RunTestsInAssembly(string assemblyPath)
        {
            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFrom(assemblyPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading assembly: {ex.Message}");
                return;
            }

            Console.WriteLine($"Running tests in: {assembly.GetName().Name}");
            Console.WriteLine("--------------------------------------------------");

            var testClasses = assembly.GetTypes()
                .Where(t => t.GetCustomAttribute<TestClassAttribute>() != null);

            foreach (var testClassType in testClasses)
            {
                await RunTestClass(testClassType);
            }

            Console.WriteLine("--------------------------------------------------");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"PASSED: {_passed} ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"FAILED: {_failed} ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"SKIPPED: {_skipped}");
            Console.ResetColor();
        }

        private async Task RunTestClass(Type testClassType)
        {
            var methods = testClassType.GetMethods()
                .Where(m => m.GetCustomAttribute<TestMethodAttribute>() != null);

            if (!methods.Any()) return;

            Console.WriteLine($"Class: {testClassType.Name}");

            object testInstance = Activator.CreateInstance(testClassType);

            if (testInstance is IUseSharedContext contextUser)
            {
                contextUser.Context = new GlobalContext();
            }

            foreach (var method in methods)
            {
                var ignoreAttr = method.GetCustomAttribute<IgnoreAttribute>();
                if (ignoreAttr != null)
                {
                    PrintResult(method.Name, "SKIPPED", ConsoleColor.Yellow, ignoreAttr.Reason);
                    _skipped++;
                    continue;
                }

                var testCaseAttrs = method.GetCustomAttributes<TestCaseAttribute>();
                var dataSourceAttr = method.GetCustomAttribute<DataSourceAttribute>();

                if (testCaseAttrs.Any())
                {
                    foreach (var attr in testCaseAttrs)
                    {
                        await RunSingleTest(testInstance, method, attr.Arguments);
                    }
                }
                else if (dataSourceAttr != null)
                {
                    await RunDataDrivenTest(testInstance, method, dataSourceAttr.FilePath);
                }
                else
                {
                    await RunSingleTest(testInstance, method, null);
                }
            }
        }

        private async Task RunDataDrivenTest(object instance, MethodInfo method, string filePath)
        {
            if (!File.Exists(filePath))
            {
                PrintResult(method.Name, "FAIL", ConsoleColor.Red, $"File not found: {filePath}");
                _failed++;
                return;
            }

            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                var args = line.Split(';').Cast<object>().ToArray();
                await RunSingleTest(instance, method, args);
            }
        }

        private async Task RunSingleTest(object instance, MethodInfo method, object[] parameters)
        {
            string testName = method.Name;
            if (parameters != null) testName += $"({string.Join(", ", parameters)})";

            var initMethod = instance.GetType().GetMethods()
                .FirstOrDefault(m => m.GetCustomAttribute<MyTestInitializeAttribute>() != null);
            initMethod?.Invoke(instance, null);

            bool passed = true;
            string failMessage = "";
            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                var timeoutAttr = method.GetCustomAttribute<MyTimeoutAttribute>();
                int timeout = timeoutAttr?.Milliseconds ?? -1;

                var task = Task.Run(async () =>
                {
                    var expectedExAttr = method.GetCustomAttribute<MyExpectedExceptionAttribute>();

                    try
                    {
                        var result = method.Invoke(instance, parameters);
                        if (result is Task t) await t; 

                        if (expectedExAttr != null)
                        {
                            throw new TestFailedException($"Expected exception {expectedExAttr.ExceptionType.Name} was not thrown.");
                        }
                    }
                    catch (TargetInvocationException ex)
                    {
                        var inner = ex.InnerException;

                        if (expectedExAttr != null && expectedExAttr.ExceptionType.IsAssignableFrom(inner.GetType()))
                        {
                            return; 
                        }

                        throw inner; 
                    }
                });

                if (timeout > 0)
                {
                    if (await Task.WhenAny(task, Task.Delay(timeout)) != task)
                    {
                        throw new TestFailedException($"Test timed out after {timeout}ms");
                    }
                }
                else
                {
                    await task;
                }
            }
            catch (TestFailedException ex)
            {
                passed = false;
                failMessage = ex.Message;
            }
            catch (Exception ex)
            {
                passed = false;
                failMessage = $"Unexpected Error: {ex.GetType().Name} - {ex.Message}";
            }
            finally
            {
                sw.Stop();
                var cleanupMethod = instance.GetType().GetMethods()
                    .FirstOrDefault(m => m.GetCustomAttribute<MyTestCleanupAttribute>() != null);
                cleanupMethod?.Invoke(instance, null);
            }

            if (passed)
            {
                PrintResult(testName, "PASS", ConsoleColor.Green, $"{sw.ElapsedMilliseconds}ms");
                _passed++;
            }
            else
            {
                PrintResult(testName, "FAIL", ConsoleColor.Red, failMessage);
                _failed++;
            }
        }

        private void PrintResult(string name, string status, ConsoleColor color, string message)
        {
            Console.Write($"[{name}] ");
            Console.ForegroundColor = color;
            Console.Write(status);
            Console.ResetColor();
            if (!string.IsNullOrEmpty(message))
            {
                Console.Write($" - {message}");
            }
            Console.WriteLine();
        }
    }
}