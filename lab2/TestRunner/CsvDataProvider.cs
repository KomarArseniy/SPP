using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace TestRunner
{
    public class CsvDataProvider
    {
        public List<object[]> ReadData(MethodInfo method, string fileName)
        {
            var rows = new List<object[]>();

            try
            {
                string asmDir = Path.GetDirectoryName(method.DeclaringType.Assembly.Location);
                string target = Path.Combine(asmDir, fileName);

                if (!File.Exists(target))
                {
                    string root = Path.GetFullPath(Path.Combine(asmDir, @"..\..\.."));
                    if (Directory.Exists(root))
                    {
                        var hits = Directory.GetFiles(root, fileName, SearchOption.AllDirectories);
                        if (hits.Any()) target = hits.First();
                    }
                }

                if (!File.Exists(target)) return rows;

                using var fs = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);

                string line;
                while ((line = sr.ReadLine()) != null)
                    if (!string.IsNullOrWhiteSpace(line))
                        rows.Add(line.Split(';').Cast<object>().ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DataLoad Error] {ex.Message}");
            }

            return rows;
        }
    }
}
