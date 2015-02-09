using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Common.CommandLine;
using Microsoft.Framework.Runtime.Common.DependencyInjection;
using Microsoft.Framework.Runtime.Loader;
using Microsoft.Framework.TestAdapter;
using Xunit;
using Xunit.Abstractions;
using YoloDev.Xunit.Sinks;
using YoloDev.Xunit.Visitors;

namespace YoloDev.Xunit
{
    public class Program
    {
        readonly IAssemblyLoaderContainer _container;
        readonly IApplicationEnvironment _environment;
        readonly IServiceProvider _services;
        readonly IAssemblyLoadContextFactory _loadContextFactory;
        readonly IAssemblyLoadContextAccessor _loadContextAccessor;

        public Program(
            IAssemblyLoaderContainer container,
            IApplicationEnvironment environment,
            IServiceProvider services,
            IAssemblyLoadContextFactory loadContextFactory,
            IAssemblyLoadContextAccessor loadContextAccessor)
        {
            _container = container;
            _environment = environment;
            _services = services;
            _loadContextFactory = loadContextFactory;
            _loadContextAccessor = loadContextAccessor;
        }

        public int Main(string[] args)
        {
            //Debugger.Launch();
            Console.WriteLine($"YoloDev.Xunit: {GetVersion()}");
            TestOptions options;
            int exitCode;

            bool shouldExit = ParseArgs(args, out options, out exitCode);
            if (shouldExit)
            {
                return exitCode;
            }

            return RunTests(options);
        }

        private bool ParseArgs(string[] args, out TestOptions options, out int exitCode)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: true);
            app.Name = "YoloDev.Xunit";

            RunKind kind = RunKind.Undefined;
            List<string> tests = null;
            var optionPackages = app.Option("--packages <PACKAGE_DIR>", "Directory containing packages",
                CommandOptionType.SingleValue);
            var optionConfiguration = app.Option("--configuration <CONFIGURATION>", "The configuration to run under", CommandOptionType.SingleValue);
            var optionCompilationServer = app.Option("--port <PORT>", "The port to the compilation server", CommandOptionType.SingleValue);
            var optionDesignTime = app.Option("--designtime", "Design Time", CommandOptionType.NoValue);
            var optionSink = app.Option("--sink", "Message sink", CommandOptionType.SingleValue);
            app.HelpOption("-?|-h|--help");
            app.VersionOption("--version", GetVersion());
            app.Command("list", listApp =>
            {
                listApp.OnExecute(() =>
                {
                    kind = RunKind.List;
                    return 0;
                });
            }, throwOnUnexpectedArg: true);
            app.Command("test", testApp =>
            {
                var optionTests = testApp.Option("--test <TEST_ID>", "Test to run", CommandOptionType.MultipleValue);
                testApp.OnExecute(() =>
                {
                    kind = RunKind.Test;
                    if (optionTests.HasValue())
                        tests = optionTests.Values;
                    return 0;
                });
            }, throwOnUnexpectedArg: true);

            app.Execute(args);

            options = null;
            exitCode = 0;

            if (app.IsShowingInformation)
            {
                // If help option or version option was specified, exit immediately with 0 exit code
                return true;
            }

            options = new TestOptions();
            options.RunKind = kind;
            options.DesignTime = optionDesignTime.HasValue();
            options.Tests = tests;
            options.Configuration = optionConfiguration.Value() ?? _environment.Configuration ?? "Debug";
            options.PackageDirectory = optionPackages.Value();
            options.Sink = optionSink.Value() ?? options.Sink;
            var portValue = optionCompilationServer.Value() ?? Environment.GetEnvironmentVariable("DOTNET_COMPILATION_SERVER_PORT");

            int port;
            if (!string.IsNullOrEmpty(portValue) && int.TryParse(portValue, out port))
            {
                options.CompilationServerPort = port;
            }

            var remainingArgs = new List<string>();

