using RawDiskLib;
using System.Text;

namespace MobilePackageGen.Adapters.RealFileSystem
{
    internal class DiskPartitionUtils
    {
        public static void ExtractFromDiskAndCopy(List<PartitionInfo> pTable, string partitionName, string destinationFile)
        {
            try
            {
                List<PartitionInfo> s = [.. pTable.Where(p => p.PartitionName.Equals(partitionName, StringComparison.CurrentCultureIgnoreCase))];

                if (s.Count > 0)
                {
                    using RawDiskLib.RawDisk disk = new(DiskNumberType.PhysicalDisk, s[0].PhysicalNumber, FileAccess.Read);
                    byte[] partitionRaw = disk.ReadSectors(s[0].FirstLBA, s[0].LastLBA - s[0].FirstLBA);
                    string dirFromDest = Path.GetDirectoryName(destinationFile);
                    if (!Directory.Exists(dirFromDest))
                    {
                        _ = Directory.CreateDirectory(dirFromDest);
                    }
                    File.WriteAllBytes(destinationFile, partitionRaw);
                }
                else
                {
                    Logging.Log("Error: Partition not found! " + partitionName, LoggingLevel.Error);
                    throw new DriveNotFoundException();
                }
            }
            catch (Exception exception)
            {
                Logging.Log("Error: Partition: " + partitionName, LoggingLevel.Error);
                Logging.Log("Error: " + exception.Message, LoggingLevel.Error);
                throw;
            }
        }

        public static List<PartitionInfo> GetDiskDetail() //TODO: USE LETTER INSTEAD....
        {
            List<int> physicalDisks = Utils.GetAllAvailableDrives(DiskNumberType.PhysicalDisk).ToList();

            List<PartitionInfo> partition_tables = [];

            try
            {
                physicalDisks.ForEach(phy =>
                {
                    using RawDiskLib.RawDisk disk = new(DiskNumberType.PhysicalDisk, phy, FileAccess.Read);
                    if (disk.SectorCount < 34 * 2)
                    {
                        Logging.Log("Too low sector count: " + disk.SectorCount);
                        return;
                    }

                    byte[] GPT_header = disk.ReadSectors(0, 34 * 2);

                    //Remember all partitions.
                    partition_tables.AddRange(GetPartitionTables(GPT_header, disk.SectorSize, phy));
                });
            }
            catch (Exception exception)
            {
                Logging.Log("Error: " + exception.Message, LoggingLevel.Error);
                throw;
            }

            return partition_tables;
        }

        private static PartitionInfo[] GetPartitionTables(byte[] header, int sectorSize, int physicalNumber)
        {
            int offset = sectorSize * 2;
            bool finished = false;

            List<PartitionInfo> partitions = [];

            while (!finished && offset < header.Length - 2 * sectorSize)
            {
                byte[] sliced = SliceByteArray(header, 128, offset);
                offset += 128;

                PartitionInfo partition = new()
                {
                    PartitionType = new Guid(SliceByteArray(sliced, 16, 0)),
                    PartitionGuid = new Guid(SliceByteArray(sliced, 16, 16)),
                    FirstLBA = BitConverter.ToInt32([.. SliceByteArray(sliced, 8, 32)], 0),
                    LastLBA = BitConverter.ToInt32([.. SliceByteArray(sliced, 8, 40)], 0),
                    AttributeFlags = SliceByteArray(sliced, 8, 48),
                    PartitionName = Encoding.Unicode.GetString(SliceByteArray(sliced, 72, 56)).Replace("\0", ""),
                    PhysicalNumber = physicalNumber
                };

                if (partition.FirstLBA == 0 &&
                    partition.LastLBA == 0 &&
                    partition.PartitionGuid == Guid.Empty &&
                    partition.PartitionType == Guid.Empty)
                {
                    finished = true;
                }
                else
                {
                    partitions.Add(partition);
                }
            }

            return [.. partitions];
        }

        private static byte[] SliceByteArray(byte[] source, int length, int offset)
        {
            byte[] destinationFoo = new byte[length];
            Array.Copy(source, offset, destinationFoo, 0, length);
            return destinationFoo;
        }
    }
}