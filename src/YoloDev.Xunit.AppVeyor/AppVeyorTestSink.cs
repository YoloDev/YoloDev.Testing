using System;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace YoloDev.Xunit.AppVeyor
{
    public class AppVeyorTestSink : ITestDiscoverySink, ITestExecutionSink
    {
        Uri _base;
        public AppVeyorTestSink()
        {
            var url = Environment.GetEnvironmentVariable("APPVEYOR_API_URL");
            if(url == null)
            {
                Console.WriteLine("No APPVEYOR_API_URL environment variable found. Not sending messages.");
                return;
            }

            Uri uri;
            if(!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                Console.WriteLine($"APPVEYOR_API_URL ${url} is not a valid URI. Not sending messages.");
                return;
            }

            _base = uri;
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
                return;

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Post, "api/test"))
            {
                client.BaseAddress = _base;

                var payload = new JObject(
                    new JProperty("testName", new JValue(test.FullyQualifiedName)),
                    new JProperty("testFramework", new JValue("Xunit"))
                ).ToString(Formatting.None);

                using (var content = new StringContent(payload))
                {
                    request.Content = content;
                    client.SendAsync(request).Result.EnsureSuccessStatusCode();
                }
            }
        }

        private void RegisterResult(ITestResult result)
        {
            if (_base == null)
                return;

            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Put, "api/test"))
            {
                client.BaseAddress = _base;

                var payload = new JObject(
                    new JProperty("testName", new JValue(result.Test.FullyQualifiedName)),
                    new JProperty("outcome", new JValue(result.Outcome.ToString())),
                    new JProperty("durationMilliseconds", new JValue(result.Duration.TotalMilliseconds)),
                    new JProperty("ErrorMessage", new JValue(result.ErrorMessage)),
                    new JProperty("ErrorStackTrace", new JValue(result.ErrorStackTrace)),
                    new JProperty("StdOut", new JValue(string.Join(Environment.NewLine, result.Messages)))
                ).ToString(Formatting.None);

                using (var content = new StringContent(payload))
                {
                    request.Content = content;
                    client.SendAsync(request).Result.EnsureSuccessStatusCode();
                }
            }
        }
    }
}