using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Framework.Runtime;

namespace YoloDev.Xunit
{
    internal class DirectoryLoader : IAssemblyLoader, IDisposable
    {
        static readonly string[] EXTENSIONS = new[] { ".dll", ".exe" };

        readonly string _assemblyDir;
        readonly IAssemblyLoadContext _loadContext;

        public DirectoryLoader(string assemblyDir, IAssemblyLoadContext loadContext)
        {
            _assemblyDir = assemblyDir;
            _loadContext = loadContext;
        }

        public Assembly Load(string name)
        {
            var file = Search(_assemblyDir, name, EXTENSIONS);

            if (file == null)
                return null;

            return _loadContext.LoadFile(file);
        }

        public void Dispose()
        {
            _loadContext.Dispose();
        }

        private static string Search(string dir, string fileName, IEnumerable<string> extensions)
        {
            foreach(var ext in extensions)
            {
                var file = Path.Combine(dir, fileName + ext);
                if (File.Exists(file))
                    return file;
            }

            return null;
        }
    }
}