using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Framework.TestAdapter;

namespace YoloDev.Xunit.Messages
{
    public class TestWrapper : ITest
    {
        readonly Test _test;
        public TestWrapper(Test test)
        {
            _test = test;
        }

        string ITest.CodeFilePath => _test.CodeFilePath;
        string ITest.DisplayName => _test.DisplayName;
        string ITest.FullyQualifiedName => _test.FullyQualifiedName;
        Guid? ITest.Id => _test.Id;
        int? ITest.LineNumber => _test.LineNumber;
        IDictionary<string, object> ITest.Properties => new ReadOnlyDictionary<string, object>(_test.Properties);

        internal Test Wrapped => _test;
    }
}