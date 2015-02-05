using System;
using Microsoft.Framework.Runtime;

namespace YoloDev.Xunit
{
    [AssemblyNeutral]
    public interface ITestDiscoverySink
    {
        void SendTest(ITest test);
    }
}