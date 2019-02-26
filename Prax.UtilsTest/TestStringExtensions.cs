using FluentAssertions;
using static LanguageExt.Prelude;
using Xunit;

namespace Prax.Utils.Test {
    public class TestStringExtensions {
        [Fact]
        public void ContainsAnyOfIsTrueIfMessageContainsAnyMembersOfALst() {
            "oh that this too too solid flesh".ContainsAnyOf(List("this", "should", "be", "true")).Should().BeTrue();
            "as should this".ContainsAnyOf(List("should", "be", "true")).Should().BeTrue();
            "this one should fail".ContainsAnyOf(List("correct", "horse", "battery", "staple")).Should().BeFalse();
            "Is case sensitive".ContainsAnyOf(List("is", "Case", "Sensitive")).Should().BeFalse();
        }

        [Fact]
        public void ContainsAnyOfIsTrueIfMessageContainsAnyMembersOfAnEnumerable() {
            "oh that this too too solid flesh".ContainsAnyOf(new[] {"this", "should", "be", "true"}).Should().BeTrue();
            "as should this".ContainsAnyOf(new[] {"should", "be", "true"}).Should().BeTrue();
            "this one should fail".ContainsAnyOf(new[] {"correct", "horse", "battery", "staple"}).Should().BeFalse();
            "Is case sensitive".ContainsAnyOf(new[] {"is", "Case", "Sensitive"}).Should().BeFalse();
        }
    }
}
