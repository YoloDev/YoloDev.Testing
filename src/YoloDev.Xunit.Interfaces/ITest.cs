using System;
using System.Collections.Generic;
using Microsoft.Framework.Runtime;

namespace YoloDev.Xunit
{
    [AssemblyNeutral]
    public interface ITest
    {
        string CodeFilePath { get; }

        string DisplayName { get; }

        string FullyQualifiedName { get; }

        Guid? Id { get; }

        int? LineNumber { get; }

        IDictionary<string, object> Properties { get; }
    }
}