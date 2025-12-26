using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SevenZipExtractor
{
    internal class SevenZipHandle : IDisposable
    {
        private SafeLibraryHandle sevenZipSafeHandle;

        public SevenZipHandle(string sevenZipLibPath)
        {
            sevenZipSafeHandle = Kernel32Dll.LoadLibrary(sevenZipLibPath);

            if (sevenZipSafeHandle.IsInvalid)
            {
                throw new Win32Exception();
            }

            IntPtr functionPtr = Kernel32Dll.GetProcAddress(sevenZipSafeHandle, "GetHandlerProperty");

            // Not valid dll
            if (functionPtr == IntPtr.Zero)
            {
                sevenZipSafeHandle.Close();
                throw new ArgumentException();
            }
        }

        ~SevenZipHandle()
        {
            Dispose(false);
        }

        protected void Dispose(bool disposing)
        {
            if ((sevenZipSafeHandle != null) && !sevenZipSafeHandle.IsClosed)
            {
                sevenZipSafeHandle.Close();
            }

            sevenZipSafeHandle = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IInArchive CreateInArchive(Guid classId)
        {
            if (sevenZipSafeHandle == null)
            {
                throw new ObjectDisposedException("SevenZipHandle");
            }

            IntPtr procAddress = Kernel32Dll.GetProcAddress(sevenZipSafeHandle, "CreateObject");
            CreateObjectDelegate createObject = (CreateObjectDelegate)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(CreateObjectDelegate));

            object result;
            Guid interfaceId = typeof(IInArchive).GUID;
            createObject(ref classId, ref interfaceId, out result);

            return result as IInArchive;
        }
    }
}