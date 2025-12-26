using DiscUtils;
using DiscUtils.Streams;
using DiscUtils.Wim;
using StorageSpace;

namespace MobilePackageGen.Adapters
{
    public static class DiskCommon
    {
        public static List<IDisk> GetUpdateOSDisks(List<IDisk> disks)
        {
            List<IDisk> updateOSDisks = [];

            foreach (IDisk disk in disks)
            {
                foreach (IPartition partition in disk.Partitions)
                {
                    IDisk? updateOSDisk = GetUpdateOSDisk(partition);
                    if (updateOSDisk != null)
                    {
                        updateOSDisks.Add(updateOSDisk);
                    }
                }
            }

            return updateOSDisks;
        }

        public static IDisk? GetUpdateOSDisk(IPartition partition)
        {
            if (partition.FileSystem != null)
            {
                IFileSystem fileSystem = partition.FileSystem;
                try
                {
                    // Handle UpdateOS as well if found
                    if (fileSystem.FileExists("PROGRAMS\\UpdateOS\\UpdateOS.wim"))
                    {
                        Logging.Log();

                        Logging.Log($"UpdateOS.wim {fileSystem.GetFileLength("PROGRAMS\\UpdateOS\\UpdateOS.wim")} WindowsImageFile");

                        List<IPartition> partitions = [];

                        Stream wimStream = fileSystem.OpenFile("PROGRAMS\\UpdateOS\\UpdateOS.wim", FileMode.Open, FileAccess.Read);
                        WimFile wimFile = new(wimStream);

                        for (int i = 0; i < wimFile.ImageCount; i++)
                        {
                            IFileSystem wimFileSystem = wimFile.GetImage(i);

                            Logging.Log($"- {i} WindowsImageIndex");

                            IPartition wimPartition = new FileSystemPartition(wimStream, wimFileSystem, $"{partition.Name.Replace("\0", "-")}-UpdateOS-{i}", Guid.Empty, Guid.Empty);
                            partitions.Add(wimPartition);
                        }

                        Logging.Log();

                        Wim.Disk updateOSDisk = new(partitions);
                        return updateOSDisk;
                    }

                    // Handle UpdateOS as well if found
                    if (fileSystem.FileExists("UpdateOS.wim"))
                    {
                        Logging.Log();

                        Logging.Log($"UpdateOS.wim {fileSystem.GetFileLength("UpdateOS.wim")} WindowsImageFile");

                        List<IPartition> partitions = [];

                        Stream wimStream = fileSystem.OpenFile("UpdateOS.wim", FileMode.Open, FileAccess.Read);
                        WimFile wimFile = new(wimStream);

                        for (int i = 0; i < wimFile.ImageCount; i++)
                        {
                            IFileSystem wimFileSystem = wimFile.GetImage(i);

                            Logging.Log($"- {i} WindowsImageIndex");

                            IPartition wimPartition = new FileSystemPartition(wimStream, wimFileSystem, $"{partition.Name.Replace("\0", "-")}-UpdateOS-{i}", Guid.Empty, Guid.Empty);
                            partitions.Add(wimPartition);
                        }

                        Logging.Log();

                        Wim.Disk updateOSDisk = new(partitions);
                        return updateOSDisk;
                    }
                }
                catch (Exception ex)
                {
                    Logging.Log($"Error: Looking up UpdateOS on file system failed! {ex.Message}", LoggingLevel.Error);
                }
            }

            return null;
        }

        public static List<(GPT.GPT.Partition, Stream)> GetPartitions(VirtualDisk virtualDisk)
        {
            List<(GPT.GPT.Partition, Stream)> partitions = [];

            int sectorSize = 4096;//virtualDisk.Geometry!.Value.BytesPerSector;

            IEnumerable<GPT.GPT.Partition> partitionTable = GetGPTPartitions(virtualDisk.Content, (uint)sectorSize);

            if (partitionTable != null)
            {
                foreach (GPT.GPT.Partition partitionInfo in partitionTable)
                {
                    Stream partitionStream = Open(partitionInfo, (uint)sectorSize, virtualDisk.Content);

                    partitions.Add((partitionInfo, partitionStream));

                    if (partitionInfo.PartitionTypeGuid == new Guid("E75CAF8F-F680-4CEE-AFA3-B001E56EFC2D"))
                    {
                        Logging.Log();

                        Logging.Log($"{partitionInfo.Name} {partitionInfo.PartitionGuid} {partitionInfo.PartitionTypeGuid} {partitionInfo.SizeInSectors * (uint)sectorSize} StoragePool");

                        Pool pool = new(partitionStream);

                        Dictionary<long, string> disks = pool.GetDisks();

                        foreach (KeyValuePair<long, string> disk in disks.OrderBy(x => x.Key))
                        {
                            using Space space = pool.OpenDisk(disk.Key);

                            Logging.Log($"- {disk.Key}: {disk.Value} ({space.Length}B / {space.Length / 1024 / 1024}MB / {space.Length / 1024 / 1024 / 1024}GB) StorageSpace");
                        }

                        Logging.Log();

                        foreach (KeyValuePair<long, string> disk in disks)
                        {
                            Space space = pool.OpenDisk(disk.Key);

                            int spaceSectorSize = TryDetectSectorSize(space);

                            IEnumerable<GPT.GPT.Partition> msPartitionTable = GetGPTPartitions(space, (uint)spaceSectorSize);

                            if (msPartitionTable != null)
                            {
                                foreach (GPT.GPT.Partition storageSpacePartition in msPartitionTable)
                                {
                                    partitions.Add((storageSpacePartition, Open(storageSpacePartition, (uint)spaceSectorSize, space)));
                                }
                            }
                        }
                    }
                }
            }

            return partitions;
        }

