using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Framework.TestAdapter;

namespace YoloDev.Xunit.Messages
{
    public class TestResultWrapper : ITestResult
    {
        readonly TestResult _result;
        readonly TestWrapper _testWrapper;

        public TestResultWrapper(TestResult result)
        {
            _result = result;
            _testWrapper = result.Test.Wrap();
        }

        ITest ITestResult.Test => _testWrapper;
        TestOutcome ITestResult.Outcome => Convert(_result.Outcome);
        string ITestResult.ErrorMessage => _result.ErrorMessage;
        string ITestResult.ErrorStackTrace => _result.ErrorStackTrace;
        string ITestResult.DisplayName => _result.DisplayName;
        IEnumerable<string> ITestResult.Messages => _result.Messages == null ? null : new ReadOnlyCollection<string>(_result.Messages);
        string ITestResult.ComputerName => _result.ComputerName;
        TimeSpan ITestResult.Duration => _result.Duration;
        DateTimeOffset ITestResult.StartTime => _result.StartTime;
        DateTimeOffset ITestResult.EndTime => _result.EndTime;

        internal TestResult Wrapped => _result;

        private static TestOutcome Convert(Microsoft.Framework.TestAdapter.TestOutcome outcome)
        {
            switch (outcome)
            {
            case Microsoft.Framework.TestAdapter.TestOutcome.None: return TestOutcome.None;
            case Microsoft.Framework.TestAdapter.TestOutcome.Passed: return TestOutcome.Passed;
            case Microsoft.Framework.TestAdapter.TestOutcome.Failed: return TestOutcome.Failed;
            case Microsoft.Framework.TestAdapter.TestOutcome.Skipped: return TestOutcome.Skipped;
            case Microsoft.Framework.TestAdapter.TestOutcome.NotFound: return TestOutcome.NotFound;
            default: throw new ArgumentException("Unknown outcome enum value", "outcome");
            }
        }
    }
}