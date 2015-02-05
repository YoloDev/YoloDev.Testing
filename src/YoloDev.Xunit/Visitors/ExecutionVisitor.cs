using System;
using Microsoft.Framework.TestAdapter;
using Xunit.Abstractions;
using Xunit;

namespace YoloDev.Xunit.Visitors
{
    public class ExecutionVisitor : TestMessageVisitor<ITestAssemblyFinished>
    {
        readonly ITestExecutionSink _sink;
        bool _hasFailures;

        public bool HasFailures => _hasFailures;

        public ExecutionVisitor(ITestExecutionSink sink)
        {
            _sink = sink;
        }

        protected override bool Visit(ITestStarting testStarting)
        {
            if (testStarting.TestCases != null)
            {
                foreach (var testCase in testStarting.TestCases)
                {
                    _sink.RecordStart(testCase.ToTest());
                }
            }
            else
            {
                _sink.RecordStart(testStarting.TestCase.ToTest());
            }

            return base.Visit(testStarting);
        }

        protected override bool Visit(ITestSkipped testSkipped)
        {
            Register(TestOutcome.Skipped, testSkipped);
            return base.Visit(testSkipped);
        }

        protected override bool Visit(ITestFailed testFailed)
        {
            _hasFailures = true;
            Register(TestOutcome.Failed, testFailed);
            return base.Visit(testFailed);
        }

        protected override bool Visit(ITestPassed testPassed)
        {
            Register(TestOutcome.Passed, testPassed);
            return base.Visit(testPassed);
        }

        private void Register(TestOutcome outcome, ITestResultMessage result)
        {
            _sink.RecordResult(result.ToTestResult(outcome));
        }
    }
}