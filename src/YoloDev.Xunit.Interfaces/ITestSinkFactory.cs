using System;
using Microsoft.Framework.Runtime;

namespace YoloDev.Xunit
{
    [AssemblyNeutral]
    public interface ITestSinkFactory
    {
        ITestDiscoverySink CreateDiscoverySink(IServiceProvider services);

        ITestExecutionSink CreateExecutionSink(IServiceProvider services);
    }
}
