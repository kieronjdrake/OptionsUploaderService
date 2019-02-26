using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FakeItEasy;
using FakeItEasy.Configuration;
using FluentAssertions;
using LanguageExt;
using static LanguageExt.Prelude;
using Prax.Aspect;
using Prax.Utils;
using Xunit;
using LogLevel = NLog.LogLevel;

namespace Prax.Uploader.Test
{
    [ExcludeFromCodeCoverage]
    public class UploaderTests {
        private static readonly DateTime FakeToday = new DateTime(2018, 07, 20);
        private static readonly DateTime FakeNow = FakeToday + new TimeSpan(10, 11, 12);
        private static readonly DateTime FakeUtcNow = FakeToday - new TimeSpan(1, 0, 0);

        private static readonly string FakeDefaultPricingGroup = "Fake Default Pricing Group";

        private static readonly InputPriceData FakeInputData1 = new InputPriceData(
            "ins1", "C", None, new DateTime(2018, 07, 19), new DateTime(2018, 09, 01), None,
            1.23m, 69m, false);

        private static readonly InputPriceData FakeInputData2 = new InputPriceData(
            "ins2", "P", None, new DateTime(2018, 07, 19), new DateTime(2018, 10, 01), None,
            2.34m, 65m, false);

        private static readonly List<OptionInstrument> FakeOptionInstruments = new List<OptionInstrument> {
            new OptionInstrument(OptionInstrumentType.EO, "instrumentName1", "ins1"),
            new OptionInstrument(OptionInstrumentType.EO, "instrumentName2", "ins2")
        };

        private static OptionPriceData ToOpd(OptionInstrument oi, OptionType ot, InputPriceData ipd,
                                             DateTime? tradeDateOverride = null) {
            return new OptionPriceData(oi, ot, tradeDateOverride ?? ipd.TradeDate, ipd.StripDate, ipd.ExpirationDate,
                                       ipd.SettlementPrice, ipd.StrikePrice,
                                       ipd.PricingGroup.IfNone(FakeDefaultPricingGroup), ipd.IsBalMoOrCso);
        }

        private static readonly List<OptionPriceData> FakeOpds = new List<OptionPriceData> {
            ToOpd(FakeOptionInstruments[0], OptionType.Call, FakeInputData1),
            ToOpd(FakeOptionInstruments[1], OptionType.Put, FakeInputData2)
        };

        private static IDateTimeProvider CreateFakeDtp(DateTime? today = null, DateTime? now = null,
                                                       DateTime? utcNow = null) {
            var dtp = A.Fake<IDateTimeProvider>();
            A.CallTo(() => dtp.Today).Returns(today ?? FakeToday);
            A.CallTo(() => dtp.Now).Returns(now ?? FakeNow);
            A.CallTo(() => dtp.UtcNow).Returns(utcNow ?? FakeUtcNow);
            return dtp;
        }

        private class FakeUploadQueueFactory : IUploadQueueFactory {
            public readonly List<IUploadQueue> Qs = new List<IUploadQueue>();

            public IUploadQueue Create() {
                var q = A.Fake<IUploadQueue>();
                A.CallTo(() => q.AddAction(A<Action>._))
                 .Invokes((Action a) => a?.Invoke())
                 .Returns(true);
                Qs.Add(q);
                return q;
            }
        }

        private static UploadRunner CreateUploader(IEnumerable<IServerFacade> servers = null,
                                                   IEnumerable<IInputSource> inputSources = null,
                                                   IUploaderConfig uploaderConfig = null,
                                                   IMessageSink uploadMessageSink = null,
                                                   IDateTimeProvider dtp = null,
                                                   IUploadQueueFactory uqf = null) {
            return new UploadRunner(
                servers ?? Enumerable.Empty<IServerFacade>(),
                inputSources ?? Enumerable.Empty<IInputSource>(),
                uploaderConfig ?? CreateFakeConfig(),
                uploadMessageSink ?? A.Fake<IMessageSink>(),
                dtp ?? CreateFakeDtp(),
                uqf ?? new FakeUploadQueueFactory());
        }

