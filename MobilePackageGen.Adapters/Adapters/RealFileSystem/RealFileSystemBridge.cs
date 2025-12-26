using DiscUtils;
using DiscUtils.Streams;

namespace MobilePackageGen.Adapters.RealFileSystem
{
    public class RealFileSystemBridge : IFileSystem
    {
        private readonly string RootPath;

        public RealFileSystemBridge(string RootPath)
        {
            this.RootPath = RootPath;
        }

        public bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public bool IsThreadSafe
        {
            get
            {
                return true;
            }
        }

        // todo
        public DiscDirectoryInfo Root
        {
            get
            {
                return null;
            }
        }

        public long Size
        {
            get
            {
                return long.MaxValue;
            }
        }

        public long UsedSpace
        {
            get
            {
                return long.MaxValue;
            }
        }

        public long AvailableSpace
        {
            get
            {
                return 0;
            }
        }

        public bool SupportsUsedAvailableSpace
        {
            get
            {
                return false;
            }
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(Path.Combine(RootPath, path));
        }

        public bool Exists(string path)
        {
            return DirectoryExists(path) || FileExists(path);
        }

        public bool FileExists(string path)
        {
            return File.Exists(Path.Combine(RootPath, path));
        }

        public FileAttributes GetAttributes(string path)
        {
            return File.GetAttributes(Path.Combine(RootPath, path));
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            return Directory.EnumerateDirectories(Path.Combine(RootPath, path));
        }

        public IEnumerable<string> GetDirectories(string path, string searchPattern)
        {
            return Directory.EnumerateDirectories(Path.Combine(RootPath, path), searchPattern);
        }

        public IEnumerable<string> GetDirectories(string path, string searchPattern, SearchOption searchOption)
        {
            return Directory.EnumerateDirectories(Path.Combine(RootPath, path), searchPattern, searchOption);
        }

        public DiscDirectoryInfo GetDirectoryInfo(string path)
        {
            throw new NotImplementedException();
        }

        public DiscFileInfo GetFileInfo(string path)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetFileSystemEntries(string path)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetFileSystemEntries(string path, string searchPattern)
        {
            throw new NotImplementedException();
        }

        public DiscFileSystemInfo GetFileSystemInfo(string path)
        {
            throw new NotImplementedException();
        }

        public byte[] ReadBootCode()
        {
            throw new NotImplementedException();
        }

        public long GetFileLength(string path)
        {
            return new FileInfo(Path.Combine(RootPath, path)).Length;
        }

        public IEnumerable<string> GetFiles(string path)
        {
            return Directory.EnumerateFiles(Path.Combine(RootPath, path));
        }

        public IEnumerable<string> GetFiles(string path, string searchPattern)
        {
            return Directory.EnumerateFiles(Path.Combine(RootPath, path), searchPattern);
        }

        public IEnumerable<string> GetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            return Directory.EnumerateFiles(Path.Combine(RootPath, path), searchPattern, searchOption);
        }

        public DateTime GetLastAccessTime(string path)
        {
            return File.GetLastAccessTime(Path.Combine(RootPath, path));
        }

        public DateTime GetLastAccessTimeUtc(string path)
        {
            return File.GetLastAccessTimeUtc(Path.Combine(RootPath, path));
        }

        public DateTime GetLastWriteTime(string path)
        {
            return File.GetLastWriteTime(Path.Combine(RootPath, path));
        }

        public DateTime GetLastWriteTimeUtc(string path)
        {
            return File.GetLastWriteTimeUtc(Path.Combine(RootPath, path));
        }

        public DateTime GetCreationTime(string path)
        {
            return File.GetCreationTime(Path.Combine(RootPath, path));
        }

        public DateTime GetCreationTimeUtc(string path)
        {
            return File.GetCreationTimeUtc(Path.Combine(RootPath, path));
        }

        public SparseStream OpenFile(string path, FileMode mode)
        {
            FileStream stream = File.Open(Path.Combine(RootPath, path), mode, FileAccess.ReadWrite, FileShare.ReadWrite);
            return SparseStream.FromStream(stream, Ownership.Dispose);
        }

        public SparseStream OpenFile(string path, FileMode mode, FileAccess access)
        {
            FileStream stream = File.Open(Path.Combine(RootPath, path), mode, access, FileShare.ReadWrite);
            return SparseStream.FromStream(stream, Ownership.Dispose);
        }

        #region writing routines

        public void CopyFile(string sourceFile, string destinationFile)
        {
            throw new NotImplementedException();
        }

        public void CreateDirectory(string path)
        {
            throw new NotImplementedException();
        }

        public void DeleteDirectory(string path)
        {
            throw new NotImplementedException();
        }

        public void DeleteDirectory(string path, bool recursive)
        {
            throw new NotImplementedException();
        }

        public void DeleteFile(string path)
        {
            throw new NotImplementedException();
        }

        public void MoveDirectory(string sourceDirectoryName, string destinationDirectoryName)
        {
            throw new NotImplementedException();
        }

        public void MoveFile(string sourceName, string destinationName)
        {
            throw new NotImplementedException();
        }

        public void MoveFile(string sourceName, string destinationName, bool overwrite)
        {
            throw new NotImplementedException();
        }

        public void SetAttributes(string path, FileAttributes newValue)
        {
            throw new NotImplementedException();
        }

        public void SetCreationTime(string path, DateTime newTime)
        {
            throw new NotImplementedException();
        }

        public void SetCreationTimeUtc(string path, DateTime newTime)
        {
            throw new NotImplementedException();
        }

        public void SetLastAccessTime(string path, DateTime newTime)
        {
            throw new NotImplementedException();
        }

        public void SetLastAccessTimeUtc(string path, DateTime newTime)
        {
            throw new NotImplementedException();
        }

        public void SetLastWriteTime(string path, DateTime newTime)
        {
            throw new NotImplementedException();
        }

        public void SetLastWriteTimeUtc(string path, DateTime newTime)
        {
            throw new NotImplementedException();
        }

        public void CopyFile(string sourceFile, string destinationFile, bool overwrite)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
