using System;
using System.Net.Http;
using System.Runtime.Versioning;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace YoloDev.Xunit.AppVeyor
{
    public class AppVeyorTestSink : ITestDiscoverySink, ITestExecutionSink
    {
        readonly Uri _base;
        readonly FrameworkName _framework;

        public AppVeyorTestSink(FrameworkName framework)
        {
            Console.WriteLine($"Running on environment: {framework.Identifier}");

            var url = Environment.GetEnvironmentVariable("APPVEYOR_API_URL");
            if(url == null)
            {
                Console.WriteLine("No APPVEYOR_API_URL environment variable found. Not sending messages.");
                return;
            }

            Uri uri;
            if(!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                Console.WriteLine($"APPVEYOR_API_URL {url} is not a valid URI. Not sending messages.");
                return;
            }

            _base = uri;
            _framework = framework;

            Console.WriteLine($"Base url is set to {uri}");
        }

        public void RecordResult(ITestResult testResult)
        {
            Console.WriteLine($"{testResult.Outcome}: {testResult.Test.FullyQualifiedName} - {testResult.Test.Id} - {testResult.Duration}");
            RegisterResult(testResult);
        }

        public void RecordStart(ITest test)
        {
            Console.WriteLine($"Started: {test.FullyQualifiedName} - {test.Id}");
            RegisterTest(test);
        }

        public void SendTest(ITest test)
        {
            Console.WriteLine($"Discovered: {test.FullyQualifiedName}");
            RegisterTest(test);
        }

        private void RegisterTest(ITest test)
        {
            if (_base == null)
            {
                Console.WriteLine($"Skipping since {nameof(_base)} is null");
                return;
            }

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Post, $"{_base}api/test"))
            {
                var payload = new JObject(
                    new JProperty("testName", new JValue(TestName(test))),
                    new JProperty("testFramework", new JValue("Xunit"))
                ).ToString(Formatting.None);
                Console.WriteLine($"Sending payload to {request.RequestUri} using {request.Method}: {payload}");

                using (var content = new StringContent(payload))
                {
                    request.Content = content;
                    var result = client.SendAsync(request).Result.EnsureSuccessStatusCode();
                    Console.WriteLine($"Result payload is {result.Content.ReadAsStringAsync().Result}");
                }
            }
        }

        private void RegisterResult(ITestResult result)
        {
            if (_base == null)
            {
                Console.WriteLine($"Skipping since {nameof(_base)} is null");
                return;
            }

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Put, $"{_base}api/test"))
            {
                var payload = new JObject(
                    new JProperty("testName", new JValue(TestName(result.Test))),
                    new JProperty("outcome", new JValue(result.Outcome.ToString())),
                    new JProperty("durationMilliseconds", new JValue(result.Duration.TotalMilliseconds)),
                    new JProperty("ErrorMessage", new JValue(result.ErrorMessage)),
                    new JProperty("ErrorStackTrace", new JValue(result.ErrorStackTrace)),
                    new JProperty("StdOut", new JValue(string.Join(Environment.NewLine, result.Messages)))
                ).ToString(Formatting.None);
                Console.WriteLine($"Sending payload to {request.RequestUri} using {request.Method}: {payload}");

                using (var content = new StringContent(payload))
                {
                    request.Content = content;
                    var r = client.SendAsync(request).Result.EnsureSuccessStatusCode();
                    Console.WriteLine($"Result payload is {r.Content.ReadAsStringAsync().Result}");
                }
            }
        }

        private string TestName(ITest test) =>
            $"{_framework.Identifier}: {test.FullyQualifiedName}";
    }
}