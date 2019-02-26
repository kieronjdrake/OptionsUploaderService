using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FakeItEasy;
using FluentAssertions;
using Prax.Utils;
using Xunit;
using LogLevel = NLog.LogLevel;

namespace Prax.Uploader.Test
{
    /// <summary>
    /// This class contains the tests that are common to more than one file input source.
    /// </summary>
    /// The processing for ICE, Nymex etc input files are generally very similar, with business logic like the
    /// expected file extension, format and settlement validation varying but the general mechanics staying the same
    /// Hence the IceDatFileTests and NymexOptionFileTets contain some file-specific tests, but the general mechanism
    /// is tested here.
    public class InputFileTests {

        private static IFileSystemProxy CreateFileSystemProxy(bool addFakeFile2 = false,
                                                              string iceFileContents = FakeIceDatFileContents,
                                                              string cmeFileContents = FakeNymexFileContents) {
            var mockFileSystem = A.Fake<IFileSystemProxy>();
            
            A.CallTo(() => mockFileSystem.GetFileContents(TestHelpers.FakeIceFilepath1))
             .Returns(TestHelpers.GenerateStreamFromString(iceFileContents));
            A.CallTo(() => mockFileSystem.GetFileContents(TestHelpers.FakeNymexFilepath1))
             .Returns(TestHelpers.GenerateStreamFromString(cmeFileContents));

            var fakeIceFiles = new List<string> {TestHelpers.FakeIceFilepath1};
            var fakeCmeFiles = new List<string> {TestHelpers.FakeNymexFilepath1};
            if (addFakeFile2) {
                fakeIceFiles.Add(TestHelpers.FakeIceFilepath2);
                fakeCmeFiles.Add(TestHelpers.FakeNymexFilepath2);
                A.CallTo(() => mockFileSystem.GetFileContents(TestHelpers.FakeIceFilepath2))
                 .Returns(TestHelpers.GenerateStreamFromString(iceFileContents));
                A.CallTo(() => mockFileSystem.GetFileContents(TestHelpers.FakeNymexFilepath2))
                 .Returns(TestHelpers.GenerateStreamFromString(cmeFileContents));
            }

            A.CallTo(() => mockFileSystem.GetMatchingFileNames(TestHelpers.FakeDirectory, TestHelpers.IceFilePattern))
             .Returns(fakeIceFiles);
            A.CallTo(() => mockFileSystem.GetMatchingFileNames(TestHelpers.FakeDirectory, TestHelpers.NymexFilePattern))
             .Returns(fakeCmeFiles);

            return mockFileSystem;
        }

        private static readonly IDateTimeProvider Dtp = TestHelpers.CreateDtp();
        private static readonly IFileInputSourceConfig Config = TestHelpers.CreateConfig();


        [Fact]
        public async Task IfNoInputFilesPresentShouldReturnEmptyOnRepeatedCallsToGetInputData() {
            var mockFileSystem = A.Fake<IFileSystemProxy>();
            A.CallTo(() => mockFileSystem.GetMatchingFileNames(TestHelpers.FakeDirectory, A<string>.Ignored))
             .Returns(Enumerable.Empty<string>());

            async Task RunTestImpl(IInputSource sut) {
                var (description1, data1) = await sut.GetInputData();
                description1.Should().BeNullOrWhiteSpace();
                data1.Should().BeEmpty();

                var (description2, data2) = await sut.GetInputData();
                description2.Should().BeNullOrWhiteSpace();
                data2.Should().BeEmpty();
            }

            await RunTestImpl(new IceDatFile(Config, mockFileSystem, Dtp, CancellationToken.None));
            await RunTestImpl(new NymexOptionFile(Config, mockFileSystem, Dtp, CancellationToken.None));
        }

