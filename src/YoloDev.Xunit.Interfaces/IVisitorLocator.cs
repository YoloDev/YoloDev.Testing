using System;
using Microsoft.Framework.Runtime;

namespace YoloDev.Xunit
{
    [AssemblyNeutral]
    public interface ITestSinkLocator
    {
        ITestDiscoverySink CreateDiscoverySink();

        ITestExecutionSink CreateExecutionSink();
    }
}
