using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
// ReSharper disable MemberCanBePrivate.Global

namespace Prax.Utils.Test
{
    public class TestDateTimeExtensions {

        private static readonly BankHolidayLookup IsNotBankHoliday = new BankHolidayLookup(_ => false);

        [Fact]
        public void ToStartOfMonthReturnsNewDateAtStartOfMonth() {
            var dt = new DateTime(2018, 2, 28);
            var startOfMonth = dt.ToStartOfMonth();
            dt.Should().Be(new DateTime(2018, 2, 28));
            startOfMonth.Should().Be(new DateTime(2018, 2, 1));
        }

        public static IEnumerable<object[]> CreateIsWeekendTestCases() {
            for (int day = 7; day <= 13; ++day)
                yield return new object[] {new DateTime(2018, 5, day), (day == 12 || day == 13)};
        }

        [Theory]
        [MemberData(nameof(CreateIsWeekendTestCases))]
        public void IsWeekendWorksAsExpected(DateTime dt, bool expectedIsWeekend) {
            dt.IsWeekend().Should().Be(expectedIsWeekend);
        }

        public static IEnumerable<object[]> CreatePreviousWorkingDayTestCases() {
            yield return new object[] {new DateTime(2018, 5, 10), new DateTime(2018, 5, 09)};
            yield return new object[] {new DateTime(2018, 5, 11), new DateTime(2018, 5, 10)};
            yield return new object[] {new DateTime(2018, 5, 12), new DateTime(2018, 5, 11)};
            yield return new object[] {new DateTime(2018, 5, 13), new DateTime(2018, 5, 11)};
            yield return new object[] {new DateTime(2018, 5, 14), new DateTime(2018, 5, 11)};
            yield return new object[] {new DateTime(2018, 5, 15), new DateTime(2018, 5, 14)};
        }
        [Theory]
        [MemberData(nameof(CreatePreviousWorkingDayTestCases))]
        public void PreviousWorkingDayShouldHandleWeekendsCorrectly(DateTime dt, DateTime expected) {
            dt.PreviousWorkingDay(IsNotBankHoliday).Should().Be(expected);
        }

        public static IEnumerable<object[]> CreateNextWorkingDayTestCases() {
            yield return new object[] {new DateTime(2018, 5, 10), new DateTime(2018, 5, 11)};
            yield return new object[] {new DateTime(2018, 5, 11), new DateTime(2018, 5, 14)};
            yield return new object[] {new DateTime(2018, 5, 14), new DateTime(2018, 5, 15)};
            yield return new object[] {new DateTime(2018, 5, 15), new DateTime(2018, 5, 16)};
        }
        [Theory]
        [MemberData(nameof(CreateNextWorkingDayTestCases))]
        public void NextWorkingDayShouldHandleWeekendsCorrectly(DateTime dt, DateTime expected) {
            dt.NextWorkingDay(IsNotBankHoliday).Should().Be(expected);
        }


        public static IEnumerable<object[]> CreateAdjustToWorkingDayTestCases() {
            yield return new object[] {new DateTime(2018, 5, 11), new DateTime(2018, 5, 11)};
            yield return new object[] {new DateTime(2018, 5, 12), new DateTime(2018, 5, 14)};
            yield return new object[] {new DateTime(2018, 5, 13), new DateTime(2018, 5, 14)};
            yield return new object[] {new DateTime(2018, 5, 14), new DateTime(2018, 5, 14)};
            yield return new object[] {new DateTime(2018, 5, 15), new DateTime(2018, 5, 15)};
        }
        [Theory]
        [MemberData(nameof(CreateAdjustToWorkingDayTestCases))]
        public void AdjustToWorkingDayShouldHandleWeekendsCorrectly(DateTime dt, DateTime expected) {
            dt.AdjustToWorkingDay(IsNotBankHoliday).Should().Be(expected);
        }

        [Fact]
        public void NextWorkingDayShouldHandleBankHolidays() {
            var isBankHoliday = new BankHolidayLookup(x => x <= new DateTime(2018, 5, 10));
            var dt = new DateTime(2018, 5, 10);
            var firstNonWeekendDayAfterBankHoliday = new DateTime(2018, 5, 11);
            dt.NextWorkingDay(isBankHoliday).Should().Be(firstNonWeekendDayAfterBankHoliday);
        }

        [Fact]
        public void NextWorkingDayShouldHandleBankHolidaysAsWellAsWeekends() {
            var isBankHoliday = new BankHolidayLookup(x => x <= new DateTime(2018, 5, 11));
            var dt = new DateTime(2018, 5, 10);
            var mondayAfterWeekend = new DateTime(2018, 5, 14);
            dt.NextWorkingDay(isBankHoliday).Should().Be(mondayAfterWeekend);
        }
    }
}
