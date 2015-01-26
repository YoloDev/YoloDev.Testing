using System;
using Xunit;

namespace YoloDev.Xunit
{
    public class TestTest
    {
        [Fact]
        public void SomeTest()
        {

        }

        [Fact]
        public void Failing()
        {
            throw new Exception("I so fail");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void FailingMaybe(bool fail)
        {
            if (fail)
                throw new Exception("I should fail sometimes too...");
        }
    }
}