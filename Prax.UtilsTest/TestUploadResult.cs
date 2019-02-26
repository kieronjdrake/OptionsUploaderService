using FluentAssertions;
using Xunit;

namespace Prax.Utils.Test {
    public class TestUploadResult {
        [Fact]
        public void UploadResultSucceededReturnsTrueIfNoFailures() {
            UploadResult.AllSucceeded(3).Succeeded.Should().BeTrue();
            UploadResult.SingleSuccess().Succeeded.Should().BeTrue();
            UploadResult.SingleIgnored().Succeeded.Should().BeTrue();
            UploadResult.SingleFailure().Succeeded.Should().BeFalse();
            UploadResult.Empty().Succeeded.Should().BeTrue();
            new UploadResult(100, 50, 0).Succeeded.Should().BeTrue();
            new UploadResult(100, 50, 1).Succeeded.Should().BeFalse();
        }

        [Fact]
        public void UploadResultAdditionWorksAsExpected() {
            var r1 = new UploadResult(1, 2, 3);
            var r2 = new UploadResult(33, 22, 11);
            var (s, i, f) = r1 + r2;
            s.Should().Be(34);
            i.Should().Be(24);
            f.Should().Be(14);
        }
    }
}
