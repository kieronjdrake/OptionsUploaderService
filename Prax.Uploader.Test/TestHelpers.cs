using System;
using System.IO;
using System.Text;
using FakeItEasy;
using FluentAssertions;
using Prax.Utils;
using LanguageExt;

namespace Prax.Uploader.Test
{
    public static class TestHelpers {
        
        public static readonly DateTime FakeToday = new DateTime(2018, 05, 11);
        public static readonly DateTime FakeNow = new DateTime(2018, 05, 11, 13, 14, 15);
        public static readonly DateTime FakeUtcNow = new DateTime(2018, 05, 11, 12, 14, 15);

        public const string FakeDirectory = @"zzz:\notarealdirectory";
        public const string FakeIceFilename1 = "iamadatfile.dat";
        public const string FakeIceFilename2 = "iamadatfile2.dat";
        public const string FakeNymexFilename1 = "iamanymexfile.s.csv";
        public const string FakeNymexFilename2 = "iamanymexfile2.s.csv";
        public static readonly string FakeIceFilepath1 = Path.Combine(FakeDirectory, FakeIceFilename1);
        public static readonly string FakeIceFilepath2 = Path.Combine(FakeDirectory, FakeIceFilename2);
        public static readonly string FakeNymexFilepath1 = Path.Combine(FakeDirectory, FakeNymexFilename1);
        public static readonly string FakeNymexFilepath2 = Path.Combine(FakeDirectory, FakeNymexFilename2);

        public const string IceFilePattern = "*.dat";
        public const string NymexFilePattern = "*.s.csv";

        public static IDateTimeProvider CreateDtp(DateTime? today = null, DateTime? now = null, DateTime? utcNow = null) {
            var dtp = A.Fake<IDateTimeProvider>();
            A.CallTo(() => dtp.UtcNow).Returns(utcNow ?? FakeUtcNow);
            A.CallTo(() => dtp.Now).Returns(now ?? FakeNow);
            A.CallTo(() => dtp.Today).Returns(today ?? FakeToday);
            return dtp;
        }

        public static IFileInputSourceConfig CreateConfig(string directory = null, int maxRetries = 3, int retryDelay = 10) {
            var config = A.Fake<IFileInputSourceConfig>();
            A.CallTo(() => config.InputDirectory).Returns(directory ?? FakeDirectory);
            A.CallTo(() => config.FileReadRetryAttempts).Returns(maxRetries);
            A.CallTo(() => config.InitialRetryDelayMs).Returns(retryDelay);
            return config;
        }

        public static StreamReader GenerateStreamFromString(string s) {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(s));
            return new StreamReader(stream);
        }
    }

    public static class OptionTestExtensions {
        public static void ShouldBe<T>(this Option<T> o1, Option<T> o2) {
            var ic1 = (IComparable<Option<T>>)o1;
            ic1.Should().Be(o2);
        }
    }
}