        private static IServerFacade CreateFakeServer(AspectEnvironment env,
                                                      bool isPrimary = false,
                                                      List<OptionInstrument> ois = null) {
            var s = A.Fake<IServerFacade>();
            A.CallTo(() => s.Environment).Returns(env);
            A.CallTo(() => s.IsPrimary).Returns(isPrimary);
            A.CallTo(() => s.GetOptionsInstruments()).Returns(ois ?? FakeOptionInstruments);
            return s;
        }

        private static IUploaderConfig CreateFakeConfig() {
            var config = A.Fake<IUploaderConfig>();
            A.CallTo(() => config.ForceUploadOldTradeDates).Returns(false);
            A.CallTo(() => config.TradeDateMapping).Returns(TradeDateMapping.AsInFile);
            A.CallTo(() => config.MarkProcessedOnceUploaded).Returns(true);
            A.CallTo(() => config.DryRun).Returns(false);
            A.CallTo(() => config.UploadChunkSize).Returns(10);
            A.CallTo(() => config.WaitIntervalBetweenBatchesMs).Returns(100);
            A.CallTo(() => config.MaxOptionsToUpload).Returns(null);
            A.CallTo(() => config.OptionsToSkip).Returns(null);
            A.CallTo(() => config.DefaultPricingGroup).Returns(FakeDefaultPricingGroup);
            return config;
        }

        private static IReturnValueArgumentValidationConfiguration<UploadResult>
            UploadOptionPricesCall(IServerFacade server) {
            return A.CallTo(
                () => server.UploadOptionPrices(A<List<OptionPriceData>>._, A<int>._, A<Option<int>>._, A<bool>._));
        }

        private static List<(IServerFacade, List<OptionPriceData>)>  SetupUploadCallIntercept(
            IEnumerable<IServerFacade> servers) {
            var uploadCallOpds = new List<(IServerFacade, List<OptionPriceData>)>();
            foreach (var server in servers) {
                UploadOptionPricesCall(server)
                 .Invokes((List<OptionPriceData> xs, int p1, Option<int> p2, bool p3) => uploadCallOpds.Add((server, xs)))
                 .ReturnsLazily(foc => UploadResult.AllSucceeded(uploadCallOpds.Last().Item2.Count));
            }
            return uploadCallOpds;
        }

        [Fact]
        public void ShouldThrowOnConstructIfNoAspectServersGiven() {
            var noServers = new List<IServerFacade>();
            var ex = Record.Exception(() => CreateUploader(noServers));
            ex.Should().NotBeNull();
            ex.Message.Should().Contain("at least one Aspect server");
        }

        [Fact]
        public void ShouldThrowOnConstructIfNoAspectServerIsPrimary() {
            var servers = new[] {
                CreateFakeServer(AspectEnvironment.Production, isPrimary: false),
                CreateFakeServer(AspectEnvironment.Test, isPrimary: false)
            };
            var ex = Record.Exception(() => CreateUploader(servers));
            ex.Should().NotBeNull();
            ex.Message.Should().Be("Please mark exactly one Aspect endpoint as isPrimary=true");
        }

        [Fact]
        public void ShouldThrowOnConstructIfMoreThanOneAspectServerIsPrimary() {
            var servers = new[] {
                CreateFakeServer(AspectEnvironment.Production, isPrimary: true),
                CreateFakeServer(AspectEnvironment.Test, isPrimary: false),
                CreateFakeServer(AspectEnvironment.Staging, isPrimary: true)
            };
            var ex = Record.Exception(() => CreateUploader(servers));
            ex.Should().NotBeNull();
            ex.Message.Should().Be("Please mark exactly one Aspect endpoint as isPrimary=true");
        }

        [Fact]
        public void ShowThrowOnConstructIfMoreThanOneServerFoundForAnEnvironment() {
            var servers = new[] {
                CreateFakeServer(AspectEnvironment.Production, isPrimary: true),
                CreateFakeServer(AspectEnvironment.Test, isPrimary: false),
                CreateFakeServer(AspectEnvironment.Test, isPrimary: false)
            };

            var ex = Record.Exception(() => CreateUploader(servers));

            ex.Should().NotBeNull();
            ex.Message.Should().Contain("Duplicate Aspect environments");
            var messageRegex = new Regex(".*: (.*)$");
            var match = messageRegex.Match(ex.Message);
            match.Success.Should().BeTrue();
            match.Groups.Count.Should().Be(2);
            var envString = match.Groups[1].Value;
            var messageEnvs = envString.Split(',');
            messageEnvs.Select(x => (AspectEnvironment)Enum.Parse(typeof(AspectEnvironment), x))
                       .Should().BeEquivalentTo(servers.Select(s => s.Environment));
        }

