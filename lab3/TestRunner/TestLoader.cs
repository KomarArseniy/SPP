using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace TestRunner
{
    public class AssemblyLoader : IDisposable
    {
        private IsolatedContext _ctx;
        private WeakReference _weakRef;

        public Assembly Load(string path)
        {
            _ctx = new IsolatedContext(path);
            _weakRef = new WeakReference(_ctx, trackResurrection: true);
            return _ctx.LoadFromAssemblyPath(path);
        }

        public void Unload()
        {
            _ctx?.Unload();
            _ctx = null;
        }

        public bool IsAlive => _weakRef?.IsAlive ?? false;

        public void Dispose() => Unload();

        private class IsolatedContext : AssemblyLoadContext
        {
            private readonly string _dir;

            public IsolatedContext(string asmPath) : base(isCollectible: true)
            {
                _dir = Path.GetDirectoryName(asmPath);
            }

            protected override Assembly Load(AssemblyName name)
            {
                if (name.Name == "TestFramework") return null;
                var path = Path.Combine(_dir, name.Name + ".dll");
                return File.Exists(path) ? LoadFromAssemblyPath(path) : null;
            }
        }
    }
}
