using System;
using System.Collections.Generic;
using System.Linq;
using FakeItEasy;
using FluentAssertions;
using static LanguageExt.Prelude;
using Prax.Aspect;
using Prax.Utils;
using Xunit;
// ReSharper disable MemberCanBePrivate.Global

namespace Prax.Uploader.Test
{
    public class InputMapperTests {
        private static readonly OptionInstrument FakeOi1 = new OptionInstrument(OptionInstrumentType.ETO, "FakeOi1", "FK1");
        private static readonly OptionInstrument FakeOi2 = new OptionInstrument((OptionInstrumentType.ETO, "FakeOi2", "FK2"));
        private static readonly OptionInstrument FakeOi3 = new OptionInstrument(OptionInstrumentType.EO, "FakeOi3", "FK3");
        private static readonly OptionInstrument FakeOi4 = new OptionInstrument(OptionInstrumentType.AO, "FakeOi4", "FK4");

        private static readonly List<OptionInstrument> FakeInstruments = new List<OptionInstrument> {
            FakeOi1,
            FakeOi2,
            FakeOi3,
            FakeOi4
        };

        private static readonly Func<List<OptionInstrument>> GetFakeInstruments = () => FakeInstruments;

        private const string DefaultPricingGroup = "DefaultPricingGroup__SYSTEM_in_Aspect";
        
        
        public interface IBankHolidayLookup {
            bool IsBankHoliday(DateTime dt);
        }

        private static readonly BankHolidayLookup NoBankHolidays = new BankHolidayLookup(dt => false);
        private static BankHolidayLookup CreateFakeBhl(int numBankHols) {
            var bhlImpl = A.Fake<IBankHolidayLookup>();
            if (numBankHols > 0) {
                A.CallTo(() => bhlImpl.IsBankHoliday(A<DateTime>.Ignored))
                 .Returns(true).NumberOfTimes(numBankHols).Then.Returns(false);
            } else {
                A.CallTo(() => bhlImpl.IsBankHoliday(A<DateTime>.Ignored)).Returns(false);
            }
            return new BankHolidayLookup(dt => bhlImpl.IsBankHoliday(dt));
        }

        [Fact]
        public void MapShouldCorrectlyMapInstrumentIfInOptionInstrumentList() {
            var input1 = new InputPriceData(
                FakeOi1.Code, "C", None, new DateTime(2018, 5, 24), new DateTime(2018, 8, 1),
                new DateTime(2018, 5, 25), 0.12m, 71m, false);
            var input2 = new InputPriceData(
                FakeOi2.Code, "P", None, new DateTime(2018, 5, 24), new DateTime(2018, 9, 1),
                new DateTime(2018, 5, 25), 0.23m, 73.5m, false);
            var input3 = new InputPriceData(
                FakeOi3.Code, "P", "AnotherPricingGroup", new DateTime(2018, 5, 24),
                new DateTime(2018, 9, 1), None, 0.23m, 73.5m, false);
            var input4 = new InputPriceData(
                FakeOi4.Code, "C", None, new DateTime(2018, 5, 24),
                new DateTime(2018, 9, 1), None, 0.34m, 74.5m, false);

            var inputData = new List<InputPriceData> {input1, input2, input3, input4};

            var fakeDtp = A.Fake<IDateTimeProvider>();
            var tradeDateMapper = new TradeDateMapper(TradeDateMapping.AsInFile, fakeDtp, NoBankHolidays);
            var mapped = InputMapper.Map(inputData, DefaultPricingGroup, GetFakeInstruments, tradeDateMapper).ToList();

            mapped.Count.Should().Be(inputData.Count);

            var mapped1 = mapped.First(opd => opd.Instrument.Name == FakeOi1.Name);
            mapped1.Instrument.Should().BeEquivalentTo(FakeOi1);
            mapped1.OptionType.Should().Be(OptionType.Call);
            mapped1.TradeDate.Should().Be(input1.TradeDate);
            mapped1.StripDate.Should().Be(input1.StripDate);
            mapped1.ExpirationDate.ShouldBe(input1.ExpirationDate);
            mapped1.SettlementPrice.Should().Be(input1.SettlementPrice);
            mapped1.StrikePrice.Should().Be(input1.StrikePrice);
            mapped1.PricingGroup.Should().Be(DefaultPricingGroup);

            var mapped2 = mapped.First(opd => opd.Instrument.Name == FakeOi2.Name);
            mapped2.Instrument.Should().BeEquivalentTo(FakeOi2);
            mapped2.OptionType.Should().Be(OptionType.Put);
            mapped2.TradeDate.Should().Be(input2.TradeDate);
            mapped2.StripDate.Should().Be(input2.StripDate);
            mapped2.ExpirationDate.ShouldBe(input2.ExpirationDate);
            mapped2.SettlementPrice.Should().Be(input2.SettlementPrice);
            mapped2.StrikePrice.Should().Be(input2.StrikePrice);
            mapped2.PricingGroup.Should().Be(DefaultPricingGroup);

            var mapped3 = mapped.First(opd => opd.Instrument.Name == FakeOi3.Name);
            mapped3.Instrument.Should().BeEquivalentTo(FakeOi3);
            mapped3.OptionType.Should().Be(OptionType.Put);
            mapped3.TradeDate.Should().Be(input3.TradeDate);
            mapped3.StripDate.Should().Be(input3.StripDate);
            mapped3.ExpirationDate.ShouldBe(input3.ExpirationDate);
            mapped3.SettlementPrice.Should().Be(input3.SettlementPrice);
            mapped3.StrikePrice.Should().Be(input3.StrikePrice);
            mapped3.PricingGroup.Should().Be((string)input3.PricingGroup);

            var mapped4 = mapped.First(opd => opd.Instrument.Name == FakeOi4.Name);
            mapped4.Instrument.Should().BeEquivalentTo(FakeOi4);
            mapped4.OptionType.Should().Be(OptionType.Call);
            mapped4.TradeDate.Should().Be(input4.TradeDate);
            mapped4.StripDate.Should().Be(input4.StripDate);
            mapped4.ExpirationDate.ShouldBe(input4.ExpirationDate);
            mapped4.SettlementPrice.Should().Be(input4.SettlementPrice);
            mapped4.StrikePrice.Should().Be(input4.StrikePrice);
            mapped4.PricingGroup.Should().Be(DefaultPricingGroup);
        }

