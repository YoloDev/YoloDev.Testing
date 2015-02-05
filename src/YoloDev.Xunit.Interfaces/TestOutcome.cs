using System;
using Microsoft.Framework.Runtime;

namespace YoloDev.Xunit
{
    [AssemblyNeutral]
    public enum TestOutcome
    {
        None,
        Passed,
        Failed,
        Skipped,
        NotFound
    }
}