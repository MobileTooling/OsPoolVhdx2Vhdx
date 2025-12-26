using DiscUtils;
using DiscUtils.Wim;

namespace MobilePackageGen.Adapters.Wim
{
    public class Disk : IDisk
    {
        public IEnumerable<IPartition> Partitions
        {
            get;
        }

        public Disk(string wimPath)
        {
            Logging.Log();

            Logging.Log($"{Path.GetFileName(wimPath)} {new FileInfo(wimPath).Length} WindowsImageFile");

            Partitions = GetPartitionStructures(wimPath);

            Logging.Log();
        }

        public Disk(List<IPartition> Partitions)
        {
            this.Partitions = Partitions;
        }

        private static List<IPartition> GetPartitionStructures(string path)
        {
            List<IPartition> partitions = [];

            Stream wimStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            WimFile wimFile = new(wimStream);

            for (int i = 0; i < wimFile.ImageCount; i++)
            {
                IFileSystem wimFileSystem = wimFile.GetImage(i);

                Logging.Log($"- {i} WindowsImageIndex");

                IPartition wimPartition = new FileSystemPartition(wimStream, wimFileSystem, $"{Path.GetFileNameWithoutExtension(path)}-{i}", Guid.Empty, Guid.Empty);
                partitions.Add(wimPartition);
            }

            Logging.Log();

            return partitions;
        }
    }
}
