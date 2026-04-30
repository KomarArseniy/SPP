using System;

namespace TestFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class DataSourceAttribute : Attribute
    {
        public string FilePath { get; }

        public DataSourceAttribute(string filePath)
        {
            FilePath = filePath;
        }
    }
}