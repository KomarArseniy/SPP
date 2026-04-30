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
            var result = new List<object[]>();
            try
            {
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DataLoad Error] {ex.Message}");
            }
            return result;
        }
    }
}