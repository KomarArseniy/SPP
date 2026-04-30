using System;
using System.Reflection;
using TestFramework.Attributes;

namespace TestRunner
{
    public static class TestFilterFactory
    {
        public delegate bool TestFilter(Type classType, MethodInfo method);

        public static TestFilter CreateFilter(TestRunOptions options)
        {
            TestFilter combinedFilter = (c, m) => true;

            if (!string.IsNullOrEmpty(options.CategoryFilter))
            {
                combinedFilter += (c, m) =>
                {
                    var catAttr = m.GetCustomAttribute<CategoryAttribute>()
                               ?? c.GetCustomAttribute<CategoryAttribute>();
                    return catAttr?.CategoryName == options.CategoryFilter;
                };
            }

            if (!string.IsNullOrEmpty(options.AuthorFilter))
            {
                combinedFilter += (c, m) =>
                {
                    var authAttr = m.GetCustomAttribute<AuthorAttribute>()
                                ?? c.GetCustomAttribute<AuthorAttribute>();

                    return authAttr?.Name.Equals(options.AuthorFilter, StringComparison.OrdinalIgnoreCase) ?? false;
                };
            }

            if (options.MinPriority.HasValue)
            {
                combinedFilter += (c, m) =>
                {
                    var prioAttr = m.GetCustomAttribute<PriorityAttribute>();
                    int level = prioAttr?.Level ?? 99;
                    return level <= options.MinPriority.Value;
                };
            }

            return combinedFilter;
        }

        public static bool ExecuteAll(TestFilter filter, Type c, MethodInfo m)
        {
            if (filter == null) return true;

            var invocationList = filter.GetInvocationList();

            if (invocationList.Length == 1) return filter(c, m);

            for (int i = 1; i < invocationList.Length; i++)
            {
                var individualFilter = (TestFilter)invocationList[i];
                if (!individualFilter(c, m)) return false;
            }

            return true;
        }
    }
}