        [Fact]
        public async Task IfAFileSizeKeepsChangingTheFileIsNotReturnedUntilTheSizeRemainsTheSame() {
            var mockFileSystem = CreateFileSystemProxy();
            A.CallTo(() => mockFileSystem.GetFileSize(TestHelpers.FakeIceFilepath1)).Returns(1234).Once().Then.Returns(2345);
            A.CallTo(() => mockFileSystem.GetFileSize(TestHelpers.FakeNymexFilepath1)).Returns(1234).Once().Then.Returns(2345);

            async Task RunTestImpl(IInputSource sut, string expectedFilepath) {
                for (var i = 0; i < 3; ++i) {
                    if (i < 2) {
                        var (description, data) = await sut.GetInputData();
                        description.Should().BeNullOrWhiteSpace();
                        data.Should().BeEmpty();
                    } else {
                        var (description, data) = await sut.GetInputData();
                        description.Should().Be(expectedFilepath);
                        data.Should().NotBeEmpty();
                    }
                }
            }

            await RunTestImpl(new IceDatFile(Config, mockFileSystem, Dtp, CancellationToken.None), TestHelpers.FakeIceFilepath1);
            await RunTestImpl(new NymexOptionFile(Config, mockFileSystem, Dtp, CancellationToken.None), TestHelpers.FakeNymexFilepath1);
        }

        [Fact]
        public async Task IfANewFileAppearsOnTheSecondCallThenItShouldOnlyBeReturnedOnTheThirdCall() {
            var mockFileSystem = CreateFileSystemProxy();

            //var mockFileSystem = A.Fake<IFileSystemProxy>();
            A.CallTo(() => mockFileSystem.GetFileSize(A<string>.Ignored)).Returns(1234);

            A.CallTo(() => mockFileSystem.GetMatchingFileNames(TestHelpers.FakeDirectory, TestHelpers.IceFilePattern))
             .Returns(Enumerable.Empty<string>())
             .Once()
             .Then
             .Returns(new[] {TestHelpers.FakeIceFilepath1});

            A.CallTo(() => mockFileSystem.GetMatchingFileNames(TestHelpers.FakeDirectory, TestHelpers.NymexFilePattern))
             .Returns(Enumerable.Empty<string>())
             .Once()
             .Then
             .Returns(new[] {TestHelpers.FakeNymexFilepath1});

            async Task RunTestImpl(IInputSource sut, string expectedFilepath) {
                for (var i = 0; i < 3; ++i) {
                    if (i < 2) {
                        var (description, data) = await sut.GetInputData();
                        description.Should().BeNullOrWhiteSpace();
                        data.Should().BeEmpty();
                    } else {
                        var (description, data) = await sut.GetInputData();
                        description.Should().Be(expectedFilepath);
                        data.Should().NotBeEmpty();
                    }
                }
            }

            await RunTestImpl(new IceDatFile(Config, mockFileSystem, Dtp, CancellationToken.None), TestHelpers.FakeIceFilepath1);
            await RunTestImpl(new NymexOptionFile(Config, mockFileSystem, Dtp, CancellationToken.None),TestHelpers.FakeNymexFilepath1);
        }

        [Fact]
        public async Task IfTwoDatFilesArePresentAndBothSizesAreUnchangingThenBothFilesAreReturned() {
            var mockFileSystem = CreateFileSystemProxy(addFakeFile2: true);
            A.CallTo(() => mockFileSystem.GetFileSize(A<string>.Ignored)).Returns(1234);

            async Task RunTestImpl(
                IInputSource sut, string[] expectedFilepaths
                ) {
                var (description1, data1) = await sut.GetInputData();
                description1.Should().BeNullOrWhiteSpace();
                data1.Should().BeEmpty();

                var (description2, data2) = await sut.GetInputData();
                description2.Should().ContainAll(expectedFilepaths);
                data2.Count.Should().Be(2);
            }

            await RunTestImpl(new IceDatFile(Config, mockFileSystem, Dtp, CancellationToken.None),
                              new[] {TestHelpers.FakeIceFilepath1, TestHelpers.FakeIceFilepath2});

            await RunTestImpl(new NymexOptionFile(Config, mockFileSystem, Dtp, CancellationToken.None),
                              new[] {TestHelpers.FakeNymexFilepath1, TestHelpers.FakeNymexFilepath2});
        }

