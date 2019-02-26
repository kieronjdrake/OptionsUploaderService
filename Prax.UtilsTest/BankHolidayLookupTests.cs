using System;
using System.Collections.Generic;
using FluentAssertions;
using LanguageExt;
using Xunit;

namespace Prax.Utils.Test
{
    public class BankHolidayLookupTests {
        [Fact]
        public void BankHolidayLookupShouldReturnTheDatesAsGivenByTheUnderlyingLookupFunction() {
            var inputs = new Dictionary<DateTime, bool> {
                {new DateTime(2018, 01, 01), true},
                {new DateTime(2018, 01, 02), false},
                {new DateTime(2018, 01, 03), false},
                {new DateTime(2018, 03, 30), true}
            };
            var lookupFn = Prelude.fun((DateTime dt) => inputs[dt]);

            var sut = new BankHolidayLookup(lookupFn);

            foreach (var input in inputs) {
                sut.IsBankHoliday(input.Key).Should().Be(input.Value);
            }
        }

        [Fact]
        public void BankHolidayLookupShouldNotCallTheUnderlyingFunctionAgainForAnAlreadyLookedUpDate() {
            var callCount = 0;
            var lookupFn = Prelude.fun((DateTime dt) => {
                ++callCount;
                return false;
            });
            
            var sut = new BankHolidayLookup(lookupFn);

            var date = new DateTime(2018, 1, 2);
            sut.IsBankHoliday(date).Should().BeFalse();
            sut.IsBankHoliday(date).Should().BeFalse();
            callCount.Should().Be(1);
        }
    }
}
