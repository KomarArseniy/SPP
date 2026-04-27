using System;
using System.IO;
using System.Threading.Tasks;

namespace TestRunner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== My Custom Test Runner ===");

            // Путь к DLL с тестами. 
            // В реальной жизни это передается через аргументы командной строки args[0]
            // Для удобства разработки захардкодим относительный путь к выходной папке проекта с тестами

            // ВАЖНО: Укажите здесь правильный путь к DLL вашего проекта MyProjectTests
            // Обычно это что-то вроде: 
            // string testAssemblyPath = Path.GetFullPath(@"..\..\..\..\MyProjectTests\bin\Debug\net6.0\MyProjectTests.dll");

            // Чтобы упростить задачу, мы попросим пользователя ввести путь или попытаемся найти
            string testAssemblyPath = "";

            if (args.Length > 0)
            {
                testAssemblyPath = args[0];
            }
            else
            {
                Console.WriteLine("Enter full path to Test Project DLL:");
                testAssemblyPath = Console.ReadLine()?.Trim('"');
            }

            if (!File.Exists(testAssemblyPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: File not found at {testAssemblyPath}");
                Console.ResetColor();
                return;
            }

            var engine = new TestEngine();
            await engine.RunTestsInAssembly(testAssemblyPath);

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}