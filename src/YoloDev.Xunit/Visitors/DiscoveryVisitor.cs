using System;
using System.Collections.Generic;
using Microsoft.Framework.TestAdapter;
using Xunit.Abstractions;
using Xunit;

namespace YoloDev.Xunit.Visitors
{
    public class DiscoveryVisitor : TestMessageVisitor<IDiscoveryCompleteMessage>
    {
        readonly ITestDiscoverySink _sink;

        public DiscoveryVisitor(ITestDiscoverySink sink)
        {
            _sink = sink;
        }

        protected override bool Visit(ITestCaseDiscoveryMessage testCaseDiscovered)
        {
            if(testCaseDiscovered.TestCases != null)
            {
                foreach(var testCase in testCaseDiscovered.TestCases)
                {
                    _sink.SendTest(testCase.ToTest());
                }
            }
            else
            {
                _sink.SendTest(testCaseDiscovered.TestCase.ToTest());
            }

            return base.Visit(testCaseDiscovered);
        }
    }
}