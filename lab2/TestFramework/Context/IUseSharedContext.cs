using TestFramework.Context;

namespace TestFramework.Context
{
    public interface IUseSharedContext
    {
        GlobalContext Context { get; set; }
    }
}