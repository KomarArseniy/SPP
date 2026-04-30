using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestRunner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            Console.Title = "TestRunner CLI Pro";

            PrintHeader();

            while (true)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("\nTR> ");
                    Console.ResetColor();

                    string input = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(input)) continue;

                    var commands = ParseArguments(input);
                    if (commands.Count == 0) continue;

                    string command = commands[0].ToLower();

                    switch (command)
                    {
                        case "quit":
                        case "q":
                            Console.WriteLine("Exiting...");
                            return;

                        case "clear":
                        case "cls":
                            Console.Clear();
                            PrintHeader();
                            break;

                        case "help":
                        case "?":
                            PrintHelp();
                            break;

                        case "run":
                            var options = BuildOptions(commands.Skip(1).ToList());

                            if (options != null)
                            {
                                Console.WriteLine($"Configuration: [Parallel: {options.RunInParallel}] [MaxThreads: {options.MaxDegreeOfParallelism}] [Category: {options.CategoryFilter ?? "All"}]");

                                var engine = new TestEngine(); 
                                await engine.RunTestsInAssembly(options);
                            }
                            break;

                        default:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Unknown command: '{command}'. Type 'help' for info.");
                            Console.ResetColor();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[CLI Error]: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }

        static TestRunOptions BuildOptions(List<string> args)
        {
            var options = new TestRunOptions();
            string providedPath = null;

            for (int i = 0; i < args.Count; i++)
            {
                string arg = args[i];

                if (arg == "-p" || arg == "--parallel")
                {
                    options.RunInParallel = true;
                }
                else if ((arg == "-c" || arg == "--category") && i + 1 < args.Count)
                {
                    options.CategoryFilter = args[++i];
                }
                else if ((arg == "-m" || arg == "--max") && i + 1 < args.Count)
                {
                    if (int.TryParse(args[++i], out int max))
                    {
                        options.MaxDegreeOfParallelism = max;
                    }
                }
                else if (!arg.StartsWith("-"))
                {
                    providedPath = arg.Trim('"', '\'');
                }
            }

            if (string.IsNullOrEmpty(providedPath))
            {
                providedPath = FindTestAssemblyAuto();

                if (providedPath != null)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"Auto-detected assembly: {Path.GetFileName(providedPath)}");
                    Console.ResetColor();
                }
            }

            if (!string.IsNullOrEmpty(providedPath) && File.Exists(providedPath))
            {
                options.AssemblyPath = providedPath;
                return options;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: Assembly not found at path: {providedPath ?? "(null)"}");
            Console.ResetColor();
            return null;
        }

        static string FindTestAssemblyAuto()
        {
            string runnerDir = AppDomain.CurrentDomain.BaseDirectory;

            string local = Path.Combine(runnerDir, "Tests.dll");
            if (File.Exists(local)) return local;

            try
            {
                string targetFramework = new DirectoryInfo(runnerDir).Name; 

                string devPathDebug = Path.GetFullPath(Path.Combine(
                    runnerDir,
                    $@"..\..\..\..\Tests\bin\Debug\{targetFramework}\Tests.dll"
                ));
                if (File.Exists(devPathDebug)) return devPathDebug;

                string devPathRelease = Path.GetFullPath(Path.Combine(
                    runnerDir,
                    $@"..\..\..\..\Tests\bin\Release\{targetFramework}\Tests.dll"
                ));
                if (File.Exists(devPathRelease)) return devPathRelease;
            }
            catch
            {
            }

            return null;
        }

        static List<string> ParseArguments(string commandLine)
        {
            var args = new List<string>();
            var currentArg = new StringBuilder();
            bool inQuotes = false;

            foreach (char c in commandLine)
            {
                if (c == '"' || c == '\'')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (c == ' ' && !inQuotes)
                {
                    if (currentArg.Length > 0)
                    {
                        args.Add(currentArg.ToString());
                        currentArg.Clear();
                    }
                }
                else
                {
                    currentArg.Append(c);
                }
            }

            if (currentArg.Length > 0) args.Add(currentArg.ToString());

            return args;
        }

        static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("==========================================");
            Console.WriteLine("              TEST RUNNER CLI             ");
            Console.WriteLine("==========================================");
            Console.ResetColor();
            Console.WriteLine("Type 'help' for commands.");
        }

        static void PrintHelp()
        {
            Console.WriteLine("\n--- COMMAND HELP ---");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("run [path] [flags]");
            Console.ResetColor();
            Console.WriteLine("  Runs tests from the specified DLL.");
            Console.WriteLine("  Flags:");
            Console.WriteLine("    -p              : Enable parallel execution");
            Console.WriteLine("    -m <int>        : Max degree of parallelism (threads)");
            Console.WriteLine("    -c <Category>   : Run only specific category (e.g. 'e2e', 'perf')");

            Console.WriteLine("\nExamples:");
            Console.WriteLine("  run");
            Console.WriteLine("  run -p -m 4");
            Console.WriteLine("  run \"C:\\MyTests\\Tests.dll\" -c e2e");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("clear");
            Console.ResetColor();
            Console.WriteLine("  Clears console.");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("quit");
            Console.ResetColor();
            Console.WriteLine("  Exits the application.");
        }
    }
}