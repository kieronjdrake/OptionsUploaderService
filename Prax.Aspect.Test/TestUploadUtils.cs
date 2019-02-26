using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Prax.Aspect.Test {
    public class TestUploadUtils {
        [Fact]
        public void ToBulkPriceDataConcatenatesPriceDataInExpectedWay() {
            var prices = new [] {
                new addOptionPriceInstrumentprice {
                    price = 12.34, day = new DateTime(2018, 09, 03), iscall = true, strikeprice = 23.45
                },
                new addOptionPriceInstrumentprice {
                    price = 12.34, day = new DateTime(2018, 09, 04), iscall = true, strikeprice = 23.45
                },
                new addOptionPriceInstrumentprice {
                    price = 13.34, day = new DateTime(2018, 09, 03), iscall = false, strikeprice = 24.56
                }
            };
            var priceData = UploadUtils.ToBulkPriceData(prices);
            priceData.Should().Be(
                "2018-09-03;12.34;23.45;true;2018-09-04;12.34;23.45;true;2018-09-03;13.34;24.56;false");
        }

        [Fact]
        public void SplitForBulkUploadSplitsPricesIntoExpectedGroups() {
            var instrument1 = new OptionInstrument(OptionInstrumentType.EO, "EO1_name", "EO1");
            var instrument2 = new OptionInstrument(OptionInstrumentType.EO, "EO2_name", "EO2");
            var instrument3 = new OptionInstrument(OptionInstrumentType.ETO, "ETO1_name", "ETO1");
            var stripDate1 = new DateTime(2018, 10, 01);
            var stripDate2 = new DateTime(2018, 11, 01);
            var pricingGroup1 = "pricingGroup1";
            var pricingGroup2 = "pricingGroup2";
            var expirationDate1 = new DateTime(2018, 11, 30);
            var expirationDate2 = new DateTime(2018, 12, 31);

            var data1 = new [] {
                new {tradeDate = new DateTime(2018, 09, 03), price = 12.34m, strike = 69m, porc = OptionType.Put},
                new {tradeDate = new DateTime(2018, 09, 03), price = 12.34m, strike = 69m, porc = OptionType.Call},
                new {tradeDate = new DateTime(2018, 09, 04), price = 13.34m, strike = 71m, porc = OptionType.Put}
            };
            var data2 = data1.Skip(1).ToList();

            var chunk1 = data1.Select(d => new OptionPriceData(instrument1, d.porc, d.tradeDate, stripDate1,
                                                               expirationDate1, d.price, d.strike, pricingGroup1, false));
            var chunk2 = data1.Select(d => new OptionPriceData(instrument2, d.porc, d.tradeDate, stripDate1,
                                                               expirationDate1, d.price, d.strike, pricingGroup1, false));
            var chunk3 = data1.Select(d => new OptionPriceData(instrument3, d.porc, d.tradeDate, stripDate1,
                                                               expirationDate1, d.price, d.strike, pricingGroup1, false));
            var chunk4 = data2.Select(d => new OptionPriceData(instrument1, d.porc, d.tradeDate, stripDate2,
                                                               expirationDate2, d.price, d.strike, pricingGroup2, false));
            var chunk5 = data2.Select(d => new OptionPriceData(instrument2, d.porc, d.tradeDate, stripDate2,
                                                               expirationDate2, d.price, d.strike, pricingGroup2, false));

            var allPrices = new[] {chunk1, chunk2, chunk3, chunk4, chunk5}.SelectMany(xs => xs).ToList();

            var split = UploadUtils.SplitForBulkUpload(allPrices);

            // Ensure the price details aren't changed
            split.Sum(xs => xs.Count).Should().Be(allPrices.Count);
            split.Sum(xs => xs.Count(x => x.Instrument.Equals(instrument1))).Should().Be(5);
            split.Sum(xs => xs.Count(x => x.Instrument.Equals(instrument2))).Should().Be(5);
            split.Sum(xs => xs.Count(x => x.Instrument.Equals(instrument3))).Should().Be(3);
            split.Sum(xs => xs.Count(x => x.OptionType == OptionType.Put)).Should().Be(8);
            split.Sum(xs => xs.Count(x => x.OptionType == OptionType.Call)).Should().Be(5);

            // Ensure we have the correct groups
            split.Count.Should().Be(5);
            split.Count(xs => xs.Count == 3).Should().Be(3);
            split.Count(xs => xs.Count == 2).Should().Be(2);
        }
    }
}