        [Fact]
        public void MapShouldNotMapInstrumentIfNotInOptionInstrumentList() {
            var input1 = new InputPriceData(
                FakeOi1.Code, "C", None, new DateTime(2018, 5, 24), new DateTime(2018, 8, 1),
                new DateTime(2018, 5, 25), 0.12m, 71m, false);
            var input2 = new InputPriceData(
                "ShouldNotBeFound", "C", None, new DateTime(2018, 5, 24), new DateTime(2018, 8, 1),
                new DateTime(2018, 5, 25), 0.12m, 71m, false);

            var inputData = new List<InputPriceData> {input1, input2};

            var fakeDtp = A.Fake<IDateTimeProvider>();
            var tradeDateMapper = new TradeDateMapper(TradeDateMapping.AsInFile, fakeDtp, NoBankHolidays);
            var mapped = InputMapper.Map(inputData, DefaultPricingGroup, GetFakeInstruments, tradeDateMapper).ToList();

            mapped.Count.Should().Be(1);
            mapped.First().Instrument.Should().BeEquivalentTo(FakeOi1);
        }

        [Fact]
        public void GetOptionInstrumentsShouldNotBeCalledIfThereIsNoInputDataToMap() {
            var func = A.Fake<Func<List<OptionInstrument>>>();
            var fakeDtp = A.Fake<IDateTimeProvider>();
            var tradeDateMapper = new TradeDateMapper(TradeDateMapping.AsInFile, fakeDtp, NoBankHolidays);

            var mapped = InputMapper.Map(Enumerable.Empty<InputPriceData>(), DefaultPricingGroup,
                                         GetFakeInstruments, tradeDateMapper).ToList();

            mapped.Should().BeEmpty();
            A.CallTo(() => func.Invoke()).MustNotHaveHappened();
        }

        [Fact]
        public void MapShouldCorrectlyMapTradeDateToTodayIfThatMappingSpecified() {
            var input1 = new InputPriceData(
                FakeOi1.Code, "C", None, new DateTime(2018, 5, 24), new DateTime(2018, 8, 1),
                new DateTime(2018, 5, 25), 0.12m, 71m, false);
            var inputData = new[] {input1};

            var fakeToday = new DateTime(2112, 1, 1);
            var fakeDtp = A.Fake<IDateTimeProvider>();
            A.CallTo(() => fakeDtp.Today).Returns(fakeToday);
            var tradeDateMapper = new TradeDateMapper(TradeDateMapping.Today, fakeDtp, NoBankHolidays);

            var mapped = InputMapper.Map(inputData, DefaultPricingGroup, GetFakeInstruments, tradeDateMapper).ToList();

            mapped.Count.Should().Be(1);
            mapped.First().TradeDate.Should().Be(fakeToday);
        }