        [Fact]
        public async Task StaleTradesShouldPreventAnUploadIfConfigFlagNotSet() {
            // Arrange
            var memoryTarget = LogUtils.SetupTestLogger(LogLevel.Error);

            var dtp = CreateFakeDtp(today: FakeInputData1.TradeDate + TimeSpan.FromDays(5));

            var inputSource = A.Fake<IInputSource>();
            const string fakeDescription = "fake input for test";
            A.CallTo(() => inputSource.GetInputData()).Returns(
                Task.FromResult((fakeDescription, new List<InputPriceData> {FakeInputData1})));

            var primaryServer = CreateFakeServer(AspectEnvironment.Test, isPrimary: true);
            var servers = new[] {primaryServer};

            var config = CreateFakeConfig();

            // Act
            var sut = CreateUploader(servers, new[] {inputSource}, config, dtp: dtp);
            await sut.UploadOptionPrices();

            // Assert
            A.CallTo(() => primaryServer.GetOptionsInstruments()).MustHaveHappenedOnceExactly();

            UploadOptionPricesCall(primaryServer).MustNotHaveHappened();

            A.CallTo(() => inputSource.InputDataUploaded(false)).MustHaveHappenedOnceExactly();

            var logs = memoryTarget.Logs;
            logs.Count.Should().Be(1);
            logs[0].Should().Contain("1 stale trade");
            logs[0].Should().Contain(fakeDescription);
        }

        [Fact]
        public async Task StaleTradesShouldBeUploadedIfConfigFlagSet() {
            // Arrange
            var memoryTarget = LogUtils.SetupTestLogger(LogLevel.Info);

            var dtp = CreateFakeDtp(today: FakeInputData1.TradeDate + TimeSpan.FromDays(5));

            var inputSource = A.Fake<IInputSource>();
            const string fakeDescription = "fake input for test";
            A.CallTo(() => inputSource.GetInputData()).Returns(
                Task.FromResult((fakeDescription, new List<InputPriceData> {FakeInputData1})));

            var primaryServer = CreateFakeServer(AspectEnvironment.Test, isPrimary: true);
            var servers = new[] {primaryServer};
            UploadOptionPricesCall(primaryServer).Returns(UploadResult.SingleSuccess());

            var config = CreateFakeConfig();
            A.CallTo(() => config.ForceUploadOldTradeDates).Returns(true);

            // Act
            var sut = CreateUploader(servers, new[] {inputSource}, config, dtp: dtp);
            await sut.UploadOptionPrices();

            // Assert
            A.CallTo(() => primaryServer.GetOptionsInstruments()).MustHaveHappenedOnceExactly();

            UploadOptionPricesCall(primaryServer).MustHaveHappenedOnceExactly();

            A.CallTo(() => inputSource.InputDataUploaded(true)).MustHaveHappenedOnceExactly();

            var logs = memoryTarget.Logs;
            logs.Count.Should().Be(3);
            logs.Count(m => m.Contains("Created input sources")).Should().Be(1);
            logs.Count(m => m.Contains("1 option prices to upload to Test Aspect")).Should().Be(1);
            logs.Count(m => m.Contains("Uploaded option prices")).Should().Be(1);
        }

