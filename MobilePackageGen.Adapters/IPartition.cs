using DiscUtils;

namespace MobilePackageGen
{
    public interface IPartition
    {
        IFileSystem? FileSystem
        {
            get;
        }

        string Name
        {
            get;
        }

        Guid ID
        {
            get;
        }

        Guid Type
        {
            get;
        }

        long Size
        {
            get;
        }

        Stream Stream
        {
            get;
        }
    }
}
