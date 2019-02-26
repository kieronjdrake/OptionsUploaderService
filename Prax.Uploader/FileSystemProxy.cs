using System.Collections.Generic;
using System.IO;

namespace Prax.Uploader {
    public class FileSystemProxy : IFileSystemProxy {
        public IEnumerable<string> GetMatchingFileNames(string directoryPath, string filePattern) {
            return Directory.GetFiles(directoryPath, filePattern);
        }

        public long GetFileSize(string filepath) {
            var file = new FileInfo(filepath);
            return file.Length;
        }

        public StreamReader GetFileContents(string filepath) {
            return File.OpenText(filepath);
        }

        public void MoveFile(string source, string destination) {
            File.Move(source, destination);
        }

        public bool FileExists(string filepath) {
            return File.Exists(filepath);
        }
    }
}