        [Fact]
        public async Task FilesThatHaveBeenUploadedShouldBeMovedToArchiveDirectoryOnCallToInputDataUploaded() {
            await FileMoveOnInputDataUploadedCallImpl("Archived", successFlag: true);
        }

        [Fact]
        public async Task FilesThatHaveBeenUploadedShouldBeMovedToErrorDirectoryOnCallToInputDataUploadedWithErrors() {
            await FileMoveOnInputDataUploadedCallImpl("Error", successFlag: false);
        }

        private static async Task FileMoveOnInputDataUploadedCallImpl(string expectedSubFolder, bool successFlag) {
            var mockFileSystem = CreateFileSystemProxy(addFakeFile2: true);
            A.CallTo(() => mockFileSystem.GetFileSize(A<string>.Ignored)).Returns(1234);
            A.CallTo(() => mockFileSystem.FileExists(A<string>.Ignored)).Returns(false);

            async Task RunTestImpl(IInputSource sut, IEnumerable<(string name, string path)> expectedFileData) {
                var fileData = expectedFileData as (string name, string path)[] ?? expectedFileData.ToArray();

                var (description1, data1) = await sut.GetInputData();
                description1.Should().BeNullOrWhiteSpace();
                data1.Should().BeEmpty();

                var (description2, data2) = await sut.GetInputData();
                
                var expectedFiles = fileData.Select(d => d.name);
                description2.Should().ContainAll(expectedFiles);
                data2.Should().NotBeEmpty();

                sut.InputDataUploaded(uploadSucceeded: successFlag);

                foreach (var (name, path) in fileData) {
                    var expectedDest = Path.Combine(TestHelpers.FakeDirectory, expectedSubFolder, name);
                    A.CallTo(() => mockFileSystem.MoveFile(path, expectedDest)).MustHaveHappenedOnceExactly();
                }
            }

            await RunTestImpl(
                new IceDatFile(Config, mockFileSystem, Dtp, CancellationToken.None),
                new[] {
                    (TestHelpers.FakeIceFilename1, TestHelpers.FakeIceFilepath1),
                    (TestHelpers.FakeIceFilename2, TestHelpers.FakeIceFilepath2)
                });

            await RunTestImpl(
                new NymexOptionFile(Config, mockFileSystem, Dtp, CancellationToken.None), 
                new[] {
                    (TestHelpers.FakeNymexFilename1, TestHelpers.FakeNymexFilepath1),
                    (TestHelpers.FakeNymexFilename2, TestHelpers.FakeNymexFilepath2)
                });
        }

        [Fact]
        public async Task InputDataUploadedShouldAskToMoveFileToCopyVersionIfArchivedVersionAlreadyExists() {
            var mockFileSystem = CreateFileSystemProxy();
            A.CallTo(() => mockFileSystem.GetFileSize(A<string>.Ignored)).Returns(1234);

            async Task RunTestImpl(IInputSource sut, (string name, string path) fileData) {
                A.CallTo(() => mockFileSystem.FileExists(A<string>.Ignored)).Returns(true).Once().Then.Returns(false);

                var (description1, data1) = await sut.GetInputData();
                description1.Should().BeNullOrWhiteSpace();
                data1.Should().BeEmpty();

                var (description2, data2) = await sut.GetInputData();
                description2.Should().NotBeNullOrWhiteSpace();
                data2.Should().NotBeEmpty();

                sut.InputDataUploaded(uploadSucceeded: true);

                var expectedDest = Path.Combine(TestHelpers.FakeDirectory, "Archived", $"{fileData.name}.copy");
                A.CallTo(() => mockFileSystem.MoveFile(fileData.path, expectedDest)).MustHaveHappenedOnceExactly();
            }

            await RunTestImpl(new IceDatFile(Config, mockFileSystem, Dtp, CancellationToken.None),
                              (TestHelpers.FakeIceFilename1, TestHelpers.FakeIceFilepath1));

            await RunTestImpl(new NymexOptionFile(Config, mockFileSystem, Dtp, CancellationToken.None),
                              (TestHelpers.FakeNymexFilename1, TestHelpers.FakeNymexFilepath1));
        }

