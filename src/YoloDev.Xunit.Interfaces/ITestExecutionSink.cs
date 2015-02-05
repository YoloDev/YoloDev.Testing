using System;
using Microsoft.Framework.Runtime;

namespace YoloDev.Xunit
{
    [AssemblyNeutral]
    public interface ITestExecutionSink
    {
        void RecordResult(ITestResult testResult);
        void RecordStart(ITest test);
    }
}