using System;
using System.Net.Http;
using Microsoft.Framework.TestAdapter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace YoloDev.Xunit.AppVeyor
{
    public class AppVeyorTestSink : ITestDiscoverySink, ITestExecutionSink
    {
        public void RecordResult(TestResult testResult)
        {
            RegisterResult(testResult);
        }

        public void RecordStart(Test test)
        {
            RegisterTest(test);
        }

        public void SendTest(Test test)
        {
            RegisterTest(test);
        }

        private void RegisterTest(Test test)
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Post, "api/test"))
            {
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

        private void RegisterResult(TestResult result)
        {
            using (var client = new HttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Put, "api/test"))
            {
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