        [Fact]
        public async Task GetInputSourceShouldRetryAndGetDataIfFileSystemThrowsExceptionOnce() {
            var mockFileSystem = CreateFileSystemProxy();
            A.CallTo(() => mockFileSystem.GetFileSize(A<string>.Ignored)).Returns(1234);

            var config = TestHelpers.CreateConfig(maxRetries: 2);

            async Task RunTestImpl(IInputSource sut, string filepath) {
                A.CallTo(() => mockFileSystem.GetMatchingFileNames(A<string>.Ignored, A<string>.Ignored))
                 .Throws(new UnauthorizedAccessException())
                 .Once()
                 .Then.Returns(new[] {filepath});

                var (description1, data1) = await sut.GetInputData();
                description1.Should().BeNullOrWhiteSpace();
                data1.Should().BeEmpty();

                var (description2, data2) = await sut.GetInputData();
                description2.Should().Be(filepath);
                data2.Should().NotBeEmpty();
            }

            await RunTestImpl(new IceDatFile(config, mockFileSystem, Dtp, CancellationToken.None),
                              TestHelpers.FakeIceFilepath1);

            await RunTestImpl(new NymexOptionFile(config, mockFileSystem, Dtp, CancellationToken.None),
                              TestHelpers.FakeNymexFilepath1);
        }

        [Fact]
        public async Task GetInputSourcesShouldLogErrorAndReturnEmptyIfFileSystemThrowsOnEveryRetry() {
            var loggerTarget = LogUtils.SetupTestLogger(LogLevel.Error);
            var mockFileSystem = A.Fake<IFileSystemProxy>();
            A.CallTo(() => mockFileSystem.GetMatchingFileNames(A<string>.Ignored, A<string>.Ignored))
             .Throws(new UnauthorizedAccessException());
            
            var config = TestHelpers.CreateConfig(maxRetries: 2);

            async Task RunTestImpl(IInputSource sut, string filepath) {
                var expectedLogMessageFragment = $"Exceeded retry count when trying to read \"{sut.SourceName}\" directory";
                for (int cycle = 0; cycle < 2; ++cycle) {
                    var (description, data) = await sut.GetInputData();
                    description.Should().BeNullOrWhiteSpace();
                    data.Should().BeEmpty();
                    loggerTarget.Logs.Count(m => m.Contains(expectedLogMessageFragment)).Should().Be(cycle + 1);
                }

                A.CallTo(() => mockFileSystem.GetFileSize(A<string>.Ignored)).MustNotHaveHappened();
                A.CallTo(() => mockFileSystem.GetFileContents(filepath)).MustNotHaveHappened();
            }

            await RunTestImpl(new IceDatFile(config, mockFileSystem, Dtp, CancellationToken.None), TestHelpers.FakeIceFilepath1);
            await RunTestImpl(new NymexOptionFile(config, mockFileSystem, Dtp, CancellationToken.None), TestHelpers.FakeNymexFilepath1);
        }

        [Fact]
        public async Task FileShouldNotBeMarkedAsReadyForUploadIfFileSystemThrowsWhenTryingToAccess() {
            var loggerTarget = LogUtils.SetupTestLogger(LogLevel.Debug);

            var mockFileSystem = A.Fake<IFileSystemProxy>();
            A.CallTo(() => mockFileSystem.GetFileSize(A<string>.Ignored)).Throws<Exception>();

            async Task RunTestImpl(IInputSource sut, string filename) {
                A.CallTo(() => mockFileSystem.GetMatchingFileNames(A<string>.Ignored, A<string>.Ignored))
                 .Returns(new[] {filename});
                for (int cycle = 1; cycle <= 3; ++cycle) {
                    var (description, data) = await sut.GetInputData();
                    description.Should().BeNullOrWhiteSpace();
                    data.Should().BeEmpty();
                    loggerTarget.Logs.Count(m => m.Contains($"Exception when getting file size for \"{filename}\""))
                                .Should().Be(cycle);
                }
            }

            await RunTestImpl(new IceDatFile(Config, mockFileSystem, Dtp, CancellationToken.None), TestHelpers.FakeIceFilename1);
            await RunTestImpl(new NymexOptionFile(Config, mockFileSystem, Dtp, CancellationToken.None), TestHelpers.FakeNymexFilename1);
        }

