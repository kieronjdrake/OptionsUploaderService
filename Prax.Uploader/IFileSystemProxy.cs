using System.Collections.Generic;
using System.IO;

namespace Prax.Uploader {
    public interface IFileSystemProxy {
        IEnumerable<string> GetMatchingFileNames(string directoryPath, string filePattern);
        long GetFileSize(string filepath);
        StreamReader GetFileContents(string filepath);
        void MoveFile(string source, string destination);
        bool FileExists(string filepath);
    }
}