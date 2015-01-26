using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Framework.TestAdapter;
using Xunit.Abstractions;

namespace YoloDev.Xunit
{
    public static class Extensions
    {
#if ASPNETCORE50
        private readonly static HashAlgorithm _hash = SHA1.Create();
#else
        private readonly static HashAlgorithm _hash = new SHA1Managed();
#endif

        public static Test ToTest(this ITestCase testCase)
        {
            return new Test
            {
                CodeFilePath = testCase.SourceInformation?.FileName,
                LineNumber = testCase.SourceInformation?.LineNumber,
                DisplayName = testCase.DisplayName,
                FullyQualifiedName = $"{testCase.TestMethod.TestClass.Class.Name}.{testCase.TestMethod.Method.Name}",
                Id = GuidFromString(testCase.UniqueID)
            };
        }

        public static TestResult ToTestResult(this ITestResultMessage result, TestOutcome outcome)
        {
            var tr = new TestResult(result.TestCase.ToTest())
            {
                Outcome = outcome,
                Duration = TimeSpan.FromSeconds((double)result.ExecutionTime),
                ErrorMessage = Conditional(result, (IFailureInformation r) => string.Join(Environment.NewLine, r.Messages)),
                ErrorStackTrace = Conditional(result, (IFailureInformation r) => string.Join(Environment.NewLine, r.StackTraces))
            };

            tr.Messages.Add(result.Output);

            return tr;
        }

        private static Guid GuidFromString(string data)
        {
            var hash = _hash.ComputeHash(Encoding.Unicode.GetBytes(data));
            var b = new byte[16];
            Array.Copy((Array)hash, (Array)b, 16);
            return new Guid(b);
        }

        private static TResult Conditional<TMessage, TResult>(object message, Func<TMessage, TResult> func)
            where TMessage : class
            where TResult : class
        {
            var msg = message as TMessage;
            return msg == null ? null : func(msg);
        }
    }
}