using System.Collections.Generic;

namespace Prax.Uploader {

    public interface IFileInputSourceConfig {
        string Type { get; }
        string InputDirectory { get; }
        int FileReadRetryAttempts { get; }
        int InitialRetryDelayMs { get; }
    }

    public interface ITestInputSourceConfig {
        string Type { get; }
    }

    public interface IInputSourceConfig {
        IEnumerable<IFileInputSourceConfig> FileInputSources { get; }
        IEnumerable<ITestInputSourceConfig> TestInputSources { get; }
    }
}