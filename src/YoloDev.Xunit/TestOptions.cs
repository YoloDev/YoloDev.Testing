using System;
using System.Collections.Generic;
using Xunit.Abstractions;

namespace YoloDev.Xunit
{
    internal enum RunKind
    {
        Undefined,
        List,
        Test
    }

    internal class TestOptions : ITestFrameworkOptions, ITestFrameworkDiscoveryOptions, ITestFrameworkExecutionOptions
    {
        public TestOptions()
        {
            Sink = Environment.GetEnvironmentVariable("YOLODEV_XUNIT_SINK");
        }

        public string Configuration { get; set; }

        public int? CompilationServerPort { get; set; }

        public string PackageDirectory { get; set; }

        public bool DesignTime { get; set; }

        public RunKind RunKind { get; set; }

        public List<string> Tests { get; set; }

        public string Sink { get; set; }

        TValue ITestFrameworkOptions.GetValue<TValue>(string name, TValue defaultValue)
        {
            return defaultValue;
        }

        void ITestFrameworkOptions.SetValue<TValue>(string name, TValue value)
        {
            throw new NotImplementedException();
        }
    }
}