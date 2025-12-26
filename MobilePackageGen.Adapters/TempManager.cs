namespace MobilePackageGen
{
    public class TempManager
    {
        private static readonly List<string> tempFiles = [];

        public static string GetTempFile()
        {
            string file = Path.GetTempFileName();
            File.Delete(file);
            tempFiles.Add(file);
            return file;
        }

        public static void CleanupTempFiles()
        {
            foreach (string file in tempFiles)
            {
                File.Delete(file);
            }
            tempFiles.Clear();
        }
    }
}
