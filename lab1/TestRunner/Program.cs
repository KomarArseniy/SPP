using System.Text;

namespace TestRunner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            Console.Title = "TestRunner CLI";

            PrintHeader();

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("\nTR> ");
                Console.ResetColor();

                string input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input)) continue;

                var commands = ParseCommandString(input);
                if (commands.Count == 0) continue;

                string command = commands[0].ToLower();

                try
                {
                    switch (command)
                    {
                        case "quit":
                        case "q":
                            Console.WriteLine("Exiting...");
                            return;

                        case "clear":
                            Console.Clear();
                            PrintHeader();
                            break;

                        case "help":
                        case "?":
                            PrintHelp();
                            break;

                        case "run":
                            await HandleRunCommand(commands.Skip(1).ToList());
                            break;

                        default:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine($"Unknown command: '{command}'.");
                            Console.ResetColor();
                            Console.WriteLine("Type 'help' to see the list of commands.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Critical execution error: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }

        static async Task HandleRunCommand(List<string> args)
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
                else if (!arg.StartsWith("-"))
                {
                    providedPath = arg;
                }
            }

            if (!string.IsNullOrEmpty(providedPath))
            {
                string cleanPath = providedPath.Trim('"', '\'');

                if (File.Exists(cleanPath))
                {
                    options.AssemblyPath = cleanPath;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Error: File not found at path:\n{cleanPath}");
                    Console.ResetColor();
                    return;
                }
            }
            else
            {
                string auto = FindTestAssemblyAuto();
                if (auto != null)
                {
                    options.AssemblyPath = auto;
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.WriteLine($"Automatically selected assembly: {Path.GetFileName(auto)}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Error: Path not specified, and Tests.dll was not found automatically.");
                    Console.ResetColor();
                    Console.WriteLine("Please specify the path explicitly: run \"C:\\Path\\To\\Tests.dll\"");
                    return;
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Starting... [Parallel: {options.RunInParallel}, Category: {options.CategoryFilter ?? "All"}]");

            var engine = new TestEngine();
            await engine.RunTestsInAssembly(options);
        }

        static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("==========================================");
            Console.WriteLine("      CUSTOM TEST RUNNER CLI v2.0         ");
            Console.WriteLine("==========================================");
            Console.ResetColor();
            Console.WriteLine("Type 'help' for info or 'run' to start testing.");
        }

        static void PrintHelp()
        {
            Console.WriteLine("\n--- COMMAND HELP ---");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("run [path] [flags]");
            Console.ResetColor();
            Console.WriteLine("  Runs tests. If path is missing, searches for Tests.dll automatically.");
            Console.WriteLine("  Flags:");
            Console.WriteLine("    -p              : Enable parallel execution (faster)");
            Console.WriteLine("    -c <Category>   : Run only tests of the specified category");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("clear");
            Console.ResetColor();
            Console.WriteLine("  Clears the console screen.");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("quit");
            Console.ResetColor();
            Console.WriteLine("  Exit the program.");

            Console.WriteLine("\n--- EXAMPLES ---");
            Console.WriteLine("  run                                     (Auto-search, all tests)");
            Console.WriteLine("  run -p                                  (Auto-search, parallel)");
            Console.WriteLine("  run -c Critical                         (Critical tests only)");
            Console.WriteLine("  run \"C:\\Tests\\MyTests.dll\" -p -c Unit   (Explicit path, parallel, Unit tests)");
        }

        static string FindTestAssemblyAuto()
        {
            string runnerDir = AppDomain.CurrentDomain.BaseDirectory;

            string local = Path.Combine(runnerDir, "Tests.dll");
            if (File.Exists(local)) return local;
            try
            {
                string fw = new DirectoryInfo(runnerDir).Name;
                string dev = Path.GetFullPath(Path.Combine(runnerDir, $@"..\..\..\..\Tests\bin\Debug\{fw}\Tests.dll"));
                if (File.Exists(dev)) return dev;
            }
            catch { }

            return null;
        }
        static List<string> ParseCommandString(string commandLine)
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
    }
}