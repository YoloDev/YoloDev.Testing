using System;
using System.Reflection;
using System.Runtime.Versioning;
using Microsoft.Framework.Runtime;

namespace YoloDev.Xunit
{
    internal class ReferenceLoader : IAssemblyLoader
    {
        readonly IAssemblyLoadContext _context;
        readonly IFrameworkReferenceResolver _frameworkReferenceResolver;
        readonly FrameworkName _framework;

        public ReferenceLoader(IFrameworkReferenceResolver frameworkReferenceResolver, FrameworkName framework)
        {
            _frameworkReferenceResolver = frameworkReferenceResolver;
            _framework = framework;
        }

        public Assembly Load(string name)
        {
            string path;
            Console.WriteLine($"Requesting {name}");
            if (_frameworkReferenceResolver.TryGetAssembly(name, _framework, out path))
            {
                Console.WriteLine($"Found {name} as {path}. Loading...");
                return _context.LoadFile(name);
            }

            return null;
        }
    }
}