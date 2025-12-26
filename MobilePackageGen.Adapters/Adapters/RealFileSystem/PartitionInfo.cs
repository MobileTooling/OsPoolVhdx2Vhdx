namespace MobilePackageGen.Adapters.RealFileSystem
{
    public struct PartitionInfo
    {
        public Guid PartitionType
        {
            get; set;
        }
        public Guid PartitionGuid
        {
            get; set;
        }
        public int FirstLBA
        {
            get; set;
        }
        public int LastLBA
        {
            get; set;
        }
        public byte[] AttributeFlags
        {
            get; set;
        }
        public string PartitionName
        {
            get; set;
        }
        public int PhysicalNumber
        {
            get; set;
        }
    }
}
