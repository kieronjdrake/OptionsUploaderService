using System.Collections.Generic;
using FluentAssertions;
using LanguageExt.UnitTesting;
using Xunit;

namespace Prax.Utils.Test
{
    public class TestDictionaryExtensions {
        [Fact]
        public void FindShouldReturnSomeIfKeyPresentOrNoneIfNotPresent() {
            var d = new Dictionary<int, string>{ {1, "one"}, {2, "two"}};
            d.Find(1).ShouldBeSome(x => x.Should().Be("one"));
            d.Find(2).ShouldBeSome(x => x.Should().Be("two"));
            d.Find(3).ShouldBeNone();
            d.Find(2112).ShouldBeNone();
        }
    }
}
