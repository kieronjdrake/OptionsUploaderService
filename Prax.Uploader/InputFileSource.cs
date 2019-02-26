using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using Prax.Utils;

namespace Prax.Uploader
{
    internal class InputFileSource {
        private readonly IFileSystemProxy _fileSystem;
        private readonly string _inputSourceName;
        private readonly string _inputDirectory;
        private readonly int _maxFileReadRetryAttempts;
        private Dictionary<string, long> _watchedFileSizes = new Dictionary<string, long>();
        private List<string> _filesReadyForUpload = new List<string>();
        private readonly List<string> _readFiles = new List<string>();
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly CancellationToken _ct;

        public InputFileSource(IFileSystemProxy fileSystem, string inputSourceName, string inputDirectory,
                               int maxFileReadRetryAttempts, CancellationToken ct) {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _inputSourceName = inputSourceName;
            _inputDirectory = inputDirectory ?? throw new ArgumentNullException(nameof(inputDirectory));
            _maxFileReadRetryAttempts = maxFileReadRetryAttempts;
            _ct = ct;
        }

        public async Task CheckInputDirectoryForInputFiles(string filePattern, int retryDelay, int attempt = 0) {
            try {
                CheckForInputFiles(filePattern);
            }
            catch (SystemException ex) {
                Logger.Warn(ex, $"Unable to read {_inputSourceName} input directory: {ex.Message}");
                
                var newRetryCount = ++attempt;
                if (newRetryCount > _maxFileReadRetryAttempts) {
                    Logger.Error("Exceeded retry count when trying to read {sourceName} directory {dir}",
                                 _inputSourceName, _inputDirectory);
                } else {
                    Logger.Info("Retrying {sourceName} file directory read: {attempt} of {maxAttempts}", 
                                _inputSourceName, newRetryCount, _maxFileReadRetryAttempts);
                    await Task.Delay(retryDelay, _ct);
                    await CheckInputDirectoryForInputFiles(filePattern, retryDelay * 2, newRetryCount);
                }
            }
        }

        public void MoveFilesToProcessedFolder(bool wasSuccess, string callerName) {
            var removed = new List<string>();
            foreach (var source in _readFiles) {
                MoveFileToProcessedFolder(wasSuccess, callerName, source);
                removed.Add(source);
            }
            removed.ForEach(f => _readFiles.Remove(f));
        }

        private void MoveFileToProcessedFolder(bool wasSuccess, string callerName, string source) {
            var subfolderName = wasSuccess ? "Archived" : "Error";
            var destination = Path.Combine(_inputDirectory, subfolderName, Path.GetFileName(source));
            while (_fileSystem.FileExists(destination)) {
                destination += ".copy";
            }
            Logger.Log(LogLevel.Info, $"{callerName}: moving {source} to {destination}");
            _fileSystem.MoveFile(source, destination);
        }

        public (string description, List<InputPriceData> data) ReadAllFilesReadyForUpload<TData>(
            Func<TextReader, IEnumerable<TData>> fileReader,
            Func<IEnumerable<TData>, IEnumerable<InputPriceData>> converter) {

            var toArchive = new List<(string, bool)>();

            // A typical ICE dat file has ~300k lines, this results in mem usage of ~120MB, so we're safe to
            // use eager evaluation here and downstream unless this has to run on a Raspberry Pi.
            List<InputPriceData> ReadFileWrapper(string filepath) {
                try {
                    using (var tr = _fileSystem.GetFileContents(filepath)) {
                        var fileContents = fileReader(tr);
                        var inputData = converter(fileContents).ToList();
                        if (inputData.IsEmpty()) toArchive.Add((filepath, true));
                        return inputData;
                    }
                }
                catch (Exception ex) {
                    Logger.Log(LogLevel.Error, ex, "Failed to read input file {path}: {message}", filepath, ex.Message);
                    toArchive.Add((filepath, false));
                    return Enumerable.Empty<InputPriceData>().ToList();
                }
            }

            var data = _filesReadyForUpload.SelectMany(ReadFileWrapper).ToList();

            toArchive.ForEach(x => {
                var (fp, success) = x;
                _filesReadyForUpload.Remove(fp);
                MoveFileToProcessedFolder(wasSuccess: success, callerName: "InputFileSource", source: fp);
            });

            // Once we've read the files move them to _readFiles to ensure they don't get read again
            if (_filesReadyForUpload.Any()) {
                _readFiles.AddRange(_filesReadyForUpload);
                _filesReadyForUpload.Clear();
            }

            return (GetSourceDescription, data);
        }

        private string GetSourceDescription => string.Join(",", _readFiles);

        private void CheckForInputFiles(string filePattern) {
            var currentDatFiles = _fileSystem.GetMatchingFileNames(_inputDirectory, filePattern).ToList();
            var notAlreadyRead = currentDatFiles.Where(f => !_readFiles.Contains(f)).ToList();
            var (newDat, existingDat, deletedDat) = notAlreadyRead.SymmetricSetDifference(_watchedFileSizes.Keys);
            
            deletedDat.ForEach(
                f => Logger.Warn("{sourceName} {filename} was deleted before it was uploaded", _inputSourceName, f));

            newDat.ForEach(f => Logger.Debug("{sourceName}: new file found: {f}", _inputSourceName, f));
            
            var existingFilesNewSizes = existingDat.ToDictionary(f => f, GetFileSize);
            var (fullyCopied, stillCopying) = existingDat.Partition(f => IsFileSizeTheSame(f, existingFilesNewSizes));

            var newFilesAndSizes = newDat.Select(f => (file: f, size: GetFileSize(f))).ToList();
            var (zeroSized, valid) = newFilesAndSizes.Partition(d => d.size == 0);
            var validNewFilesAndSizes = valid.ToDictionary(d => d.file, d => d.size);
            zeroSized.ForEach(d => Logger.Debug("Ignoring zero-byte file {f}", d.file));

            var stillCopyingFilesAndSizes = existingFilesNewSizes.Where(kv => stillCopying.Contains(kv.Key));
            _watchedFileSizes = stillCopyingFilesAndSizes.Concat(validNewFilesAndSizes).ToDictionary(kv => kv.Key, kv => kv.Value);

            _filesReadyForUpload = fullyCopied;
        }

        private bool IsFileSizeTheSame(string filename, Dictionary<string, long> existingFilesNewSizes) {
            var oldSize = _watchedFileSizes.Find(filename).IfNone(InvalidFileSize);
            var newSize = existingFilesNewSizes.Find(filename).IfNone(InvalidFileSize);
            Logger.Debug("Checking file {filename} filesize {oldSize} -> {newSize}", filename, oldSize, newSize);
            return oldSize == newSize && oldSize != InvalidFileSize;
        }

        private long GetFileSize(string filepath) {
            try {
                return _fileSystem.GetFileSize(filepath);
            }
            catch (Exception ex) {
                Logger.Debug(ex, "Exception when getting file size for {filename}: {message}", filepath, ex.Message);
                return InvalidFileSize;
            }
        }

        private const long InvalidFileSize = -1;
    }
}
