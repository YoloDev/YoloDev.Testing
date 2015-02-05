using System;
using System.Collections.Generic;
using Microsoft.Framework.Runtime;

namespace YoloDev.Xunit
{
    [AssemblyNeutral]
    public interface ITestResult
    {
        ITest Test { get; }

        TestOutcome Outcome { get; }

        string ErrorMessage { get; }

        string ErrorStackTrace { get; }

        string DisplayName { get; }

        IEnumerable<string> Messages { get; }

        string ComputerName { get; }

        TimeSpan Duration { get; }

        DateTimeOffset StartTime { get; }

        DateTimeOffset EndTime { get; }
    }
}