using DiscUtils;
using DiscUtils.Streams;
using MobilePackageGen;
using StorageSpace;

namespace OsPoolVhdx2Vhdx
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Logging.Log("\nVHDX Containing a Storage Space OS Pool To Individual Storage Space VHDx(s) tool\nVersion: 1.0.0.0\n");

            if (args.Length < 2)
            {
                PrintHelp();
                return;
            }

            string[] inputArgs = args[..^1];
            string outputFolder = args[^1];

            IEnumerable<IDisk> disks = DiskLoader.LoadDisks(inputArgs);

            if (!disks.Any())
            {
                PrintHelp();
                return;
            }

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            DumpSpaces(disks, outputFolder);
        }

        public static void PrintHelp()
        {
            Logging.Log("\nVHDX Containing a Storage Space OS Pool To Individual Storage Space VHDx(s) tool\nVersion: 1.0.0.0\n");
        }

        public static void DumpSpaces(IEnumerable<IDisk> idisks, string outputDirectory)
        {
            foreach (IDisk idisk in idisks)
            {
                foreach (IPartition partition in idisk.Partitions)
                {
                    if (partition.Type == new Guid("E75CAF8F-F680-4CEE-AFA3-B001E56EFC2D"))
                    {
                        partition.Stream.Position = 0;
                        Pool pool = new(partition.Stream);

                        Dictionary<long, string> disks = pool.GetDisks();

                        foreach (KeyValuePair<long, string> disk in disks.OrderBy(x => x.Key).Skip(1))
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