            return false;
        }

        private int RunTests(TestOptions options)
        {
            if (options.Sink != null)
            {
                var parts = options.Sink.Split('|');
                var packagesPath = Path.GetFullPath(parts[0]);
                var dependencyResolver = new NuGetDependencyResolver(packagesPath);
                var lib = dependencyResolver.GetDescription(new LibraryRange
                {
                    Name = parts[1]
                }, _environment.RuntimeFramework);
                dependencyResolver.Initialize(new[] { lib }, _environment.RuntimeFramework);
                
                var dependencyLoader = new NuGetAssemblyLoader(_loadContextAccessor, dependencyResolver);

                using (_container.AddLoader(dependencyLoader))
                using (var context = _loadContextFactory.Create())
                {
                    var assembly = dependencyLoader.Load(parts[1], context);
                    var locator = assembly.GetCustomAttributes()
                        .OfType<ITestSinkLocator>()
                        .FirstOrDefault();

                    if(locator == null)
                        throw new InvalidOperationException($"No assembly attribute found that implements the interface 'ITestSinkLocator' in the assembly ${assembly.GetName().Name}");

                    var testServices = new ServiceProvider(_services);
                    testServices.Add(typeof(ITestSinkLocator), locator);

                    return RunTestAssembly(options, ".", _container, testServices) ? 0 : -1;
                }
            }
            else
            {
                return RunTestAssembly(options, ".", _container, _services) ? 0 : -1;
            }
        }

        private bool RunTestAssembly(TestOptions options, string testTarget, IAssemblyLoaderContainer container, IServiceProvider services)
        {
            bool success = true;
            testTarget = Path.GetFullPath(testTarget);
            var applicationDir = Directory.Exists(testTarget) ? testTarget : Path.GetDirectoryName(testTarget);
            var applicationName = Directory.Exists(testTarget) ? Path.GetFileName(testTarget) : Path.GetFileNameWithoutExtension(testTarget);

            var hostOptions = new DefaultHostOptions
            {
                WatchFiles = false,
                PackageDirectory = options.PackageDirectory,
                TargetFramework = _environment.RuntimeFramework,
                Configuration = options.Configuration,
                ApplicationBaseDirectory = applicationDir,
                CompilationServerPort = options.CompilationServerPort
            };

            using (var host = new DefaultHost(hostOptions, services))
            using (host.AddLoaders(container))
            {
                host.Initialize();
                var libraryManager = (ILibraryManager)host.ServiceProvider.GetService(typeof(ILibraryManager));
                var assemblies = libraryManager.GetLibraryInformation(applicationName).LoadableAssemblies.Select(Assembly.Load);

                foreach (var assembly in assemblies)
                {
                    using (var framework = new Xunit2(new NullSourceInformationProvider(), assembly.GetName().Name))
                    {
                        switch (options.RunKind)
                        {
                        case RunKind.List:
                            DiscoverTests(options, framework, services);
                            break;

                        default:
                            success = RunTests(options, framework, services) && success;
                            break;
                        }
                    }
                }
            }

            return success;
        }

        private void DiscoverTests(TestOptions options, IFrontController frontController, IServiceProvider services)
        {
            var visitor = new DiscoveryVisitor((services.GetService(typeof(ITestSinkLocator)) as ITestSinkLocator)?.CreateDiscoverySink() ?? new DefaultTestDiscoverySink());
            frontController.Find(true, visitor, options);
            visitor.Finished.WaitOne();
        }

        private bool RunTests(TestOptions options, IFrontController frontController, IServiceProvider services)
        {
            var visitor = new ExecutionVisitor((services.GetService(typeof(ITestSinkLocator)) as ITestSinkLocator)?.CreateExecutionSink() ?? new DefaultTestExecutionSink());
            frontController.RunAll(visitor, options, options);
            visitor.Finished.WaitOne();
            return !visitor.HasFailures;
        }

        private static string GetVersion()
        {
            var assembly = typeof(Program).GetTypeInfo().Assembly;
            var assemblyInformationalVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return assemblyInformationalVersionAttribute.InformationalVersion;
        }
    }
}
