using System;

[assembly: YoloDev.Xunit.AppVeyor.SinkLocator]

namespace YoloDev.Xunit.AppVeyor
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class SinkLocatorAttribute : Attribute, ITestSinkLocator
    {
        public ITestDiscoverySink CreateDiscoverySink()
        {
            return new AppVeyorTestSink();
        }

        public ITestExecutionSink CreateExecutionSink()
        {
            return new AppVeyorTestSink();
        }
    }
}