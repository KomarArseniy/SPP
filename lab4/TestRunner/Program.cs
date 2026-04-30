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
            Console.InputEncoding  = Encoding.UTF8;
            Console.Title = "TestRunner CLI Pro";

            ShowBanner();

            while (true)
            {
                try
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.Write("\nTR> ");
                    Console.ResetColor();

                    string input = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(input)) continue;

                    var tokens = Tokenize(input);
                    if (tokens.Count == 0) continue;

                    string cmd = tokens[0].ToLower();

                    switch (cmd)
                    {
                        case "q":
                        case "quit":
                            Console.WriteLine("Exiting...");
                            return;

                        case "cls":
                        case "clear":
                            Console.Clear();
                            ShowBanner();
                            break;

                        case "?":
                        case "help":
                            ShowHelp();
                            break;

                        case "run":
                            var options = BuildOptions(tokens.Skip(1).ToList());
                            if (options != null)
                            {
                                Console.WriteLine(
                                    $"Configuration: [Parallel: {options.RunInParallel}] " +
                                    $"[MaxThreads: {options.MaxDegreeOfParallelism}] " +
                                    $"[Category: {options.CategoryFilter ?? "All"}] " +
                                    $"[Author: {options.AuthorFilter ?? "All"}] " +
                                    $"[MinPriority: {(options.MinPriority.HasValue ? options.MinPriority.Value.ToString() : "All")}]");

                                var engine = new TestEngine();
                                await engine.RunTestsInAssembly(options);
                            }
                            break;

                        default:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Unknown command: '{cmd}'. Type 'help' for info.");
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
            var opts = new TestRunOptions();
            string userPath = null;

            for (int i = 0; i < args.Count; i++)
            {
                string flag = args[i];

                if (flag == "-p" || flag == "--parallel")
                {
                    opts.RunInParallel = true;
                }
                else if ((flag == "-c" || flag == "--category") && i + 1 < args.Count)
                {
                    opts.CategoryFilter = args[++i];
                }
                else if ((flag == "-m" || flag == "--max") && i + 1 < args.Count)
                {
                    if (int.TryParse(args[++i], out int val))
                        opts.MaxDegreeOfParallelism = val;
                }
                else if ((flag == "-a" || flag == "--author") && i + 1 < args.Count)
                {
                    opts.AuthorFilter = args[++i];
                }
                else if ((flag == "-prio" || flag == "--priority") && i + 1 < args.Count)
                {
                    if (int.TryParse(args[++i], out int prio))
                        opts.MinPriority = prio;
                }
                else if (!flag.StartsWith("-"))
                {
                    userPath = flag.Trim('"', '\'');
                }
            }

            if (string.IsNullOrEmpty(userPath))
            {
                userPath = AutoDetect();
                if (userPath != null)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"Auto-detected assembly: {Path.GetFileName(userPath)}");
                    Console.ResetColor();
                }
            }

            if (!string.IsNullOrEmpty(userPath) && File.Exists(userPath))
            {
                opts.AssemblyPath = userPath;
                return opts;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: Assembly not found at path: {userPath ?? "(null)"}");
            Console.ResetColor();
            return null;
        }

        static string AutoDetect()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            string local = Path.Combine(baseDir, "Tests.dll");
            if (File.Exists(local)) return local;

            try
            {
                string fw = new DirectoryInfo(baseDir).Name;

                string debugPath = Path.GetFullPath(
                    Path.Combine(baseDir, $@"..\..\..\..\Tests\bin\Debug\{fw}\Tests.dll"));
                if (File.Exists(debugPath)) return debugPath;

                string releasePath = Path.GetFullPath(
                    Path.Combine(baseDir, $@"..\..\..\..\Tests\bin\Release\{fw}\Tests.dll"));
                if (File.Exists(releasePath)) return releasePath;
            }
            catch { }

            return null;
        }

        static List<string> Tokenize(string line)
        {
            var tokens    = new List<string>();
            var buf       = new StringBuilder();
            bool inQuotes = false;

            foreach (char ch in line)
            {
                if (ch == '"' || ch == '\'') { inQuotes = !inQuotes; continue; }
                if (ch == ' ' && !inQuotes)
                {
                    if (buf.Length > 0) { tokens.Add(buf.ToString()); buf.Clear(); }
                }
                else buf.Append(ch);
            }
            if (buf.Length > 0) tokens.Add(buf.ToString());

            return tokens;
        }

        static void ShowBanner()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("==========================================");
            Console.WriteLine("      CUSTOM TEST RUNNER CLI v4.0         ");
            Console.WriteLine("==========================================");
            Console.ResetColor();
            Console.WriteLine("Type 'help' for commands.");
        }

        static void ShowHelp()
        {
            Console.WriteLine("\n--- COMMAND HELP ---");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("run [path] [flags]");
            Console.ResetColor();
            Console.WriteLine("  Runs tests from the specified DLL.");
            Console.WriteLine("  Flags:");
            Console.WriteLine("    -p                  : Enable parallel execution (uses CustomThreadPool)");
            Console.WriteLine("    -m <int>            : Max degree of parallelism (threads in pool)");
            Console.WriteLine("    -c <Category>       : Run only tests of the specified category");
            Console.WriteLine("    -a <Name>           : Run only tests by specified author");
            Console.WriteLine("    -prio <int>         : Run only tests with priority <= N (e.g. -prio 2)");

            Console.WriteLine("\nExamples:");
            Console.WriteLine("  run");
            Console.WriteLine("  run -p -m 4");
            Console.WriteLine("  run -c Filter");
            Console.WriteLine("  run -a AuthorA");
            Console.WriteLine("  run -a AuthorA -prio 2");
            Console.WriteLine("  run -c Yield");
            Console.WriteLine("  run -c ExpressionTree");
            Console.WriteLine("  run \"C:\\Tests\\Tests.dll\" -p -m 6 -c LoadTest");

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
