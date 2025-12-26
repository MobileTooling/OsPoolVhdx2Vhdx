using System.Runtime.InteropServices;

namespace MobilePackageGen.Adapters.RealFileSystem
{
    public class Disk : IDisk
    {
        public IEnumerable<IPartition> Partitions
        {
            get;
        }

        public Disk(string path)
        {
            Logging.Log();

            Partitions = GetPartitionStructures(path);

            Logging.Log();
        }

        public Disk(List<IPartition> Partitions)
        {
            this.Partitions = Partitions;
        }

        private static List<IPartition> GetPartitionStructures(string path)
        {
            List<IPartition> partitions = [];

            // RawDisk is only supported on Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                DeferredStreamPartition partition = new(new RealFileSystemBridge(path), path.Replace(":", "").Replace(Path.DirectorySeparatorChar, '_'), Guid.Empty, Guid.Empty);
                partitions.Add(partition);
            }

            return partitions;
        }
    }
}
