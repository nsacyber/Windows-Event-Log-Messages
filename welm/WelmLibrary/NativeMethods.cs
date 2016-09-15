using System;
using System.Runtime.InteropServices;

namespace WelmLibrary.Classic
{
    /// <summary>
    /// The documentation for all these functions and structures can be looked up in MSDN.
    /// </summary>
    internal class NativeMethods
    {
        private NativeMethods()
        {}

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hReservedNull, uint dwFlags);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [Flags]
        public enum LoadLibraryFlags : uint
        {
            LoadLibrary = 0x0, // acts the same as LoadLibrary so GetModuleFileName, GetModuleHandle, GetProcAddress will work
            DontResolveDllReferences = 0x1, // do not use, backwards compatability only, but GetModuleFileName et. al. works though
            LoadLibraryAsDataFile = 0x2, // GetModuleFileName, GetModuleHandle, GetProcAddress do not work when using this option
            LoadWithAlteredSearchPath = 0x8, // can't be combined with any other LoadLibrarySearch* flags
            LoadIgnoreCodeAuthzLevel = 0x10, // Windows 7+, requires KB2532445 to be installed
            LoadLibraryAsImageResource = 0x20, // Vista+, GetModuleFileName et. al. do not work
            LoadLibraryAsDataFileExclusive = 0x40, // Vista+, GetModuleFileName et. al. do not work
            LoadLibrarySearchDllLoadDir = 0x100, // Vista+, requires KB2533623 to be installed
            LoadLibrarySearchApplicationDir = 0x200, // Vista+, requires KB2533623 to be installed
            LoadLibrarySearchUserDirs = 0x400, // Vista+, requires KB2533623 to be installed
            LoadLibrarySearchSystem32 = 0x800, // Vista+, requires KB2533623 to be installed
            LoadLibrarySearchDefaultDirs = 0x1000 // Vista+, requires KB2533623 to be installed      
        }

        /**
        Since LoadLibraryAsDataFile is being used to load the event message files, using GetModuleFileName on the hModule handles inside the Enum* functions will not work
         
        StringBuilder builder = new StringBuilder(260);
        uint result = NativeMethods.GetModuleFileName(hModule, builder, builder.Capacity);
        
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint GetModuleFileName([In] IntPtr hModule, [Out] [MarshalAs(UnmanagedType.LPStr)] StringBuilder lpFilename, [In][MarshalAs(UnmanagedType.U4)] int nSize);
        **/

        public delegate bool EnumResTypeProc(IntPtr hModule, IntPtr lpType, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool EnumResourceTypes(IntPtr hModule, EnumResTypeProc lpEnumFunc, IntPtr lParam);

        public delegate bool EnumResNameProc(IntPtr hModule, IntPtr lpType, IntPtr lpName, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool EnumResourceNames(IntPtr hModule, IntPtr lpType, EnumResNameProc lpEnumFunc, IntPtr lParam);
        
        public delegate bool EnumResLangProc(IntPtr hModule, IntPtr lpType, IntPtr lpName, ushort wIDLanguage, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool EnumResourceLanguages(IntPtr hModule, IntPtr lpType, IntPtr lpName, EnumResLangProc lpEnumFunc, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr FindResourceEx(IntPtr hModule, IntPtr lpType, IntPtr lpName, ushort wLanguage);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr LockResource(IntPtr hResData);

        public static bool IsIntresource(IntPtr value)
        {
            return (uint)value <= short.MaxValue;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
        public struct MESSAGE_RESOURCE_BLOCK
        {
            public UInt32 LowId;
            public UInt32 HighId;
            public UInt32 OffsetToEntries;
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool Wow64DisableWow64FsRedirection(ref IntPtr ptr);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool Wow64RevertWow64FsRedirection(ref IntPtr ptr);

        // not supported on XP 64-bit
        //[return: MarshalAs(UnmanagedType.Bool)]
        //[DllImport("kernel32.dll", SetLastError = true)]
        //public static extern bool Wow64EnableWow64FSRedirection(bool Wow64EnableFsRedirection);
    }
}
