using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Prax.Utils;

namespace Prax.Uploader {
    public static class InputSourceFactory {
        public static IEnumerable<IInputSource> CreateInputSources(IInputSourceConfig config, CancellationToken ct) {
            var testInputs = config.TestInputSources.Select(CreateTestInputSource);
            var fileInputs = config.FileInputSources.Select(c => CreateFileInputSource(c, ct));
            return testInputs.Concat(fileInputs);
        }

        private static IInputSource CreateFileInputSource(IFileInputSourceConfig config, CancellationToken ct) {
            switch (config.Type) {
                case "IceDatFile":
                    return new IceDatFile(config, new FileSystemProxy(), new DateTimeProvider(), ct);
                case "NymexOptionFile":
                    return new NymexOptionFile(config, new FileSystemProxy(), new DateTimeProvider(), ct);
                default:
                    throw new Exception($"Invalid input source type: {config.Type}");
            }
        }

        private static IInputSource CreateTestInputSource(ITestInputSourceConfig config) {
            return new TestInputSource();
        }
    }
}