namespace TestFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class PriorityAttribute : Attribute
    {
        public int Level { get; }
        public PriorityAttribute(int level) => Level = level;
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class AuthorAttribute : Attribute
    {
        public string Name { get; }
        public AuthorAttribute(string name) => Name = name;
    }
}