using System;

namespace TestFramework.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class TestInitializeAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestCleanupAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class ClassInitializeAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Method)]
    public class ClassCleanupAttribute : Attribute { }
}