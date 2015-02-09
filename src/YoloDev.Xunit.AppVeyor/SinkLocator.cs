using System;
using Microsoft.Framework.Runtime;

[assembly: YoloDev.Xunit.AppVeyor.SinkLocator]

namespace YoloDev.Xunit.AppVeyor
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class SinkLocatorAttribute : Attribute, ITestSinkFactory
    {
        public ITestDiscoverySink CreateDiscoverySink(IServiceProvider services)
        {
            return new AppVeyorTestSink(services.Get<IApplicationEnvironment>().RuntimeFramework);
        }

        public ITestExecutionSink CreateExecutionSink(IServiceProvider services)
        {
            return new AppVeyorTestSink(services.Get<IApplicationEnvironment>().RuntimeFramework);
        }
    }
}