        public static IEnumerable<object[]> NextWorkingDayTestCases() {
            yield return new object[] {new DateTime(2018, 06, 01), 0, new DateTime(2018, 06, 04)};
            yield return new object[] {new DateTime(2018, 06, 04), 0, new DateTime(2018, 06, 05)};
            yield return new object[] {new DateTime(2018, 06, 04), 1, new DateTime(2018, 06, 06)};
            yield return new object[] {new DateTime(2018, 06, 04), 2, new DateTime(2018, 06, 07)};
        }

        [Theory]
        [MemberData(nameof(NextWorkingDayTestCases))]
        public void MapShouldCreateNextWorkingDayIfSpecified(DateTime tradeDate, int numBankHols, DateTime expected) {
            var input1 = new InputPriceData(
                FakeOi1.Code, "C", None,
                tradeDate,
                new DateTime(2018, 8, 1), new DateTime(2018, 5, 25), 0.12m, 71m, false);
            var inputData = new[] {input1};

            var bhl = CreateFakeBhl(numBankHols);

            var fakeDtp = A.Fake<IDateTimeProvider>();
            var tradeDateMapper = new TradeDateMapper(TradeDateMapping.NextWorkingDay, fakeDtp, bhl);

            var mapped = InputMapper.Map(inputData, DefaultPricingGroup, GetFakeInstruments, tradeDateMapper).ToList();

            mapped.Count.Should().Be(1);
            var mappedDates = mapped.Select(opd => opd.TradeDate).ToList();
            mappedDates.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public void MapShouldCreateAPriceForTheTradeDateAndTodayIfAsInFileAndTodayIsSpecified() {
            var input1 = new InputPriceData(
                FakeOi1.Code, "C", None, new DateTime(2018, 5, 24), new DateTime(2018, 8, 1),
                new DateTime(2018, 5, 25), 0.12m, 71m, false);
            var inputData = new[] {input1};

            var fakeToday = new DateTime(2112, 1, 1);
            var fakeDtp = A.Fake<IDateTimeProvider>();
            A.CallTo(() => fakeDtp.Today).Returns(fakeToday);
            var tradeDateMapper = new TradeDateMapper(TradeDateMapping.AsInFileAndToday, fakeDtp, NoBankHolidays);

            var mapped = InputMapper.Map(inputData, DefaultPricingGroup, GetFakeInstruments, tradeDateMapper).ToList();

            mapped.Count.Should().Be(2);
            var mappedDates = mapped.Select(opd => opd.TradeDate).ToList();
            mappedDates.Should().BeEquivalentTo(fakeToday, input1.TradeDate);
        }

        [Theory]
        [MemberData(nameof(NextWorkingDayTestCases))]
        public void MapShouldCreateAPriceForTheTradeDateAndNextWorkingDayIfAsInFileAndTodayAndTradeDateIsToday(
            DateTime today, int numBankHols, DateTime expected) {

            var input1 = new InputPriceData(
                FakeOi1.Code, "C", None,
                today, // TradeDate
                new DateTime(2018, 8, 1), new DateTime(2018, 5, 25), 0.12m, 71m, false);
            var inputData = new[] {input1};

            var fakeDtp = A.Fake<IDateTimeProvider>();
            A.CallTo(() => fakeDtp.Today).Returns(today);
            var tradeDateMapper = new TradeDateMapper(TradeDateMapping.AsInFileAndToday, fakeDtp, CreateFakeBhl(numBankHols));

            var mapped = InputMapper.Map(inputData, DefaultPricingGroup, GetFakeInstruments, tradeDateMapper).ToList();

            mapped.Count.Should().Be(2);
            var mappedDates = mapped.Select(opd => opd.TradeDate).ToList();
            mappedDates.Should().BeEquivalentTo(expected, input1.TradeDate);
        }

        [Fact]
        public void TradeDateMapperWillThrowIfGivenInvalidMappingType() {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new TradeDateMapper((TradeDateMapping)123456, A.Fake<IDateTimeProvider>(), NoBankHolidays));
        }

