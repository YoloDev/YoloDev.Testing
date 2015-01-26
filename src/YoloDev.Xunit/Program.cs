using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Framework.Runtime;
using Microsoft.Framework.Runtime.Common.CommandLine;
using Microsoft.Framework.Runtime.Common.DependencyInjection;
using Microsoft.Framework.TestAdapter;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using YoloDev.Xunit.Sinks;
using YoloDev.Xunit.Visitors;

namespace YoloDev.Xunit
{
    public class Program
    {
        readonly IAssemblyLoaderContainer _container;
        readonly IApplicationEnvironment _environment;
        readonly IServiceProvider _services;

        public Program(IAssemblyLoaderContainer container, IApplicationEnvironment environment, IServiceProvider services)
        {
            _container = container;
            _environment = environment;
            _services = services;
        }

        public int Main(string[] args)
        {
            //Debugger.Launch();
            TestOptions options;
            string[] testTargets;
            int exitCode;

            bool shouldExit = ParseArgs(args, out options, out testTargets, out exitCode);
            if (shouldExit)
            {
                return exitCode;
            }

            return RunTests(options, testTargets);
        }

        private bool ParseArgs(string[] args, out TestOptions options, out string[] testTargets, out int exitCode)
        {
            Console.WriteLine(Assembly.Load(new AssemblyName("xunit.execution")).GetName().FullName);
            var app = new CommandLineApplication(throwOnUnexpectedArg: false);
            app.Name = "YoloDev.Xunit";

            RunKind kind = RunKind.Undefined;
            List<string> tests = null;
            List<string> assemblies = null;
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
                    assemblies = listApp.RemainingArguments;
                    return 0;
                });
            }, throwOnUnexpectedArg: false);
            app.Command("test", testApp =>
            {
                var optionTests = testApp.Option("--test <TEST_ID>", "Test to run", CommandOptionType.MultipleValue);
                testApp.OnExecute(() =>
                {
                    kind = RunKind.Test;
                    if (optionTests.HasValue())
                        tests = optionTests.Values;
                    assemblies = testApp.RemainingArguments;
                    return 0;
                });
            }, throwOnUnexpectedArg: false);

            app.Execute(args);
            if (assemblies == null)
                assemblies = app.RemainingArguments;

            options = null;
            testTargets = null;
            exitCode = 0;

            if (app.IsShowingInformation)
            {
                // If help option or version option was specified, exit immediately with 0 exit code
                return true;
            }
            else if (!assemblies.Any())
            {
                // If no subcommand was specified, show error message
                // and exit immediately with non-zero exit code
                Console.WriteLine("Please specify at least one target to test");
                exitCode = 2;
                return true;
            }

            options = new TestOptions();
            options.RunKind = kind;
            options.DesignTime = optionDesignTime.HasValue();
            options.Tests = tests;
            options.Configuration = optionConfiguration.Value() ?? _environment.Configuration ?? "Debug";
            options.PackageDirectory = optionPackages.Value();
            options.Sink = optionSink.Value();
            var portValue = optionCompilationServer.Value() ?? Environment.GetEnvironmentVariable("DOTNET_COMPILATION_SERVER_PORT");

            int port;
            if (!string.IsNullOrEmpty(portValue) && int.TryParse(portValue, out port))
            {
                options.CompilationServerPort = port;
            }

            var remainingArgs = new List<string>();
            remainingArgs.AddRange(assemblies);
            testTargets = remainingArgs.ToArray();

            return false;
        }

        private int RunTests(TestOptions options, IEnumerable<string> testTargets)
        {
            if (options.Sink != null)
            {
                var parts = options.Sink.Split('|');
                var assemblyPath = Path.GetFullPath(parts[0]);
                var assemblyDir = Directory.Exists(assemblyPath) ? assemblyPath : Path.GetDirectoryName(assemblyPath);
                var assemblyName = Directory.Exists(assemblyPath) ? Path.GetFileName(assemblyDir) : Path.GetFileNameWithoutExtension(assemblyPath);

                var hostOptions = new DefaultHostOptions
                {
                    WatchFiles = false,
                    PackageDirectory = options.PackageDirectory,
                    TargetFramework = _environment.RuntimeFramework,
                    Configuration = options.Configuration,
                    ApplicationBaseDirectory = assemblyDir,
                    CompilationServerPort = options.CompilationServerPort
                };

                using (var host = new DefaultHost(hostOptions, _services))
                using (host.AddLoaders(_container))
                {
                    host.Initialize();
                    var libraryManager = (ILibraryManager)host.ServiceProvider.GetService(typeof(ILibraryManager));
                    var assemblyN = libraryManager.GetLibraryInformation(assemblyName).LoadableAssemblies.Single();
                    var assembly = Assembly.Load(assemblyN);
                    var sinkType = assembly.GetType(parts[1]);
                    var sink = Activator.CreateInstance(sinkType);
                    var discoverySink = sink as ITestDiscoverySink;
                    var executionSink = sink as ITestExecutionSink;

                    var testServices = new ServiceProvider(host.ServiceProvider);
                    testServices.Add(typeof(ITestDiscoverySink), discoverySink);
                    testServices.Add(typeof(ITestExecutionSink), executionSink);

                    var container = (IAssemblyLoaderContainer)host.ServiceProvider.GetService(typeof(IAssemblyLoaderContainer));

                    return RunTests(options, testTargets, container, testServices);
                }
            }
            else
            {
                return RunTests(options, testTargets, _container, _services);
            }
        }

        private int RunTests(TestOptions options, IEnumerable<string> testTargets, IAssemblyLoaderContainer container, IServiceProvider services)
        {
            bool allSucceeded = true;

            foreach (var testTarget in testTargets)
            {
                allSucceeded = RunTestAssembly(options, testTarget, container, services) && allSucceeded;
            }

            return allSucceeded ? 0 : 1;
        }

        private bool RunTestAssembly(TestOptions options, string testTarget, IAssemblyLoaderContainer container, IServiceProvider services)
        {
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
            using (var testFramework = new XunitTestFramework())
            {
                host.Initialize();
                var libraryManager = (ILibraryManager)host.ServiceProvider.GetService(typeof(ILibraryManager));
                var assemblies = libraryManager.GetLibraryInformation(applicationName).LoadableAssemblies.Select(Assembly.Load);

                foreach (var assembly in assemblies)
                {
                    switch (options.RunKind)
                    {
                    case RunKind.List:
                        DiscoverTests(options, testFramework, new ReflectionAssemblyInfo(assembly));
                        break;

                    default:
                        RunTests(options, testFramework, assembly.GetName());
                        break;
                    }
                }
            }

            return true;
        }

        private void DiscoverTests(TestOptions options, TestFramework framework, IAssemblyInfo assemblyInfo)
        {
            using (var discoverer = framework.GetDiscoverer(assemblyInfo))
            {
                Console.WriteLine($"{assemblyInfo.Name}:");
                var visitor = new DiscoveryVisitor(new DefaultTestDiscoverySink());
                discoverer.Find(true, visitor, options);
                visitor.Finished.WaitOne();
            }
        }

        private void RunTests(TestOptions options, TestFramework framework, AssemblyName assemblyName)
        {
            using (var executor = framework.GetExecutor(assemblyName))
            {
                Console.WriteLine($"{assemblyName.Name}:");
                var visitor = new ExecutionVisitor(new DefaultTestExecutionSink());
                executor.RunAll(visitor, options, options);
                visitor.Finished.WaitOne();
            }
        }

        private static string GetVersion()
        {
            var assembly = typeof(Program).GetTypeInfo().Assembly;
            var assemblyInformationalVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            return assemblyInformationalVersionAttribute.InformationalVersion;
        }
    }
}
