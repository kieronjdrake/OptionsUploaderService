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
    /// Contains tests that are specific to parsing the contents of a Nymex / CME csv file
    /// </summary>
    /// NB: the generic file-monitoring etc logic tests are in InputFileTests
    public class NymexOptionFileTests {

        private static readonly DateTime FakeToday = new DateTime(2018, 07, 03);
        private static readonly DateTime FakeNow = new DateTime(2018, 07, 03, 13, 14, 15);
        private static readonly DateTime FakeUtcNow = new DateTime(2018, 07, 03, 12, 14, 15);

        private static IFileSystemProxy CreateFileSystemProxy(string fileContents = FakeCsvFileContents) {
            var mockFileSystem = A.Fake<IFileSystemProxy>();
            A.CallTo(() => mockFileSystem.GetFileContents(TestHelpers.FakeNymexFilepath1))
             .Returns(TestHelpers.GenerateStreamFromString(fileContents));

            A.CallTo(() => mockFileSystem.GetMatchingFileNames(TestHelpers.FakeDirectory, TestHelpers.NymexFilePattern))
             .Returns(new List<string> {TestHelpers.FakeNymexFilepath1});

            A.CallTo(() => mockFileSystem.GetFileSize(A<string>.Ignored)).Returns(1234);

            return mockFileSystem;
        }

        [Fact]
        public async Task CsvFileShouldBeReadCorrectly() {
            var mockFileSystem = CreateFileSystemProxy();
            var dtp = TestHelpers.CreateDtp(today: FakeToday, now: FakeNow, utcNow: FakeUtcNow);

            var sut = new NymexOptionFile(TestHelpers.CreateConfig(), mockFileSystem, dtp, CancellationToken.None);

            var (description1, data1) = await sut.GetInputData();
            description1.Should().BeNullOrWhiteSpace();
            data1.Should().BeEmpty();

            var (description2, data2) = await sut.GetInputData();
            description2.Should().Be(TestHelpers.FakeNymexFilepath1);
            var xs = data2.ToList();

            xs.Count.Should().Be(4);

            var symbols = new[] {"BZO", "LO"};
            var putCall = new[] {"P", "C"};
            
            foreach (var symbol in symbols) {
                xs.Count(pd => pd.InstrumentName == symbol).Should().Be(2);
            }

            foreach (var data in from s in symbols
                              from pc in putCall
                              select (s, pc)) {
                var (s, pc) = data;
                xs.Count(pd => pd.InstrumentName == s && pd.ContractType == pc).Should().Be(1);
            }

            foreach (var ipd in xs) {
                var expected = new DateTime(2018, 07, 02);
                ipd.TradeDate.Should().Be(expected);
            }

            var strikePriceCases = new[] {
                ("BZO", new[] {68m, 69m}),
                ("LO", new [] {68m, 69.5m})
            };
            foreach (var (symbol, expected) in strikePriceCases) {
                xs.Where(x => x.InstrumentName == symbol).Select(x => x.StrikePrice).Should().BeEquivalentTo(expected);
            }

            var settlementPriceCases = new[] {
                ("BZO", new[] {9.44m, 0.19m}),
                ("LO", new [] {4.77m, 0.22m})
            };
            foreach (var (symbol, expected) in settlementPriceCases) {
                xs.Where(x => x.InstrumentName == symbol).Select(x => x.SettlementPrice).Should().BeEquivalentTo(expected);
            }

            var stripDateCases = new[] {
                ("BZO", new[] { new DateTime(2018, 09, 01), new DateTime(2018, 09, 02) }),
                ("LO",  new[] { new DateTime(2018, 08, 01), new DateTime(2018, 08, 01)})
            };
            foreach (var (symbol, expected) in stripDateCases) {
                xs.Where(x => x.InstrumentName == symbol).Select(x => x.StripDate).Should().BeEquivalentTo(expected);
            }
        }
        
        // Data is (should return 6 valid rows):
        // 
        // 2 rows of each type we're interested in (BZO, LO)
        private const string FakeCsvFileContents = @"
BizDt,Sym,ID,StrkPx,SecTyp,MMY,MatDt,PutCall,Exch,Desc,LastTrdDt,BidPrice,OpeningPrice,SettlePrice,SettleDelta,HighLimit,LowLimit,DHighPrice,DLowPrice,HighBid,LowBid,PrevDayVol,PrevDayOI,FixingPrice,UndlyExch,UndlyID,UndlySecTyp,UndlyMMY,BankBusDay
""2018-07-02"",""BZO"",""BZO"",""68.0"",""OOF"",""201809"",""2018-07-26"",""1"",""NYMEX"","""",""2018-07-26"","""","""",""9.44"",""0.95153"","""",""0.01"","""","""",""10.72"",""9.35"",""58"",""1192"","""",""NYMEX"",""BZ"",""FUT"",""201809"",""""
""2018-07-02"",""BZO"",""BZO"",""69.0"",""OOF"",""20180902"",""2018-07-26"",""0"",""NYMEX"","""",""2018-07-26"","""","""",""0.19"",""-0.04847"","""",""0.01"","""","""",""0.13"",""0.1"",""58"",""642"","""",""NYMEX"",""BZ"",""FUT"",""201809"",""""
""2018-07-02"",""LO"",""LO"",""68.0"",""OOF"",""201808"",""2018-07-17"",""1"",""NYMEX"","""",""2018-07-17"","""","""",""4.77"",""0.90616"",""99999.0"",""0.01"","""","""",""5.46"",""4.26"",""57"",""2940"","""",""NYMEX"",""CL"",""FUT"",""201808"",""""
""2018-07-02"",""LO"",""LO"",""69.5"",""OOF"",""201808"",""2018-07-17"",""0"",""NYMEX"","""",""2018-07-17"","""","""",""0.22"",""-0.09384"",""99999.0"",""0.01"",""0.28"",""0.16"","""","""",""1193"",""4438"","""",""NYMEX"",""CL"",""FUT"",""201808"",""""
";

    }
}