        public static IEnumerable<object[]> TodayAtWeekendTestCases() {
            yield return new object[] {new DateTime(2018, 06, 01), new DateTime(2018, 06, 01)};
            yield return new object[] {new DateTime(2018, 06, 02), new DateTime(2018, 06, 04)};
            yield return new object[] {new DateTime(2018, 06, 03), new DateTime(2018, 06, 04)};
            yield return new object[] {new DateTime(2018, 06, 04), new DateTime(2018, 06, 04)};
            yield return new object[] {new DateTime(2018, 06, 05), new DateTime(2018, 06, 05)};
            // Add public holiday test cases here if we ever incorporate holiday calendars
        }

        [Theory]
        [MemberData(nameof(TodayAtWeekendTestCases))]
        public void MapCreatesPriceForNextWorkingDayIfTodayIsAWeekendIfTodaySpecified(DateTime today, DateTime expected) {
            var input1 = new InputPriceData(
                FakeOi1.Code, "C", None,
                new DateTime(2018, 6, 1),
                new DateTime(2018, 8, 1), new DateTime(2018, 5, 25), 0.12m, 71m, false);
            var inputData = new[] {input1};

            var fakeDtp = A.Fake<IDateTimeProvider>();
            A.CallTo(() => fakeDtp.Today).Returns(today);
            var tradeDateMapper = new TradeDateMapper(TradeDateMapping.Today, fakeDtp, NoBankHolidays);

            var mapped = InputMapper.Map(inputData, DefaultPricingGroup, GetFakeInstruments, tradeDateMapper).ToList();

            mapped.Count.Should().Be(1);
            var mappedDates = mapped.Select(opd => opd.TradeDate).ToList();
            mappedDates.Should().BeEquivalentTo(expected);
        }

        [Theory]
        [MemberData(nameof(TodayAtWeekendTestCases))]
        public void MapCreatesPriceForNextWorkingDayAndTradeDateIfTodayAtWeekendAndAsInFileAndTodaySpecified(
            DateTime today, DateTime expected) {

            var input1 = new InputPriceData(
                FakeOi1.Code, "C", None,
                new DateTime(2018, 5, 24),
                new DateTime(2018, 8, 1), new DateTime(2018, 5, 25), 0.12m, 71m, false);
            var inputData = new[] {input1};

            var fakeDtp = A.Fake<IDateTimeProvider>();
            A.CallTo(() => fakeDtp.Today).Returns(today);
            var tradeDateMapper = new TradeDateMapper(TradeDateMapping.AsInFileAndToday, fakeDtp, NoBankHolidays);

            var mapped = InputMapper.Map(inputData, DefaultPricingGroup, GetFakeInstruments, tradeDateMapper).ToList();

            mapped.Count.Should().Be(2);
            var mappedDates = mapped.Select(opd => opd.TradeDate).ToList();
            mappedDates.Should().BeEquivalentTo(expected, input1.TradeDate);
        }

        [Fact]
        public void MapCreatesPriceForTuesdayIfTodayAtWeekendAndMondayIsBankHoliday() {
            var friday = new DateTime(2018, 06, 01);
            var saturday = new DateTime(2018, 06, 02);
            var monday = new DateTime(2018, 06, 04);
            var tuesday = new DateTime(2018, 06, 05);

            var input1 = new InputPriceData(
                FakeOi1.Code, "C", None,
                tradeDate: friday,
                stripDate: new DateTime(2018, 8, 1),
                expirationDate: new DateTime(2018, 5, 25),
                settlementPrice: 0.12m,
                strikePrice: 71m,
                isBalMoOrCso: false);
            var inputData = new[] {input1};

            var fakeDtp = A.Fake<IDateTimeProvider>();
            A.CallTo(() => fakeDtp.Today).Returns(saturday);

            var isBankHoliday = new BankHolidayLookup(dt => dt == monday);
            var tradeDateMapper = new TradeDateMapper(TradeDateMapping.Today, fakeDtp, isBankHoliday);

            var mapped = InputMapper.Map(inputData, DefaultPricingGroup, GetFakeInstruments, tradeDateMapper).ToList();

            mapped.Count.Should().Be(1);
            var mappedDates = mapped.Select(opd => opd.TradeDate).ToList();
            mappedDates.Should().BeEquivalentTo(tuesday);
        }
    }
}
