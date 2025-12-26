
namespace MobilePackageGen
{
    public interface IDisk
    {
        IEnumerable<IPartition> Partitions
        {
            get;
        }
    }
}
