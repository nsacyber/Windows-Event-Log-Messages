using System;
using System.Globalization;

namespace WelmLibrary
{
    public class NtStatus
    {
        /// <summary>
        /// The severity level associated with the NTSTATUS.
        /// </summary>
        public NtSeverity Severity { get; set; }

        /// <summary>
        /// Specifies whether the customer bit is set. Will always be 0 (false) for Microsoft code.
        /// </summary>
        public bool IsCustomer { get; set; }

        /// <summary>
        /// Specifies whether the reserved bit is set. Willl be 0 (false) for NSTATUS values (and Microsoft code). If set, then value can be translated to an HRESULT code.
        /// </summary>
        public bool IsReserved { get; set; }

        /// <summary>
        /// The facility value associated with the NTSTATUS. The facility specifies optional information about what component the NTSTATUS applies to.
        /// </summary>
        public uint Facility { get; set; }

        /// <summary>
        /// The code portion of the NTSTATUS. The code is what is associated with the Event ID column in the Event Viewer.
        /// </summary>
        public ushort Code { get; set; }

        /// <summary>
        /// The original value of the NTSTATUS.
        /// </summary>
        public long Value { get; set; }

        public NtStatus()
        {
            Severity = NtSeverity.Maximum;
            IsCustomer = false;
            IsReserved = false;
            Facility = 0;
            Code = 0;
            Value = 0;
        }

        /// <summary>
        /// Create an NTStatus based on the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        public NtStatus(long value)
        {
            Value = value;

            long severity = GetBits(31, 30, value);
            long c = GetBits(29, 29, value);
            long n = GetBits(28, 28, value);
            long facility = GetBits(27, 16, value);
            long code = GetBits(15, 0, value);

            //Severity = Enum.GetName(typeof(NtSeverity), severity);
            Severity = (NtSeverity)severity;
            IsCustomer = Convert.ToBoolean(c);
            IsReserved= Convert.ToBoolean(n);

            //Facility = Customer ? facility.ToString(CultureInfo.CurrentCulture) : Enum.GetName(typeof(NtFacility), facility);
            Facility = (uint)facility;

            Code = (ushort)code; // (ushort)(value & 0xFFFFL);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "Severity: {0},  Customer: {1}, Reserved: {2}, Facility: {3}, Code: {4}, Value: {5} ({5:X})", Severity, IsCustomer, IsReserved, Facility, Code, Value);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as NtStatus);
        }

        public bool Equals(NtStatus other)
        {
            bool equal = false;

            if (other != null)
            {
                if (Value == other.Value)
                {
                    equal = true;
                }
            }

            return equal;
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        /// <summary>
        /// Get specific bits from a value. It is 0-based so on a 32-bit value use 0 through 31.
        /// </summary>
        /// <param name="msb">The most significant bit number to start at, inclusive.</param>
        /// <param name="lsb">The least significant bit number to stop at, inclusive.</param>
        /// <param name="num">The number to take the bits from.</param>
        /// <returns>The value of the bits at the specified position.</returns>
        private static long GetBits(int msb, int lsb, long num)
        {
            return (num >> lsb) & ~(~0 << (msb - lsb + 1));
        }
    }

    /// <summary>
    /// Valid severity levels for an NTStatus.
    /// </summary>
    public enum NtSeverity
    {
        Success = 0,
        Information = 1, // actually it is Informational but for the sake of consistency with EventLevel, I will use the exact same spelling/terminology
        Warning = 2,
        Error = 3,
        Maximum = 4 // not actually defined but I'm adding it anyway
    }

    /// <summary>
    /// Valid facility values for an NTSTATUS. See MS-ERREF Appendix A and ntstatus.h in the DDK include/shared folder.
    /// </summary>
    public enum NtFacility
    {
        // facilities: https://msdn.microsoft.com/en-us/library/cc231214.aspx
        Null = 0, //not actually defined anywhere but this will suppress invalid facility log messages
        Debugger = 1, //MS-ERREF
        RpcRuntime = 2, //MS-ERREF
        RpcStubs = 3, //MS-ERREF
        IoError = 4, //MS-ERREF
        CodClassError = 6, //ntstatus.h
        Win32 = 7, //MS-ERREF
        NtCert = 8, //ntstatus.h
        Sspi = 9, //MS-ERREF
        TerminalServer = 10, //0x0A MS-ERREF
        MuiError = 11, //0x0B MS-ERREF
        UsbError = 16, //0x10 MS-ERREF
        HidError = 17, //0x11 MS-ERREF
        FirewireError = 18, //0x12 MS-ERREF
        ClusterError = 19, //0x13 MS-ERREF
        AcpiError = 20, //0x14 MS-ERREF
        SxsError = 21, //0x15 MS-ERREF
        ManifestError = 23, //0x17 ntstatus.h
        Transaction = 25, //0x19 MS-ERREF
        CommonLog = 26, //0x1A MS-ERREF
        Video = 27, //0x1B MS-ERREF
        FilterManager = 28, //0x1C MS-ERREF
        Monitor = 29, //0x1D MS-ERREF
        GraphicsKernel = 30, //0x1E MS-ERREF
        DriverFramework = 32, //0x20 MS-ERREF
        FveError = 33, //0x21 MS-ERREF
        FwpError = 34, //0x22 MS-ERREF
        NdisError = 35, //0x23 MS-ERREF
        Tpm = 41, //0x29 nstatus.h
        Rtpm = 42, //0x2A ntstatus.h
        Hypervisor = 53, //0x35 MS-ERREF
        IpSec = 54, //0x36 MS-ERREF
        Virtualization = 55, //0x37 nstatus.h
        VolumeManager = 56, //0x38 nstatus.h
        Bcd = 57, //0x39 ntstatus.h
        Dis = 60, //0x3C
        Win32NtUser = 62, //0x3E ntstatus.h
        Win32NtGdi = 63, //0x3F ntstatus.h
        ResumeKeyFilter = 64, //0x40 ntstatus.h
        Rdbss = 65, //0x41 ntstatus.h
        BthAtt = 66, //0x42 ntstatus.h
        SecureBoot = 67, //0x43 ntstatus.h
        AudioKernel = 68, //0x44 ntstatus.h
        Vsm = 69, //0x45 ntstatus.h
        VolSnap = 80, //0x50 nstatus.h
        SdBus = 81, //0x51 ntstatus.h
        SharedVhdx = 92, //0x5C ntstatus.h
        Smb = 93, //0x5D ntstatus.h
        Interix = 153, //0x99 ntstatus.h
        Spaces = 231, //0xE7 ntstatus.h
        SecurityCore = 232, //0xE8 ntstatus.h
        SystemIntegrity = 233, //0xE9 nstatus.h
        Licensing = 234, //0xEA ntstatus.h
        PlatformManifiest = 235, //0xEB ntstatus.h
        Maximum = 236 //0xEC ntstatus.h for Windows 10.0.14393.33 SDK
    }
}
