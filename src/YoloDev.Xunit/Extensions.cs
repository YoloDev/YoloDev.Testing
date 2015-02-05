using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Framework.TestAdapter;
using Xunit.Abstractions;
using YoloDev.Xunit.Messages;

namespace YoloDev.Xunit
{
    public static class Extensions
    {
#if ASPNETCORE50
        private readonly static HashAlgorithm _hash = SHA1.Create();
#else
        private readonly static HashAlgorithm _hash = new SHA1Managed();
#endif

        public static TestWrapper ToTest(this ITestCase testCase)
        {
            return new Test
            {
                CodeFilePath = testCase.SourceInformation?.FileName,
                LineNumber = testCase.SourceInformation?.LineNumber,
                DisplayName = testCase.DisplayName,
                FullyQualifiedName = $"{testCase.TestMethod.TestClass.Class.Name}.{testCase.TestMethod.Method.Name}",
                Id = GuidFromString(testCase.UniqueID)
            }.Wrap();
        }

        public static TestResultWrapper ToTestResult(this ITestResultMessage result, TestOutcome outcome)
        {
            var tr = new TestResult(result.TestCase.ToTest().Wrapped)
            {
                Outcome = Convert(outcome),
                Duration = TimeSpan.FromSeconds((double)result.ExecutionTime),
                ErrorMessage = Conditional(result, (IFailureInformation r) => string.Join(Environment.NewLine, r.Messages)),
                ErrorStackTrace = Conditional(result, (IFailureInformation r) => string.Join(Environment.NewLine, r.StackTraces))
            };

            tr.Messages.Add(result.Output);

            return tr.Wrap();
        }

        private static Microsoft.Framework.TestAdapter.TestOutcome Convert(TestOutcome outcome)
        {
            switch (outcome)
            {
            case TestOutcome.None: return Microsoft.Framework.TestAdapter.TestOutcome.None;
            case TestOutcome.Passed: return Microsoft.Framework.TestAdapter.TestOutcome.Passed;
            case TestOutcome.Failed: return Microsoft.Framework.TestAdapter.TestOutcome.Failed;
            case TestOutcome.Skipped: return Microsoft.Framework.TestAdapter.TestOutcome.Skipped;
            case TestOutcome.NotFound: return Microsoft.Framework.TestAdapter.TestOutcome.NotFound;
            default: throw new ArgumentException("Unknown outcome enum value", "outcome");
            }
        }

        public static TestWrapper Wrap(this Test test)
        {
            return test == null ? null : new TestWrapper(test);
        }

        public static TestResultWrapper Wrap(this TestResult testResult)
        {
            return testResult == null ? null : new TestResultWrapper(testResult);
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