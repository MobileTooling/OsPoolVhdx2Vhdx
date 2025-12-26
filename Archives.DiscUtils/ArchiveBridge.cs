using DiscUtils;
using DiscUtils.Internal;
using DiscUtils.Streams;
using SevenZipExtractor;

namespace Archives.DiscUtils
{
    public class ArchiveBridge : IFileSystem
    {
        private readonly Stream stream;
        private readonly SevenZipFormat sevenZipFormat;

        public ArchiveBridge(Stream stream, SevenZipFormat sevenZipFormat)
        {
            this.stream = stream;
            this.sevenZipFormat = sevenZipFormat;
            using ArchiveFile archiveFile = new(stream, sevenZipFormat);
            IList<Entry> _ = archiveFile.Entries; // Prefetch entries
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
                using ArchiveFile archiveFile = new(stream, sevenZipFormat);
                return archiveFile.GetArchiveSize();
            }
        }

        public long UsedSpace
        {
            get
            {
                using ArchiveFile archiveFile = new(stream, sevenZipFormat);
                return archiveFile.GetArchiveSize();
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
            using ArchiveFile archiveFile = new(stream, sevenZipFormat);
            return archiveFile.Entries.Any(x => x.IsFolder && x.FileName.Equals(path, StringComparison.InvariantCultureIgnoreCase));
        }

        public bool Exists(string path)
        {
            using ArchiveFile archiveFile = new(stream, sevenZipFormat);
            return archiveFile.Entries.Any(x => x.FileName.Equals(path, StringComparison.InvariantCultureIgnoreCase));
        }

        public bool FileExists(string path)
        {
            using ArchiveFile archiveFile = new(stream, sevenZipFormat);
            return archiveFile.Entries.Any(x => !x.IsFolder && x.FileName.Equals(path, StringComparison.InvariantCultureIgnoreCase));
        }

        public FileAttributes GetAttributes(string path)
        {
            using ArchiveFile archiveFile = new(stream, sevenZipFormat);
            Entry entry = archiveFile.Entries.First(x => x.FileName.Equals(path, StringComparison.InvariantCultureIgnoreCase));
            return (FileAttributes)entry.Attributes;
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            return GetDirectories(path, null, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<string> GetDirectories(string path, string searchPattern)
        {
            return GetDirectories(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<string> GetDirectories(string path, string searchPattern, SearchOption searchOption)
        {
            Func<string, bool>? re = null;

            if (!string.IsNullOrEmpty(searchPattern))
            {
                re = Utilities.ConvertWildcardsToRegEx(searchPattern, true);
            }

            using ArchiveFile archiveFile = new(stream, sevenZipFormat);
            IEnumerable<string> prereq = archiveFile.Entries
                .Where(x => x.IsFolder && x.FileName.StartsWith(path, StringComparison.InvariantCultureIgnoreCase) && (re == null || re(x.FileName)))
                .Select(x => x.FileName);
            switch (searchOption)
            {
                case SearchOption.AllDirectories:
                    {
                        return prereq.ToArray();
                    }
                case SearchOption.TopDirectoryOnly:
                    {
                        int expectedCount = path.Count(x => x == '\\');
                        if (!path.EndsWith("\\"))
                        {
                            expectedCount++;
                        }

                        return prereq.Where(x => x.Count(x => x == '\\') == expectedCount).ToArray();
                    }
            }

            return Array.Empty<string>();
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
            using ArchiveFile archiveFile = new(stream, sevenZipFormat);
            Entry entry = archiveFile.Entries.First(x => x.FileName.Equals(path, StringComparison.InvariantCultureIgnoreCase));
            return (long)entry.Size;
        }

        public IEnumerable<string> GetFiles(string path)
        {
            return GetFiles(path, null, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<string> GetFiles(string path, string searchPattern)
        {
            return GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        public IEnumerable<string> GetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            Func<string, bool>? re = null;

            if (!string.IsNullOrEmpty(searchPattern))
            {
                re = Utilities.ConvertWildcardsToRegEx(searchPattern, true);
            }

            using ArchiveFile archiveFile = new(stream, sevenZipFormat);
            IEnumerable<string> prereq = archiveFile.Entries
                .Where(x => !x.IsFolder && x.FileName.StartsWith(path, StringComparison.InvariantCultureIgnoreCase) && (re == null || re(x.FileName)))
                .Select(x => x.FileName);
            switch (searchOption)
            {
                case SearchOption.AllDirectories:
                    {
                        return prereq.ToArray();
                    }
                case SearchOption.TopDirectoryOnly:
                    {
                        int expectedCount = path.Count(x => x == '\\');
                        if (!path.EndsWith("\\"))
                        {
                            expectedCount++;
                        }

                        return prereq.Where(x => x.Count(x => x == '\\') == expectedCount).ToArray();
                    }
            }

            return Array.Empty<string>();
        }

        public DateTime GetLastAccessTime(string path)
        {
            using ArchiveFile archiveFile = new(stream, sevenZipFormat);
            Entry entry = archiveFile.Entries.First(x => x.FileName.Equals(path, StringComparison.InvariantCultureIgnoreCase));
            return entry.LastAccessTime;
        }

        public DateTime GetLastAccessTimeUtc(string path)
        {
            using ArchiveFile archiveFile = new(stream, sevenZipFormat);
            Entry entry = archiveFile.Entries.First(x => x.FileName.Equals(path, StringComparison.InvariantCultureIgnoreCase));
            return entry.LastAccessTime.ToUniversalTime();
        }

        public DateTime GetLastWriteTime(string path)
        {
            using ArchiveFile archiveFile = new(stream, sevenZipFormat);
            Entry entry = archiveFile.Entries.First(x => x.FileName.Equals(path, StringComparison.InvariantCultureIgnoreCase));
            return entry.LastWriteTime;
        }

        public DateTime GetLastWriteTimeUtc(string path)
        {
            using ArchiveFile archiveFile = new(stream, sevenZipFormat);
            Entry entry = archiveFile.Entries.First(x => x.FileName.Equals(path, StringComparison.InvariantCultureIgnoreCase));
            return entry.LastWriteTime.ToUniversalTime();
        }

        public DateTime GetCreationTime(string path)
        {
            using ArchiveFile archiveFile = new(stream, sevenZipFormat);
            Entry entry = archiveFile.Entries.First(x => x.FileName.Equals(path, StringComparison.InvariantCultureIgnoreCase));
            return entry.CreationTime;
        }

        public DateTime GetCreationTimeUtc(string path)
        {
            using ArchiveFile archiveFile = new(stream, sevenZipFormat);
            Entry entry = archiveFile.Entries.First(x => x.FileName.Equals(path, StringComparison.InvariantCultureIgnoreCase));
            return entry.CreationTime.ToUniversalTime();
        }

        public SparseStream OpenFile(string path, FileMode mode)
        {
            using ArchiveFile archiveFile = new(stream, sevenZipFormat);
            Entry entry = archiveFile.Entries.First(x => x.FileName.Equals(path, StringComparison.InvariantCultureIgnoreCase));
            MemoryStream memstrm = new();
            entry.Extract(memstrm);
            memstrm.Seek(0, SeekOrigin.Begin);
            return SparseStream.FromStream(memstrm, Ownership.Dispose);
        }

        public SparseStream OpenFile(string path, FileMode mode, FileAccess access)
        {
            using ArchiveFile archiveFile = new(stream, sevenZipFormat);
            Entry entry = archiveFile.Entries.First(x => x.FileName.Equals(path, StringComparison.InvariantCultureIgnoreCase));
            MemoryStream memstrm = new();
            entry.Extract(memstrm);
            memstrm.Seek(0, SeekOrigin.Begin);
            return SparseStream.FromStream(memstrm, Ownership.Dispose);
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