        [Fact]
        public async Task ZeroSizedFileShouldBeIgnoredUntilItHasASteadyNonZeroFileSize() {
            // This is something that was found by Kieron when downloading ICE dat files using Edge
            // Edge creates a zero-byte foo.dat file and then downloads the file to foo.dat.someuid.partial
            // When the file download is complete then the .partial file moves to overwrite the foo.dat, however
            // the uploader was locking the zero-byte file and thus the file move failed
            // Note that Chrome doesn't have this issue, it uses foo.dat.crdownload -> foo.dat

            var mockFileSystem = CreateFileSystemProxy();

            async Task RunTestImpl(IInputSource sut, string filepath) {
                
                // File data should only appear on the fifth run
                A.CallTo(() => mockFileSystem.GetFileSize(filepath)).Returns(0).NumberOfTimes(3).Then.Returns(1234);

                for (int cycle = 1; cycle <= 5; ++cycle) {
                    var (_, data) = await sut.GetInputData();
                    if (cycle < 5) {
                        data.Should().BeEmpty($"cycle {cycle} should have empty data");
                    } else {
                        data.Should().NotBeEmpty("cycle 5 should find data to upload");
                    }
                }
            }

            await RunTestImpl(new IceDatFile(Config, mockFileSystem, Dtp, CancellationToken.None), TestHelpers.FakeIceFilepath1);
            await RunTestImpl(new NymexOptionFile(Config, mockFileSystem, Dtp, CancellationToken.None), TestHelpers.FakeNymexFilepath1);
        }

        [Fact]
        public async Task FileReadErrorShouldMoveInputFileToErrorFolderAndReturnEmptyData() {
            var mockFileSystem = CreateFileSystemProxy(addFakeFile2: true);
            A.CallTo(() => mockFileSystem.GetFileSize(A<string>.Ignored)).Returns(1234);

            async Task RunTestImpl(IInputSource sut, string filepath) {
                var loggerTarget = LogUtils.SetupTestLogger(LogLevel.Error);

                // Overwrite the contents of the Filepath1 file to return something that will make the csv reading fail
                A.CallTo(() => mockFileSystem.GetFileContents(filepath))
                 .Returns(TestHelpers.GenerateStreamFromString("Not a valid csv file"));

                // One input file should be read OK and return data, the other (that is overridden to return guff)
                // should error and return no data
                var (description1, data1) = await sut.GetInputData();
                description1.Should().BeNullOrWhiteSpace();
                data1.Should().BeEmpty();

                var (description, data) = await sut.GetInputData();
                description.Should().NotContain(TestHelpers.FakeIceFilepath1);
                description.Should().NotContain(TestHelpers.FakeNymexFilepath1);

                loggerTarget.Logs.Count(m => m.Contains("Failed to read input file")).Should().Be(1);
                data.Count.Should().Be(1);

                A.CallTo(() => mockFileSystem.MoveFile(filepath, A<string>.That.Contains("Error")))
                 .MustHaveHappenedOnceExactly();
            }

            await RunTestImpl(new IceDatFile(Config, mockFileSystem, Dtp, CancellationToken.None), TestHelpers.FakeIceFilepath1);
            await RunTestImpl(new NymexOptionFile(Config, mockFileSystem, Dtp, CancellationToken.None), TestHelpers.FakeNymexFilepath1);
        }

