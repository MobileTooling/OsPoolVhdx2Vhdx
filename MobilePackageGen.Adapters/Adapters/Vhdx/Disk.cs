using DiscUtils;

namespace MobilePackageGen.Adapters.Vhdx
{
    public class Disk : IDisk
    {
        public IEnumerable<IPartition> Partitions
        {
            get;
        }

        public Disk(string vhdx)
        {
            Logging.Log();

            Logging.Log($"{Path.GetFileName(vhdx)} {new FileInfo(vhdx).Length} VirtualHardDisk");

            (IEnumerable<(GPT.GPT.Partition, Stream)>, int, Stream) partitionInfos = GetPartitions(vhdx);
            Partitions = GetPartitionStructures(partitionInfos.Item1, partitionInfos.Item2, partitionInfos.Item3);

            Logging.Log();
        }

        public Disk(List<IPartition> Partitions)
        {
            this.Partitions = Partitions;
        }

        private static List<IPartition> GetPartitionStructures(IEnumerable<(GPT.GPT.Partition, Stream)> partitionInfos, int SectorSize, Stream _diskData)
        {
            List<IPartition> partitions = [];

            foreach ((GPT.GPT.Partition, Stream) partitionInfo in partitionInfos)
            {
                IPartition partition = new FileSystemPartition(partitionInfo.Item2, partitionInfo.Item1.Name, partitionInfo.Item1.PartitionTypeGuid, partitionInfo.Item1.PartitionGuid);
                partitions.Add(partition);
            }

            return partitions;
        }

        private static (IEnumerable<(GPT.GPT.Partition, Stream)>, int, Stream) GetPartitions(string vhdx)
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

            return (DiskCommon.GetPartitions(virtualDisk), virtualDisk.Geometry!.Value.BytesPerSector, virtualDisk.Content);
        }
    }
}
