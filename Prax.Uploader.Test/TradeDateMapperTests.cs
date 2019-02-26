using System;
using System.Collections.Generic;
using FakeItEasy;
using FluentAssertions;
using Prax.Utils;
using Xunit;

namespace Prax.Uploader.Test
{
    public class TradeDateMapperTests {
        
        public static IEnumerable<object[]> AsIsTestCases() {
            yield return new object[] {new DateTime(2018, 5, 10), new DateTime(2018, 5, 10)};
            yield return new object[] {new DateTime(2018, 5, 11), new DateTime(2018, 5, 11)};
            yield return new object[] {new DateTime(2018, 5, 14), new DateTime(2018, 5, 14)};
            yield return new object[] {new DateTime(2018, 5, 15), new DateTime(2018, 5, 15)};
        }
        [Theory]
        [MemberData(nameof(AsIsTestCases))]
        public void MappingAsIsShouldAlwaysReturnTheSameDate(DateTime dt, DateTime expected) {
            var fakeDtp = A.Fake<IDateTimeProvider>();
            var sut = new TradeDateMapper(TradeDateMapping.AsInFile, fakeDtp);
            sut.MapTradeDate(dt).Should().Be(expected);
        }

        public static IEnumerable<object[]> NextWorkingDayTestCases() {
            yield return new object[] {new DateTime(2018, 5, 10), new DateTime(2018, 5, 11)};
            yield return new object[] {new DateTime(2018, 5, 11), new DateTime(2018, 5, 14)};
            yield return new object[] {new DateTime(2018, 5, 14), new DateTime(2018, 5, 15)};
            yield return new object[] {new DateTime(2018, 5, 15), new DateTime(2018, 5, 16)};
        }
        [Theory]
        [MemberData(nameof(NextWorkingDayTestCases))]
        public void MappingNextWorkingDayShouldAvoidWeekends(DateTime dt, DateTime expected) {
            var fakeDtp = A.Fake<IDateTimeProvider>();
            var sut = new TradeDateMapper(TradeDateMapping.NextWorkingDay, fakeDtp);
            sut.MapTradeDate(dt).Should().Be(expected);
        }

        [Fact]
        public void MappingToTodayShouldAlwaysReturnToday() {
            var dt = new DateTime(2018, 5, 24);
            var fakeDtp = A.Fake<IDateTimeProvider>();
            var sut = new TradeDateMapper(TradeDateMapping.Today, fakeDtp);

            var today1 = new DateTime(2018, 1, 1);
            A.CallTo(() => fakeDtp.Today).Returns(today1);
            sut.MapTradeDate(dt).Should().Be(today1);

            var today2 = new DateTime(2018, 11, 30);
            A.CallTo(() => fakeDtp.Today).Returns(today2);
            sut.MapTradeDate(dt).Should().Be(today2);
        }
    }
}