        [Fact]
        public async Task ZeroPriceFileShouldBeMovedToArchiveDirectory() {
            var mockFileSystem = CreateFileSystemProxy(
                addFakeFile2: false,
                iceFileContents: FakeIceDatFileContents.Split('\n')[0],
                cmeFileContents: FakeNymexFileContents.Split('\n')[0]);
            A.CallTo(() => mockFileSystem.GetFileSize(A<string>.Ignored)).Returns(1234);

            async Task RunTestImpl(IInputSource sut, string filepath) {
                var (description1, data1) = await sut.GetInputData();
                description1.Should().BeNullOrWhiteSpace();
                data1.Should().BeEmpty();

                var (description2, data2) = await sut.GetInputData();
                description2.Should().BeNullOrWhiteSpace();
                data2.Should().BeEmpty();

                A.CallTo(() => mockFileSystem.MoveFile(filepath, A<string>.That.Contains("Archived")))
                 .MustHaveHappenedOnceExactly();
            }

            await RunTestImpl(new IceDatFile(Config, mockFileSystem, Dtp, CancellationToken.None), TestHelpers.FakeIceFilepath1);
            await RunTestImpl(new NymexOptionFile(Config, mockFileSystem, Dtp, CancellationToken.None), TestHelpers.FakeNymexFilepath1);
        }

        [Fact]
        public async Task AWarningShouldBeLoggedIfAFileIsDeletedBeforeItIsRead() {
            var mockFileSystem = A.Fake<IFileSystemProxy>();
            A.CallTo(() => mockFileSystem.GetMatchingFileNames(TestHelpers.FakeDirectory, TestHelpers.IceFilePattern))
             .Returns(new [] {TestHelpers.FakeIceFilepath1}).Once().Then.Returns(Enumerable.Empty<string>());
            A.CallTo(() => mockFileSystem.GetMatchingFileNames(TestHelpers.FakeDirectory, TestHelpers.NymexFilePattern))
             .Returns(new [] {TestHelpers.FakeNymexFilepath1}).Once().Then.Returns(Enumerable.Empty<string>());
            A.CallTo(() => mockFileSystem.GetFileSize(A<string>.Ignored)).Returns(1234);

            async Task RunTestImpl(IInputSource sut, string filepath) {
                var loggerTarget = LogUtils.SetupTestLogger(LogLevel.Warn);

                var (description1, data1) = await sut.GetInputData();
                description1.Should().BeNullOrWhiteSpace();
                data1.Should().BeEmpty();

                var (description2, data2) = await sut.GetInputData();
                description2.Should().BeNullOrWhiteSpace();
                data2.Should().BeEmpty();

                loggerTarget.Logs.Count(m => m.Contains($"\"{filepath}\" was deleted before it was uploaded")).Should().Be(1);
            }

            await RunTestImpl(new IceDatFile(Config, mockFileSystem, Dtp, CancellationToken.None), TestHelpers.FakeIceFilepath1);
            await RunTestImpl(new NymexOptionFile(Config, mockFileSystem, Dtp, CancellationToken.None), TestHelpers.FakeNymexFilepath1);
        }

        private const string FakeIceDatFileContents = @"
TRADE DATE|HUB|PRODUCT|STRIP|CONTRACT|CONTRACT TYPE|STRIKE|SETTLEMENT PRICE|NET CHANGE|EXPIRATION DATE|PRODUCT_ID|OPTION_VOLATILITY|DELTA_FACTOR
5/10/2018|North Sea|Brent Crude Futures|10/1/2018|B|C|100.0000|0.25000|0.01000|8/28/2018|254|28.76|0.05168
";
        private const string FakeNymexFileContents = @"
BizDt,Sym,ID,StrkPx,SecTyp,MMY,MatDt,PutCall,Exch,Desc,LastTrdDt,BidPrice,OpeningPrice,SettlePrice,SettleDelta,HighLimit,LowLimit,DHighPrice,DLowPrice,HighBid,LowBid,PrevDayVol,PrevDayOI,FixingPrice,UndlyExch,UndlyID,UndlySecTyp,UndlyMMY,BankBusDay
""2018-07-02"",""BZO"",""BZO"",""68.0"",""OOF"",""201809"",""2018-07-26"",""1"",""NYMEX"","""",""2018-07-26"","""","""",""9.44"",""0.95153"","""",""0.01"","""","""",""10.72"",""9.35"",""58"",""1192"","""",""NYMEX"",""BZ"",""FUT"",""201809"",""""
";
    }
}
