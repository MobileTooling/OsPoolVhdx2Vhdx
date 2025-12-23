using DiscUtils;
using DiscUtils.Streams;
using StorageSpace;

namespace OsPoolVhdx2Vhdx
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Logging.Log("\nVHDX Containing a Storage Space OS Pool To Individual Storage Space VHDx(s) tool\nVersion: 1.0.0.0\n");

            if (args.Length != 2)
            {
                Logging.Log("Usage: OsPoolVhdx2Vhdx <Path to VHD(X) File with Storage Pool> <Output director for SPACEDisk.vhdx files>");
                return;
            }

            string VhdxPath = args[0];
            string OutputDirectory = args[1];

            if (!File.Exists(VhdxPath))
            {
                Logging.Log($"VHD(X) file does not exist: {VhdxPath}");
                return;
            }

            if (!Directory.Exists(OutputDirectory))
            {
                Directory.CreateDirectory(OutputDirectory);
            }

            DumpSpaces(VhdxPath, OutputDirectory);
        }

        public static void DumpSpaces(string vhdx, string outputDirectory)
        {
            VirtualDisk virtualDisk;
            if (vhdx.EndsWith(".vhd", StringComparison.InvariantCultureIgnoreCase))
            {
                virtualDisk = new DiscUtils.Vhd.Disk(vhdx, FileAccess.Read);
            }
            else
            {
                virtualDisk = new DiscUtils.Vhdx.Disk(vhdx, FileAccess.Read);
            }

            int sectorSize = virtualDisk.Geometry!.Value.BytesPerSector;//4096;//virtualDisk.Geometry!.Value.BytesPerSector;

            IEnumerable<GPT.GPT.Partition>? partitionTable = GetGPTPartitions(virtualDisk.Content, (uint)sectorSize);

            if (partitionTable != null)
            {
                foreach (GPT.GPT.Partition partitionInfo in partitionTable)
                {
                    Stream partitionStream = Open(partitionInfo, (uint)sectorSize, virtualDisk.Content);

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

                            DumpSpace(outputDirectory, disk.Value, space, spaceSectorSize);

                            space.Dispose();
                        }
                    }
                }
            }
        }

        private static void DumpSpace(string outputDirectory, string disk, Space space, int spaceSectorSize)
        {
            Logging.Log();

            string vhdFile = Path.Combine(outputDirectory, $"{disk}.vhdx");
            Logging.Log($"Dumping {vhdFile}...");

            long diskCapacity = space.Length;
            using Stream fs = new FileStream(vhdFile, FileMode.CreateNew, FileAccess.ReadWrite);
            using VirtualDisk outDisk = DiscUtils.Vhdx.Disk.InitializeDynamic(fs, Ownership.None, diskCapacity, Geometry.FromCapacity(diskCapacity, spaceSectorSize));

            DateTime now = DateTime.Now;
            void progressCallback(ulong readBytes, ulong totalBytes)
            {
                ShowProgress(readBytes, totalBytes, now);
            }

            Logging.Log($"Dumping {disk}");
            space.CopyTo(outDisk.Content, progressCallback);
            Logging.Log();
        }

        private static SubStream Open(GPT.GPT.Partition entry, uint SectorSize, Stream _diskData)
        {
            ulong start = entry.FirstSector * SectorSize;
            ulong end = (entry.LastSector + 1) * SectorSize;

            if ((long)end >= _diskData.Length)
            {
                end = (ulong)_diskData.Length;
            }

            return new SubStream(_diskData, (long)start, (long)(end - start));
        }

        private static List<GPT.GPT.Partition>? GetGPTPartitions(Stream diskStream, uint sectorSize)
        {
            diskStream.Seek(0, SeekOrigin.Begin);

            try
            {
                byte[] buffer = new byte[sectorSize * 2];
                diskStream.Read(buffer, 0, buffer.Length);
                diskStream.Seek(0, SeekOrigin.Begin);

                uint GPTBufferSize = OsPoolVhdx2Vhdx.GPT.GPT.GetGPTSize(buffer, sectorSize);

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

        protected static void ShowProgress(ulong readBytes, ulong totalBytes, DateTime startTime)
        {
            DateTime now = DateTime.Now;
            TimeSpan timeSoFar = now - startTime;

            TimeSpan remaining = readBytes != 0 ?
                TimeSpan.FromMilliseconds(timeSoFar.TotalMilliseconds / readBytes * (totalBytes - readBytes)) : TimeSpan.MaxValue;

            double speed = Math.Round(readBytes / 1024L / 1024L / timeSoFar.TotalSeconds);

            uint percentage = (uint)(readBytes * 100 / totalBytes);

            Logging.Log($"{Logging.GetDISMLikeProgressBar(percentage)} {speed}MB/s {remaining:hh\\:mm\\:ss\\.f}", returnLine: false);
        }
    }
}
