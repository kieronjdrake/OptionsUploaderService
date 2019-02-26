using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Prax.Utils.Test
{
    public class TestListExtensions {
        [Fact]
        public void SplitIntoTwoReturnsCorrectEqualSizedChunksForEvenSizedList() {
            var xs = new List<int> {1, 2, 3, 4};
            var (l1, l2) = xs.SplitIntoTwo();
            l1.Should().BeEquivalentTo(1, 2);
            l2.Should().BeEquivalentTo(3, 4);
        }

        [Fact]
        public void SplitIntoTwoReturnsLargerFirstListForOddSizedList() {
            var xs = new List<int> {1, 2, 3, 4, 5};
            var (l1, l2) = xs.SplitIntoTwo();
            l1.Should().BeEquivalentTo(1, 2, 3);
            l2.Should().BeEquivalentTo(4, 5);
        }

        [Fact]
        public void SplitIntoTwoGivenEmptyOrNullListReturnsTwoEmptyLists() {
            var (l11, l12) = new List<int>().SplitIntoTwo();
            l11.Should().BeEmpty();
            l12.Should().BeEmpty();
            var (l21, l22) = ((List<int>)null).SplitIntoTwo();
            l21.Should().BeEmpty();
            l22.Should().BeEmpty();
        }

        [Fact]
        public void SplitIntoTwoGivenListOfOneReturnsListOfOneAndEmptyList() {
            var (l1, l2) = new List<int> {42}.SplitIntoTwo();
            l1.Should().BeEquivalentTo(42);
            l2.Should().BeEmpty();
        }
    }
}
