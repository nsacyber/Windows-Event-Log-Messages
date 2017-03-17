using System;
using System.CodeDom.Compiler;
using System.Runtime.InteropServices;

namespace WelmConsole
{
    [GeneratedCode("Brain","1.0")]
    public static class NativeMethods
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct PROCESSOR_INFO_UNION
        {
            [FieldOffset(0)]
            public UInt32 dwOemId;
            [FieldOffset(0)]
            public UInt16 wProcessorArchitecture;
            [FieldOffset(2)]
            public UInt16 wReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_INFO
        {
            public PROCESSOR_INFO_UNION uProcessorInfo;
            public UInt32 dwPageSize;
            public IntPtr lpMinimumApplicationAddress;
            public IntPtr lpMaximumApplicationAddress;
            public UIntPtr dwActiveProcessorMask;
            public UInt32 dwNumberOfProcessors;
            public UInt32 dwProcessorType;
            public UInt32 dwAllocationGranularity;
            public UInt16 wProcessorLevel;
            public UInt16 wProcessorRevision;
        }

        [DllImport("kernel32.dll")]
        public static extern void GetNativeSystemInfo(ref SYSTEM_INFO lpSystemInfo);
    }
}