        private static SparseStream Open(GPT.GPT.Partition entry, uint SectorSize, Stream _diskData)
        {
            ulong start = entry.FirstSector * SectorSize;
            ulong end = (entry.LastSector + 1) * SectorSize;

            if ((long)end >= _diskData.Length)
            {
                end = (ulong)_diskData.Length;
            }

            return new SubStream(_diskData, (long)start, (long)(end - start));
        }

        private static IEnumerable<GPT.GPT.Partition> GetGPTPartitions(Stream diskStream, uint sectorSize)
        {
            diskStream.Seek(0, SeekOrigin.Begin);

            try
            {
                byte[] buffer = new byte[sectorSize * 2];
                diskStream.Read(buffer, 0, buffer.Length);
                diskStream.Seek(0, SeekOrigin.Begin);

                uint GPTBufferSize = MobilePackageGen.GPT.GPT.GetGPTSize(buffer, sectorSize);

                buffer = new byte[GPTBufferSize];
                diskStream.Read(buffer, 0, buffer.Length);
                diskStream.Seek(0, SeekOrigin.Begin);

                GPT.GPT GPT = new(buffer, sectorSize);

                return GPT.Partitions;
            }
            catch (Exception ex)
            {
                Console.WriteLine("EXCEPTION!");
                Console.WriteLine(ex);
                diskStream.Seek(0, SeekOrigin.Begin);
                return null;
            }
        }

        private static int TryDetectSectorSize(Stream diskStream)
        {
            // Default is 4096
            int sectorSize = 4096;

            if (diskStream.Length > 4096 * 2)
            {
                BinaryReader reader = new(diskStream);

                diskStream.Seek(512, SeekOrigin.Begin);
                byte[] header1 = reader.ReadBytes(8);

                diskStream.Seek(4096, SeekOrigin.Begin);
                byte[] header2 = reader.ReadBytes(8);

                string header1str = System.Text.Encoding.ASCII.GetString(header1);
                string header2str = System.Text.Encoding.ASCII.GetString(header2);

                if (header1str == "EFI PART")
                {
                    sectorSize = 512;
                }
                else if (header2str == "EFI PART")
                {
                    sectorSize = 4096;
                }
                else if (diskStream.Length % 512 == 0 && diskStream.Length % 4096 != 0)
                {
                    sectorSize = 512;
                }

                diskStream.Seek(0, SeekOrigin.Begin);
            }
            else
            {
                if (diskStream.Length % 512 == 0 && diskStream.Length % 4096 != 0)
                {
                    sectorSize = 512;
                }
            }

            return sectorSize;
        }

        public static void PrintDiskInfo(IEnumerable<IDisk> disks)
        {
            Logging.Log();
            Logging.Log("Found partitions with recognized file system:");
            Logging.Log();

            foreach (IDisk disk in disks)
            {
                foreach (IPartition partition in disk.Partitions)
                {
                    if (partition.FileSystem != null)
                    {
                        Logging.Log($"{partition.Name.Replace("\0", "-")} {partition.ID} {partition.Type} {partition.Size} KnownFS");
                    }
                    else if (partition.Type == new Guid("E75CAF8F-F680-4CEE-AFA3-B001E56EFC2D"))
                    {
                        Logging.Log($"{partition.Name.Replace("\0", "-")} {partition.ID} {partition.Type} {partition.Size} StoragePool");
                    }
                }
            }

            Logging.Log();
            Logging.Log("Found partitions with unrecognized file system:");
            Logging.Log();

            foreach (IDisk disk in disks)
            {
                foreach (IPartition partition in disk.Partitions)
                {
                    if (partition.FileSystem == null && partition.Type != new Guid("E75CAF8F-F680-4CEE-AFA3-B001E56EFC2D"))
                    {
                        Logging.Log($"{partition.Name.Replace("\0", "-")} {partition.ID} {partition.Type} {partition.Size} UnknownFS");
                    }
                }
            }
        }
    }
}
