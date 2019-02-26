using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Xunit;
using static LanguageExt.Prelude;

namespace Prax.Uploader.Test
{
    [ExcludeFromCodeCoverage]
    public class UploadUtilsTests {
        [Fact]
        public void SettlementPriceWithExpiryOfTodayShouldStillBeUploaded() {
            // This is based on an issue that we ran into in production, where the settlement prices for Ice Brent Jul18
            // weren't uploaded from the settlement file of the 24th but should have been (expiry 25th).
            var today = new DateTime(2018, 5, 25);
            var tradeDate = new DateTime(2018, 5, 24);
            var stripDate = new DateTime(2018, 7, 1);
            const bool isBalMoOrCso = false;

            UploadUtils.ArePriceDatesValidForUpload(today, today, tradeDate, stripDate, isBalMoOrCso).Should().BeTrue();
        }

        [Fact]
        public void SettlementPriceWithTradeDateGreaterThanExpiryShouldNotBeValid() {
            var today = new DateTime(2018, 5, 25);
            var tradeDate = new DateTime(2018, 5, 25);
            var expiry = new DateTime(2018, 5, 24);
            var stripDate = new DateTime(2018, 7, 1);
            const bool isBalMoOrCso = false;

            UploadUtils.ArePriceDatesValidForUpload(today, expiry, tradeDate, stripDate, isBalMoOrCso).Should().BeFalse();
        }

        [Fact]
        public void SettlementPriceWithExpiryLessThanTodayShouldNotBeValid() {
            var today = new DateTime(2018, 5, 25);
            var tradeDate = new DateTime(2018, 5, 24);
            var expiry = new DateTime(2018, 5, 24);
            var stripDate = new DateTime(2018, 7, 1);
            const bool isBalMoOrCso = false;

            UploadUtils.ArePriceDatesValidForUpload(today, expiry, tradeDate, stripDate, isBalMoOrCso).Should().BeFalse();
        }

        [Fact]
        public void NonBalMoOrCsoWithStripDateLessThanOrEqualToStartOfCurrentMonthShouldNotBeValid() {
            var today = new DateTime(2018, 5, 25);
            var tradeDate = new DateTime(2018, 5, 24);
            var expiry = new DateTime(2018, 5, 31);
            var stripDate = new DateTime(2018, 5, 1);
            const bool isBalMoOrCso = false;

            UploadUtils.ArePriceDatesValidForUpload(today, expiry, tradeDate, stripDate, isBalMoOrCso).Should().BeFalse();
        }

        [Fact]
        public void BalMoOrCsoWithStripDateEqualToCurrentMonthShouldBeValid() {
            var today = new DateTime(2018, 5, 25);
            var tradeDate = new DateTime(2018, 5, 24);
            var expiry = new DateTime(2018, 5, 31);
            var stripDate = new DateTime(2018, 5, 1);
            const bool isBalMoOrCso = true;

            UploadUtils.ArePriceDatesValidForUpload(today, expiry, tradeDate, stripDate, isBalMoOrCso).Should().BeTrue();
        }

        [Fact]
        public void IfNoExpiryDateThenValidityDeterminedByStipDateAndIsBalMoOrCsoFlag() {
            var today = new DateTime(2018, 5, 25);
            var tradeDate = new DateTime(2018, 5, 24);
            var testCases = List(
                (new DateTime(2018, 6, 1), false, true),
                (new DateTime(2018, 6, 1), true, true),
                (new DateTime(2018, 5, 1), false, false),
                (new DateTime(2018, 5, 1), true, true),
                (new DateTime(2018, 4, 1), false, false),
                (new DateTime(2018, 4, 1), true, false)
            );
            foreach (var (stripDate, isBalMoOrCso, expected) in testCases) {
                UploadUtils.ArePriceDatesValidForUpload(today, None, tradeDate, stripDate, isBalMoOrCso).Should().Be(expected);
            }
        }

        [Theory]
        [InlineData("p", true)]
        [InlineData("P", true)]
        [InlineData("put", true)]
        [InlineData("Put", true)]
        [InlineData("c", true)]
        [InlineData("C", true)]
        [InlineData("call", true)]
        [InlineData("Call", true)]
        [InlineData("F", false)]
        [InlineData("f", false)]
        [InlineData("nonsense", false)]
        [InlineData("we must perform a quirkafleeg", false)]
        [InlineData("", false)]
        [InlineData((string)null, false)]
        public void IsPutOrCallShouldCorrectlyDetermineIfAStringRepresentsAPutOrACall(string s, bool expected) {
            UploadUtils.IsPutOrCall(s).Should().Be(expected);
        }
    }
}
