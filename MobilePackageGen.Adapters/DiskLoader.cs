using MobilePackageGen.Adapters;

namespace MobilePackageGen
{
    public static class DiskLoader
    {
        public static IEnumerable<IDisk> LoadDisks(string[] inputArgs)
        {
            Logging.Log("Getting Disks...");

            List<IDisk> disks = [];

            foreach (string inputArg in inputArgs)
            {
                if (Directory.Exists(inputArg))
                {
                    if (Directory.Exists(Path.Combine(inputArg, @"Windows\Servicing\Packages")) || Directory.Exists(Path.Combine(inputArg, @"Windows\Packages\DsmFiles")))
                    {
                        disks.Add(new Adapters.RealFileSystem.Disk(inputArg));
                    }
                    else
                    {
                        string[] files = Directory.EnumerateFiles(inputArg).ToArray();

                        foreach (string file in files)
                        {
                            IDisk? disk = LoadFile(file);
                            if (disk != null)
                            {
                                disks.Add(disk);
                            }
                        }
                    }
                }
                else if (File.Exists(inputArg))
                {
                    IDisk? disk = LoadFile(inputArg);
                    if (disk != null)
                    {
                        disks.Add(disk);
                    }
                }
            }

            if (disks.Count == 0)
            {
                return disks;
            }

            Logging.Log("Getting Update OS Disks...");

            disks.AddRange(DiskCommon.GetUpdateOSDisks(disks));

            DiskCommon.PrintDiskInfo(disks);

            return disks;

        }
        private static IDisk? LoadFile(string file)
        {
            string extension = Path.GetExtension(file);
            switch (extension.ToLowerInvariant())
            {
                case ".xml":
                    {
                        Stream rawProgramStream = QualcommEDLProgramStream.ParseProgramXML.GetStream(file, 4096)!;
                        return new Adapters.RawDisk.Disk(rawProgramStream);
                    }
                case ".wim":
                    {
                        return new Adapters.Wim.Disk(file);
                    }
                case ".ffu":
                    {
                        return new Adapters.FullFlashUpdate.Disk(file);
                    }
                case ".vhd":
                    {
                        return new Adapters.Vhdx.Disk(file);
                    }
                case ".vhdx":
                    {
                        return new Adapters.Vhdx.Disk(file);
                    }
                case ".img":
                    {
                        return new Adapters.RawDisk.Disk(File.OpenRead(file));
                    }
                case ".bin":
                    {
                        return new Adapters.RawDisk.Disk(File.OpenRead(file));
                    }
                default:
                    {
                        return null;
                    }
            }
        }
    }
}
