using System;
using Microsoft.Framework.TestAdapter;

namespace YoloDev.Xunit.Sinks
{
    public class DefaultTestExecutionSink : ITestExecutionSink
    {
        public void RecordStart(Test test)
        {
            Console.WriteLine($"Started: {test.FullyQualifiedName} - {test.Id}");
        }

        public void RecordResult(TestResult testResult)
        {
            Console.WriteLine($"{testResult.Outcome}: {testResult.Test.FullyQualifiedName} - {testResult.Test.Id} - {testResult.Duration}");
        }
    }
}