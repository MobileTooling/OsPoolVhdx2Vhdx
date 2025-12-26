using Archives.DiscUtils;
using DiscUtils;
using SevenZipExtractor;
using System.Runtime.InteropServices;

namespace MobilePackageGen.Adapters
{
    public class FileSystemPartition : IPartition
    {
        public string Name
        {
            get;
        }

        public Guid Type
        {
            get;
        }

        public Guid ID
        {
            get;
        }

        public long Size
        {
            get;
        }

        public IFileSystem? FileSystem
        {
            get;
        }

        public Stream Stream
        {
            get;
        }

        public FileSystemPartition(Stream Stream, string Name, Guid Type, Guid ID)
        {
            this.Stream = Stream;
            Size = Stream.Length;
            this.Name = Name;
            this.Type = Type;
            this.ID = ID;
            FileSystem = TryCreateFileSystem(Stream);
        }

        public FileSystemPartition(Stream Stream, IFileSystem FileSystem, string Name, Guid Type, Guid ID)
        {
            this.Stream = Stream;
            Size = Stream.Length;
            this.Name = Name;
            this.Type = Type;
            this.ID = ID;
            this.FileSystem = FileSystem;
        }

        private static IFileSystem? TryCreateFileSystem(Stream partitionStream)
        {
            bool isFAT = false;
            bool isNTFS = false;

            try
            {
                partitionStream.Seek(0, SeekOrigin.Begin);
                if (DiscUtils.Ntfs.NtfsFileSystem.Detect(partitionStream))
                {
                    isNTFS = true;
                    partitionStream.Seek(0, SeekOrigin.Begin);
                    return new DiscUtils.Ntfs.NtfsFileSystem(partitionStream);
                }

                partitionStream.Seek(0, SeekOrigin.Begin);
                if (DiscUtils.Fat.FatFileSystem.Detect(partitionStream))
                {
                    isFAT = true;
                    partitionStream.Seek(0, SeekOrigin.Begin);
                    return new DiscUtils.Fat.FatFileSystem(partitionStream);
                }
            }
            catch (Exception ex)
            {
                Logging.Log($"Error: Looking up file system using DiscUtils failed! {ex.Message}", LoggingLevel.Error);
            }

            // Archive Bridge is Windows only for now
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    // DiscUtils fat implementation is a bit broken, on top of not supporting lfn (which we know how to fix)
                    // It also fails to find the last chunk on some known partitions (like PLAT)
                    // As a result, we use a bridge file system making use of 7z, it's slower, but it's the best we can do for now

                    partitionStream.Seek(0x36, SeekOrigin.Begin);
                    byte[] buf = new byte[3];
                    partitionStream.Read(buf, 0, 3);
                    if (buf[0] == 'F' && buf[1] == 'A' && buf[2] == 'T' || isFAT)
                    {
                        partitionStream.Seek(0, SeekOrigin.Begin);
                        return new ArchiveBridge(partitionStream, SevenZipFormat.Fat);
                    }

                    if (isNTFS)
                    {
                        partitionStream.Seek(0, SeekOrigin.Begin);
                        return new ArchiveBridge(partitionStream, SevenZipFormat.Ntfs);
                    }
                }
                catch (Exception ex)
                {
                    Logging.Log($"Error: Looking up file system using 7z failed! {ex.Message}", LoggingLevel.Error);
                }
            }

            return null;
        }
    }
}