        [Fact]
        public async Task ValidTradesShouldBeSentToAllAspectServersToUploadWithCorrectConfig() {
            // Arrange
            var servers = new[] {
                CreateFakeServer(AspectEnvironment.Production, isPrimary: true),
                CreateFakeServer(AspectEnvironment.Test, isPrimary: false)
            };

            var uploadCallOpds = SetupUploadCallIntercept(servers);

            var inputSource = A.Fake<IInputSource>();
            const string fakeDescription = "fake input for test";
            A.CallTo(() => inputSource.GetInputData()).Returns(
                Task.FromResult((fakeDescription, new List<InputPriceData> {FakeInputData1})));

            var inputSource2 = A.Fake<IInputSource>();
            const string fakeDescription2 = "fake input for test 2";
            A.CallTo(() => inputSource2.GetInputData()).Returns(
                Task.FromResult((fakeDescription2, new List<InputPriceData> {FakeInputData2})));

            var dtp = CreateFakeDtp();
            var config = CreateFakeConfig();

            // Act
            var sut = CreateUploader(servers, new[] {inputSource, inputSource2}, config, dtp: dtp);
            await sut.UploadOptionPrices();

            // Assert
            A.CallTo(() => servers[0].GetOptionsInstruments()).MustHaveHappenedOnceExactly();
            A.CallTo(() => servers[1].GetOptionsInstruments()).MustNotHaveHappened();

            foreach (var server in servers) {
                A.CallTo(
                    () =>
                    server.UploadOptionPrices(A<List<OptionPriceData>>.Ignored, // Can't compare the opds here
                                              config.UploadChunkSize,
                                              config.WaitIntervalBetweenBatchesMs.ToOption(),
                                              config.DryRun)).MustHaveHappenedOnceExactly();
            }

            uploadCallOpds.Count.Should().Be(2);
            uploadCallOpds[0].Item1.Should().BeSameAs(servers[0]);
            uploadCallOpds[1].Item1.Should().BeSameAs(servers[1]);
            uploadCallOpds[0].Item2.Should().BeEquivalentTo(FakeOpds);
            uploadCallOpds[1].Item2.Should().BeEquivalentTo(FakeOpds);

            A.CallTo(() => inputSource.InputDataUploaded(true)).MustHaveHappenedOnceExactly();
            A.CallTo(() => inputSource2.InputDataUploaded(true)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task AsInFileAndTodayShouldResultInTwoUploadsForEachInput() {
            // Arrange
            var primaryServer = CreateFakeServer(AspectEnvironment.Test, isPrimary: true);

            var inputSource = A.Fake<IInputSource>();
            const string fakeDescription = "fake input for test";
            A.CallTo(() => inputSource.GetInputData()).Returns(
                Task.FromResult((fakeDescription, new List<InputPriceData> {FakeInputData1})));

            var config = CreateFakeConfig();
            A.CallTo(() => config.TradeDateMapping).Returns(TradeDateMapping.AsInFileAndToday);

            var servers = new[] {primaryServer};
            var uploadCallOpds = SetupUploadCallIntercept(servers);

            // Act
            var sut = CreateUploader(servers, new[] {inputSource}, config);
            await sut.UploadOptionPrices();

            uploadCallOpds.Count.Should().Be(1);
            uploadCallOpds[0].Item1.Should().BeSameAs(primaryServer);
            var uploaded = uploadCallOpds[0].Item2;
            uploaded.Count.Should().Be(2);
            uploaded.Count(opd => opd.TradeDate == FakeToday).Should().Be(1);
            uploaded.Count(opd => opd.TradeDate == FakeInputData1.TradeDate).Should().Be(1);

            // The Today part should lookup each input's TradeDate to see if it's a bank holiday
            A.CallTo(() => primaryServer.IsBankHoliday(FakeToday)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task IfAnInputSourceThrowsOnInputDataUploadedCallAWarningIsLogged() {
            // Arrange
            var memoryTarget = LogUtils.SetupTestLogger(LogLevel.Warn);

            var primaryServer = CreateFakeServer(AspectEnvironment.Test, isPrimary: true);

            var inputSource = A.Fake<IInputSource>();
            const string fakeDescription = "fake input for test";
            A.CallTo(() => inputSource.GetInputData()).Returns(
                Task.FromResult((fakeDescription, new List<InputPriceData> {FakeInputData1})));
            A.CallTo(() => inputSource.InputDataUploaded(A<bool>._)).Throws<FileNotFoundException>();

            var servers = new[] {primaryServer};

            // Act
            var sut = CreateUploader(servers, new[] {inputSource});
            await sut.UploadOptionPrices();

            // Assert
            var logs = memoryTarget.Logs;
            logs.Count.Should().Be(1);
            logs[0].Should().Contain("Data source cleanup failed");
        }

        [Fact]
        public async Task EachCallToUploadOptionPricesShouldPostACompleteMessageIfThereArePricesToUpload() {
            // Arrange
            var primaryServer = CreateFakeServer(AspectEnvironment.Test, isPrimary: true);

            var fakeInputSourceName = "I am an input source";
            var inputSource = A.Fake<IInputSource>();
            A.CallTo(() => inputSource.GetInputData()).Returns(
                Task.FromResult(("fake input for test", new List<InputPriceData> {FakeInputData1})));
            A.CallTo(() => inputSource.SourceName).Returns(fakeInputSourceName);

            var servers = new[] {primaryServer};

            var messageSink = A.Fake<IMessageSink>();

            // Act
            var sut = CreateUploader(servers, new[] {inputSource}, uploadMessageSink: messageSink);
            await sut.UploadOptionPrices();

            // Assert
            A.CallTo(() => messageSink.SendMessage(A<string>.That.Contains(fakeInputSourceName), true)).MustHaveHappenedOnceExactly();
            
            // Act again
            await sut.UploadOptionPrices();

            // Assert again
            A.CallTo(() => messageSink.SendMessage(A<string>.That.Contains(fakeInputSourceName), true)).MustHaveHappenedTwiceExactly();
        }

        [Fact]
        public async Task ACallToUploadShouldPostAMessageReportingTheNumberOfSuccessesIgnoredAndFailures() {
            // Arrange
            var fakeResult = new UploadResult(123, 23, 3);
            var primaryServer = CreateFakeServer(AspectEnvironment.Test, isPrimary: true);
            var servers = new[] {primaryServer};
            UploadOptionPricesCall(primaryServer).Returns(fakeResult);

            var fakeInputSourceName = "I am an input source";
            var inputSource = A.Fake<IInputSource>();
            A.CallTo(() => inputSource.GetInputData()).Returns(
                Task.FromResult(("fake input for test", new List<InputPriceData> {FakeInputData1})));
            A.CallTo(() => inputSource.SourceName).Returns(fakeInputSourceName);

            var messageSink = A.Fake<IMessageSink>();

            // Act
            var sut = CreateUploader(servers, new[] {inputSource}, uploadMessageSink: messageSink);
            await sut.UploadOptionPrices();

            // Assert
            var expectedMessages = new[] {"123 successes", "23 ignored", "3 failures"};
            foreach (var message in expectedMessages)
                A.CallTo(() => messageSink.SendMessage(A<string>.That.Contains(message), true))
                 .MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task CancellingCancellationTokenShouldShouldLogMessagesAndCloseQueuesAndSinks() {
            // Arrange
            var memoryTarget = LogUtils.SetupTestLogger(LogLevel.Debug);

            var servers = new[] {
                CreateFakeServer(AspectEnvironment.Production, isPrimary: true),
                CreateFakeServer(AspectEnvironment.Test, isPrimary: false)
            };

            // Fake up the cancellation by throwing the exception from the input source
            var inputSource = A.Fake<IInputSource>();
            A.CallTo(() => inputSource.GetInputData()).Throws<TaskCanceledException>();

            var qf = new FakeUploadQueueFactory();
            var ums = A.Fake<IMessageSink>();

            // Act
            var sut = CreateUploader(servers, new[]{inputSource}, uqf: qf, uploadMessageSink: ums);
            await sut.UploadOptionPrices();

            // Assert
            qf.Qs.Count.Should().Be(servers.Length);
            qf.Qs.ForEach(q => A.CallTo(() => q.SetCompleted()).MustHaveHappenedOnceExactly());
            A.CallTo(() => ums.Close()).MustHaveHappenedOnceExactly();

            var logs = memoryTarget.Logs;
            logs.Count.Should().BeGreaterOrEqualTo(3);
            var messageSnippets = new[] {
                "Upload cancelled",
                "All upload queues are SetCompleted",
                "Upload message sink closed"
            };
            foreach (var snippet in messageSnippets) {
                logs.Count(m => m.Contains(snippet)).Should().Be(1);
            }
        }

        [Fact]
        public async Task OptionsToSkipConfigShouldResultInSpecifiedNumberOfPricesSkipped() {
            // Arrange
            var primaryServer = CreateFakeServer(AspectEnvironment.Test, isPrimary: true);

            var inputSource = A.Fake<IInputSource>();
            A.CallTo(() => inputSource.GetInputData()).Returns(
                Task.FromResult(("fake input for test", new List<InputPriceData> {FakeInputData1, FakeInputData2})));

            var servers = new[] {primaryServer};
            var uploadCallOpds = SetupUploadCallIntercept(servers);
            var config = CreateFakeConfig();
            A.CallTo(() => config.OptionsToSkip).Returns(1);

            // Act
            var sut = CreateUploader(servers, new[] {inputSource}, uploaderConfig: config);
            await sut.UploadOptionPrices();

            // Assert
            uploadCallOpds.Count.Should().Be(1);
            var uploads = uploadCallOpds[0].Item2;
            uploads.Should().BeEquivalentTo(FakeOpds[1]);
        }

        [Fact]
        public async Task MaxOptionsToUploadConfigShouldLimitTheNumberOfPricesUploadedCorrectly() {
            // Arrange
            var primaryServer = CreateFakeServer(AspectEnvironment.Test, isPrimary: true);

            var inputSource = A.Fake<IInputSource>();
            A.CallTo(() => inputSource.GetInputData()).Returns(
                Task.FromResult(("fake input for test", new List<InputPriceData> {FakeInputData1, FakeInputData2})));

            var servers = new[] {primaryServer};
            var uploadCallOpds = SetupUploadCallIntercept(servers);
            var config = CreateFakeConfig();
            A.CallTo(() => config.MaxOptionsToUpload).Returns(1);

            // Act
            var sut = CreateUploader(servers, new[] {inputSource}, uploaderConfig: config);
            await sut.UploadOptionPrices();

            // Assert
            uploadCallOpds.Count.Should().Be(1);
            var uploads = uploadCallOpds[0].Item2;
            uploads.Should().BeEquivalentTo(FakeOpds[0]);
        }

        [Fact]
        public async Task IfThereAreUploadFailuresInputShouldBeAskedToMoveToErrorOnlyOnce() {
            // Arrange
            var servers = new[] {
                CreateFakeServer(AspectEnvironment.Production, isPrimary: true)
            };

            var inputSource = A.Fake<IInputSource>();
            A.CallTo(() => inputSource.GetInputData()).Throws<FileNotFoundException>();

            var config = CreateFakeConfig();
            A.CallTo(() => config.MarkProcessedOnceUploaded).Returns(true);

            // Act
            var sut = CreateUploader(servers, new[]{inputSource}, uploaderConfig: config);
            await sut.UploadOptionPrices();

            // Assert
            A.CallTo(() => inputSource.InputDataUploaded(false)).MustHaveHappenedOnceExactly();
        }

        [Fact]
        public async Task AnExceptionWhenReadingAnInputSourceShouldLogAnErrorAndGiveNoPricesToUpload() {
            // Arrange
            var memoryTarget = LogUtils.SetupTestLogger(LogLevel.Error);

            var primaryServer = CreateFakeServer(AspectEnvironment.Test, isPrimary: true);

            var inputSource = A.Fake<IInputSource>();
            A.CallTo(() => inputSource.GetInputData()).Throws<FileNotFoundException>();

            var servers = new[] {primaryServer};

            // Act
            var sut = CreateUploader(servers, new[] {inputSource});
            await sut.UploadOptionPrices();

            // Assert
            UploadOptionPricesCall(primaryServer).MustNotHaveHappened();
            var logs = memoryTarget.Logs;
            logs.Count.Should().Be(1);
            logs[0].Should().Contain("Exception when reading input source");

        }

        [Fact]
        public void AFatalExceptionWhenReadingAnInputSourceShouldRethrow() {
            // Fatal exceptions should cause the whole system to stop as it's a fail-fast mechanism for serious issues
            // like the input file format changing

            // Arrange
            var memoryTarget = LogUtils.SetupTestLogger(LogLevel.Error);

            var primaryServer = CreateFakeServer(AspectEnvironment.Test, isPrimary: true);

            var fakeError = "Sir, we have an ID-10-T error";
            var inputSource = A.Fake<IInputSource>();
            A.CallTo(() => inputSource.GetInputData()).Throws(() => new FatalUploaderException(fakeError));

            var servers = new[] {primaryServer};

            // Act
            var sut = CreateUploader(servers, new[] {inputSource});
            var ex = Record.ExceptionAsync(async () => await sut.UploadOptionPrices());
            ex.Should().NotBeNull();
            ex.Result.Message.Should().Contain(fakeError);
        }
    }
}
