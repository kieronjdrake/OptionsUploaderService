using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Prax.Utils.Test
{
    public class TestEnumerableExtensions {
        [Fact]
        public void SymmetricSetDifferenceCorrectlyIdentifiesLeftBothRight() {
            var list1 = new List<int> {1, 2, 3, 4};
            var list2 = new List<int> {7, 6, 5, 4, 3};
            var (left, both, right) = list1.SymmetricSetDifference(list2);
            left.Should().BeEquivalentTo(1, 2);
            both.Should().BeEquivalentTo(3, 4);
            right.Should().BeEquivalentTo(5, 6, 7);
        }

        [Fact]
        public void IsEmptyCorrectlyIdentifiesEmptyEnumerables() {
            new[] {1, 2}.IsEmpty().Should().BeFalse();
            Enumerable.Empty<int>().IsEmpty().Should().BeTrue();
            new List<int>().IsEmpty().Should().BeTrue();
            new List<int>{1}.IsEmpty().Should().BeFalse();
        }

        [Fact]
        public void IsNullOrEmptyShouldWorkForBothEmptyAndNullEnumerables() {
            new[] {1, 2}.IsNullOrEmpty().Should().BeFalse();
            new List<int>().IsNullOrEmpty().Should().BeTrue();
            ((List<int>)null).IsNullOrEmpty().Should().BeTrue();
        }

        [Fact]
        public void ConsShouldPrependElementToEnumerable() {
            EnumerableExtensions.Cons(1, new[] {1, 2}).Should().BeEquivalentTo(1, 1, 2);
            EnumerableExtensions.Cons(1, Enumerable.Empty<int>()).Should().BeEquivalentTo(1);
        }

        [Fact]
        public void PartitionShouldSplitBasedOnPredicate() {
            var xs = new[] {1, 2, 3, 4, 5};
            var (trues, falses) = xs.Partition(x => x % 2 == 0);
            trues.Should().BeEquivalentTo(2, 4);
            falses.Should().BeEquivalentTo(1, 3, 5);
        }

        [Fact]
        public void PartitionShouldSplitCorrectlyWhereAllElementsAreTruthy() {
            var xs = new[] {1, 2, 3, 4, 5};
            var (trues, falses) = xs.Partition(x => x < 100);
            trues.Should().BeEquivalentTo(xs);
            falses.Should().BeEmpty();
        }

        [Fact]
        public void PartitionShouldSplitCorrectlyWhereAllElementsAreFalsy() {
            var xs = new[] {1, 2, 3, 4, 5};
            var (trues, falses) = xs.Partition(x => x > 100);
            trues.Should().BeEmpty();
            falses.Should().BeEquivalentTo(xs);
        }
    }
}
