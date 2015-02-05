using System;
using Microsoft.Framework.TestAdapter;

namespace YoloDev.Xunit.Sinks
{
    public class DefaultTestDiscoverySink : ITestDiscoverySink
    {
        public void SendTest(ITest test)
        {
            Console.WriteLine($"Discovered: {test.FullyQualifiedName}");
        }
    }
}