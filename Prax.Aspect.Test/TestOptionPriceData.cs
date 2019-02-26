using System;
using FluentAssertions;
using Prax.Utils;
using static LanguageExt.Prelude;
using Xunit;

namespace Prax.Aspect.Test
{
    public class TestOptionPriceData {
        private static OptionInstrument CreateFakeInstrument() {
            return new OptionInstrument(OptionInstrumentType.ETO, "fakeName", "FAK");
        }

        private static OptionPriceData CreateFakeOptionPriceData(DateTime tradeDate) {
            var dt = new DateTime(2112, 1, 1, 2, 3, 4);
            return new OptionPriceData(
                CreateFakeInstrument(), OptionType.Put, tradeDate, stripDate: dt, expirationDate: Some(dt),
                settlementPrice: 2.34m, strikePrice: 123m, pricingGroup: "iamapricinggroup", isBalMoOrCso: false);
        }

        [Fact]
        public void OptionPriceDataIsTradeDateGivenDayOrPreviousDayShouldCorrectlyIdentifyOldTradeDates() {
            // For this tradeData, a "today" value of the 12th or earlier should return true
            var tradeDate = new DateTime(2018, 5, 9, 17, 29, 25);
            var bhl = new BankHolidayLookup(dt => false);
            var optionPriceData = CreateFakeOptionPriceData(tradeDate);
            optionPriceData.IsTradeDateMoreRecentThanPreviousWorkingDay(new DateTime(2018, 5, 10, 00, 00, 01), bhl).Should().BeTrue();
            optionPriceData.IsTradeDateMoreRecentThanPreviousWorkingDay(new DateTime(2018, 5, 10), bhl).Should().BeTrue();
            optionPriceData.IsTradeDateMoreRecentThanPreviousWorkingDay(new DateTime(2018, 5, 9, 23, 59, 59), bhl).Should().BeTrue();
            optionPriceData.IsTradeDateMoreRecentThanPreviousWorkingDay(new DateTime(2018, 5, 9), bhl).Should().BeTrue();
            optionPriceData.IsTradeDateMoreRecentThanPreviousWorkingDay(new DateTime(2017, 6, 1, 23, 59, 59), bhl).Should().BeTrue();
            optionPriceData.IsTradeDateMoreRecentThanPreviousWorkingDay(new DateTime(2017, 6, 1), bhl).Should().BeTrue();

            // The 13th or later for "today" means that the trade date is > 2 days old
            optionPriceData.IsTradeDateMoreRecentThanPreviousWorkingDay(new DateTime(2018, 5, 13, 00, 00, 01), bhl).Should().BeFalse();
            optionPriceData.IsTradeDateMoreRecentThanPreviousWorkingDay(new DateTime(2018, 5, 13), bhl).Should().BeFalse();
            optionPriceData.IsTradeDateMoreRecentThanPreviousWorkingDay(new DateTime(2019, 1, 1, 00, 00, 01), bhl).Should().BeFalse();
            optionPriceData.IsTradeDateMoreRecentThanPreviousWorkingDay(new DateTime(2019, 1, 1), bhl).Should().BeFalse();
        }

        [Fact]
        public void OptionPriceUploadOnMondayShouldNotThinkFridayPricesAreStale() {
            // Ideally this shouldn't be needed as the auto-download/upload should happen on the day of publish, which
            // should be Friday/Saturday for the Friday settlement prices. However a weekend failure and a restart
            // is sadly all too possible so handle this as a known edge case.
            var bhl = new BankHolidayLookup(dt => false);
            var tradeDate = new DateTime(2018, 5, 11); // Friday
            var followingMonday = new DateTime(2018, 5, 14, 9, 0, 0); // Today == the following Monday
            var optionPriceData = CreateFakeOptionPriceData(tradeDate);
            optionPriceData.IsTradeDateMoreRecentThanPreviousWorkingDay(followingMonday, bhl).Should().BeTrue();
        }

        [Fact]
        public void OptionPriceOnMondayShouldNotThinkThursdayPricesAreStaleIfFridayIsABankHoliday() {
            var tradeDate = new DateTime(2018, 5, 10); // Thursday
            var friday = new DateTime(2018, 5, 11);
            var bhl = new BankHolidayLookup(dt => dt == friday);
            var followingMonday = new DateTime(2018, 5, 14, 9, 0, 0); // Today == the following Monday
            var optionPriceData = CreateFakeOptionPriceData(tradeDate);
            optionPriceData.IsTradeDateMoreRecentThanPreviousWorkingDay(followingMonday, bhl).Should().BeTrue();
        }
    }
}
