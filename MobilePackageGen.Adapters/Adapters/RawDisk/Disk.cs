using DiscUtils;
using DiscUtils.Streams;

namespace MobilePackageGen.Adapters.RawDisk
{
    public class Disk : IDisk
    {
        public IEnumerable<IPartition> Partitions
        {
            get;
        }

        public Disk(Stream diskStream)
        {
            Logging.Log();

            (IEnumerable<(GPT.GPT.Partition, Stream)>, int, Stream) partitionInfos = GetPartitions(diskStream);
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

        private static (IEnumerable<(GPT.GPT.Partition, Stream)>, int, Stream) GetPartitions(Stream diskStream)
        {
            VirtualDisk virtualDisk = new DiscUtils.Raw.Disk(diskStream, Ownership.None);
            return (DiskCommon.GetPartitions(virtualDisk), 4096/*virtualDisk.Geometry!.Value.BytesPerSector*/, virtualDisk.Content);
        }
    }
}
