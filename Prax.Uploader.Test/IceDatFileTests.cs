using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Xunit;

namespace Prax.Uploader.Test
{
    /// <summary>
    /// Contains tests that are specific to parsing the contents of an ICE dat file
    /// </summary>
    /// NB: the generic file-monitoring etc logic tests are in InputFileTests
    public class IceDatFileTests {

        private static readonly DateTime FakeToday = new DateTime(2018, 05, 11);
        private static readonly DateTime FakeNow = new DateTime(2018, 05, 11, 13, 14, 15);
        private static readonly DateTime FakeUtcNow = new DateTime(2018, 05, 11, 12, 14, 15);

        private static IFileSystemProxy CreateFileSystemProxy() {
            var mockFileSystem = A.Fake<IFileSystemProxy>();
            A.CallTo(() => mockFileSystem.GetFileContents(TestHelpers.FakeIceFilepath1))
             .Returns(TestHelpers.GenerateStreamFromString(FakeDatFileContents));

            var fakeFiles = new List<string> {TestHelpers.FakeIceFilepath1};
            A.CallTo(() => mockFileSystem.GetMatchingFileNames(TestHelpers.FakeDirectory, TestHelpers.IceFilePattern))
             .Returns(fakeFiles);

            A.CallTo(() => mockFileSystem.GetFileSize(A<string>.Ignored)).Returns(1234);

            return mockFileSystem;
        }

        [Fact]
        public async Task DatFileShouldBeReadCorrectly() {
            var mockFileSystem = CreateFileSystemProxy();
            var dtp = TestHelpers.CreateDtp(today: FakeToday, now: FakeNow, utcNow: FakeUtcNow);

            var sut = new IceDatFile(TestHelpers.CreateConfig(), mockFileSystem, dtp, CancellationToken.None);
            var (description1, data1) = await sut.GetInputData();
            description1.Should().BeNullOrWhiteSpace();
            data1.Should().BeEmpty();

            var (description2, data2) = await sut.GetInputData();
            description2.Should().Be(TestHelpers.FakeIceFilepath1);
            var xs = data2.ToList();

            xs.Count.Should().Be(8);

            foreach (var symbol in new[] {"BRN", "WBS", "HO", "WUL"}) {
                xs.Count(pd => pd.InstrumentName == symbol).Should().Be(2);
            }

            var strikePriceCases = new[] {
                ("BRN", new[] {100m, 101m}),
                ("WBS", new [] {28m, 30m}),
                ("HO", new [] {10m, 1m}),
                ("WUL", new [] {31m, 32m})
            };
            foreach (var (symbol, expected) in strikePriceCases) {
                xs.Where(x => x.InstrumentName == symbol).Select(x => x.StrikePrice).Should().BeEquivalentTo(expected);
            }

            var settlementPriceCases = new[] {
                ("BRN", new[] {0.25m, 0.23m}),
                ("WBS", new [] {42.13m, 40.13m}),
                ("HO", new [] {0.0001m, 1.205m}),
                ("WUL", new [] {38.87m, 37.87m})
            };
            foreach (var (symbol, expected) in settlementPriceCases) {
                xs.Where(x => x.InstrumentName == symbol).Select(x => x.SettlementPrice).Should().BeEquivalentTo(expected);
            }
        }

        // This contains:
        // 2 valid rows for each of the symbols the ICE Dat file has to map (B, T, HOF)
        // 2 valid rows for a symbol the doesn't need to be mapped (WUL)
        //
        // Remember that ICE use US-format dates
        private const string FakeDatFileContents = @"
TRADE DATE|HUB|PRODUCT|STRIP|CONTRACT|CONTRACT TYPE|STRIKE|SETTLEMENT PRICE|NET CHANGE|EXPIRATION DATE|PRODUCT_ID|OPTION_VOLATILITY|DELTA_FACTOR
5/10/2018|North Sea|Brent Crude Futures|10/1/2018|B|C|100.0000|0.25000|0.01000|8/28/2018|254|28.76|0.05168
5/10/2018|North Sea|Brent Crude Futures|10/1/2018|B|C|101.0000|0.23000|0.01000|8/28/2018|254|29.08|0.04728
5/10/2018|WTI|WTI Crude Futures|10/1/2018|T|C|28.0000|42.13000|0.40000|9/17/2018|425|41.29|0.99994
5/10/2018|WTI|WTI Crude Futures|10/1/2018|T|C|30.0000|40.13000|0.40000|9/17/2018|425|41.18|0.99983
5/10/2018|HO 1st Line|Heating Oil Futures|10/1/2018|HOF|C|10.0000|0.00010|0.00000|10/31/2018|39|20.99|0.00000
5/10/2018|HO 1st Line|Heating Oil Futures|10/1/2018|HOF|C|1.0000|1.20500|0.00950|10/31/2018|39|27.81|0.99063
5/10/2018|WTI Euro Option (Cash Settled)|Crude Futures|10/1/2018|WUL|C|31.0000|38.87000|0.40000|9/17/2018|1746|41.1|0.99295
5/10/2018|WTI Euro Option (Cash Settled)|Crude Futures|10/1/2018|WUL|C|32.0000|37.87000|0.40000|9/17/2018|1746|41|0.99280
";
    }
}
