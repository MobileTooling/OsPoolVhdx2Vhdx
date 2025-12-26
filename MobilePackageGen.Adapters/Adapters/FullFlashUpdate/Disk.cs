using DiscUtils;
using DiscUtils.Streams;
using Img2Ffu.Reader;

namespace MobilePackageGen.Adapters.FullFlashUpdate
{
    public class Disk : IDisk
    {
        public IEnumerable<IPartition> Partitions
        {
            get;
        }

        public Disk(string ffuPath)
        {
            Logging.Log();

            Logging.Log($"{Path.GetFileName(ffuPath)} {new FileInfo(ffuPath).Length} FullFlashUpdate");

            List<(IEnumerable<(GPT.GPT.Partition, Stream)>, int, Stream)> partitionInfos = GetPartitions(ffuPath);

            List<IPartition> Partitions = [];

            foreach ((IEnumerable<(GPT.GPT.Partition, Stream)>, int, Stream) partitionInfo in partitionInfos)
            {
                Partitions.AddRange(GetPartitionStructures(partitionInfo.Item1, partitionInfo.Item2, partitionInfo.Item3));
            }

            this.Partitions = Partitions;

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

        private static List<(IEnumerable<(GPT.GPT.Partition, Stream)>, int, Stream)> GetPartitions(string ffuPath)
        {
            List<(IEnumerable<(GPT.GPT.Partition, Stream)>, int, Stream)> partitions = [];
            for (int i = 0; i < FullFlashUpdateReaderStream.GetStoreCount(ffuPath); i++)
            {
                FullFlashUpdateReaderStream store = new(ffuPath, (ulong)i);

                string FriendlyDevicePath = DevicePathUtils.FormatDevicePath(store.DevicePath);

                Logging.Log($"- {i}: {FriendlyDevicePath} {store.Length} FullFlashUpdateStore");

                long diskCapacity = store.Length;
                VirtualDisk virtualDisk = new DiscUtils.Raw.Disk(store, Ownership.None, Geometry.FromCapacity(diskCapacity, store.SectorSize));

                partitions.Add((DiskCommon.GetPartitions(virtualDisk), store.SectorSize, store));
            }

            return partitions;
        }
    }
}