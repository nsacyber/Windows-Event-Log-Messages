using Microsoft.Win32;
using System;
using System.Globalization;
using System.Linq;
using System.Management;

namespace WelmConsole
{
    public class OperatingSystem
    {
        /// <summary>
        /// Operating system version.
        /// </summary>
        public Version Version { get; }

        /// <summary>
        /// Operating system edition.
        /// </summary>
        public string Edition { get; }

        /// <summary>
        /// Operating system release identifier (Windows 10+).
        /// </summary>
        public UInt32 ReleaseId { get; }

        /// <summary>
        /// Operating system service pack (Windows 7 and earlier).
        /// </summary>
        public UInt32 ServicePack { get; }

        /// <summary>
        /// Operating system name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Operating system architecture (x64,x86).
        /// </summary>
        public string Architecture { get; }

        /// <summary>
        /// The identifier WELM uses to uniquely identify an operating system.
        /// </summary>
        public string WelmId { get; }

        public OperatingSystem()
        {
            Version = GetVersion();
            Edition = GetEdition(Version);
            ReleaseId = GetReleaseId(Version);
            ServicePack = GetServicePack();
            Name = GetName();
            Architecture = GetArchitecture();
            WelmId = GetWelmId(Edition, ReleaseId, Version, ServicePack, Name, Architecture);
        }

        /// <summary>
        /// Tests if the operating systems is Windows 10 or later.
        /// </summary>
        /// <returns>Returns true if the operating systems is Windows 10 or later, otherwise false.</returns>
        private static bool IsWindows10OrLater()
        {
            bool isWindows10OrLater = false;

            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                using (RegistryKey currentKey = hklm.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (currentKey != null)
                    {
                        string[] names = currentKey.GetValueNames();

                        if (names.Contains("CurrentMajorVersionNumber"))
                        {
                            UInt32 version = (UInt32) (Int32) currentKey.GetValue("CurrentMajorVersionNumber", 0);
                            isWindows10OrLater = version >= 10;
                        }
                    }
                }
            }

            return isWindows10OrLater;
        }

        /// <summary>
        /// Gets the operating system edition.
        /// </summary>
        /// <returns>The operating system edition.</returns>
        private static string GetEdition(Version version)
        {
            string edition = "";

            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                using (RegistryKey currentKey = hklm.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (currentKey != null)
                    {
                        string[] names = currentKey.GetSubKeyNames();

                        if (names.Contains("EditionID"))
                        {
                            edition = (string) currentKey.GetValue("EditionID", "");
                        }
                        else
                        {
                            string product = "";

                            // ProductName registry value does not contain edition information in Windows XP 
                            // so retrieve Caption with WMI since it has edition at the end
                            if (version.Major == 5 && version.Minor == 1)
                            {
                                string query = "SELECT * FROM Win32_OperatingSystem WHERE Primary='true'";

                                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                                {
                                    using (ManagementObject mgmtObject = searcher.Get().Cast<ManagementObject>().FirstOrDefault())
                                    {
                                        if (mgmtObject != null)
                                        {
                                            product = mgmtObject.Properties["Caption"].Value.ToString();
                                        }
                                    }
                                }
                            }
                            else
                            {
                                product = (string)currentKey.GetValue("ProductName", "");
                            }

                            product =
                                product.Replace("Microsoft", "")
                                    .Replace("Windows", "")
                                    .Replace("Vista", "")
                                    .Replace("XP", "")
                                    .Replace("Service Pack", "")
                                    .Replace("Server", "")
                                    .Replace("R2", "")
                                    .Replace("\u00A9", "")
                                    .Replace("\u00AE", "")
                                    .Replace("\u2122", "")
                                    .Replace("(R)", "")
                                    .Replace("(TM)", "")
                                    .Replace("  ", " ")
                                    .Trim();

                            string[] productParts = product.Split(new []{' '},
                                StringSplitOptions.RemoveEmptyEntries);

                            foreach (string part in productParts)
                            {
                                UInt32 intGarbage;
                                Decimal decimalGarbage;

                                if (!(UInt32.TryParse(part.Trim(), out intGarbage) || Decimal.TryParse(part.Trim(), out decimalGarbage)))
                                {
                                    edition = part.Trim();
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return edition;
        }

        /// <summary>
        /// Gets the operating system release identifier (Windows 10 and later).
        /// </summary>
        /// <returns>The operating system release identifier.</returns>
        private static UInt32 GetReleaseId(Version version)
        {
            UInt32 release = 0;

            if (version.Major >= 10)
            {
                using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    using (RegistryKey currentKey = hklm.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion"))
                    {
                        if (currentKey != null)
                        {
                            string[] names = currentKey.GetValueNames();

                            if (names.Contains("ReleaseId"))
                            {
                                UInt32.TryParse((string) currentKey.GetValue("ReleaseId", ""), out release);
                            }
                            else
                            {
                                // there was no ReleaseID registry value name in Windows 10 1507
                                release = 1507;
                            }
                        }
                    }
                }
            }

            return release;
        }

        /// <summary>
        /// Gets the operating system version.
        /// </summary>
        /// <returns>The operating system version.</returns>
        private static Version GetVersion()
        {
            Version osVersion = new Version();

            if (IsWindows10OrLater())
            {
                using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    using (RegistryKey currentKey = hklm.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion"))
                    {
                        if (currentKey != null)
                        {
                            Int32 major = (Int32) currentKey.GetValue("CurrentMajorVersionNumber", 0);
                            Int32 minor = (Int32) currentKey.GetValue("CurrentMinorVersionNumber", 0);

                            Int32 build;

                            Int32.TryParse((string) currentKey.GetValue("CurrentBuild", ""), out build);

                            Int32 revision = (Int32) currentKey.GetValue("UBR", 0);
                            osVersion = new Version(major, minor, build, revision);
                        }
                    }
                }
            }
            else
            {
                using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    using (RegistryKey currentKey = hklm.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion"))
                    {
                        if (currentKey != null)
                        {
                            string currentVersion = (string) currentKey.GetValue("CurrentVersion", "");

                            Int32 major;
                            Int32.TryParse(currentVersion.Split('.')[0], out major);

                            Int32 minor;
                            Int32.TryParse(currentVersion.Split('.')[1], out minor);

                            Int32 build;

                            if (!Int32.TryParse((string) currentKey.GetValue("CurrentBuild", ""), out build))
                            {
                                // Windows XP's CurrentBuild has text saying it is obsolete so use CurrentBuildNumber instead
                                Int32.TryParse((string) currentKey.GetValue("CurrentBuildNumber", ""), out build);
                            }

                            osVersion = new Version(major, minor, build, 0);
                        }
                    }
                }
            }

            return osVersion;
        }

        /// <summary>
        /// Gets the operating system service pack (Windows 7 and earlier).
        /// </summary>
        /// <returns>The operating system service pack</returns>
        private static UInt32 GetServicePack()
        {
            UInt32 servicePack = 0;

            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                using (RegistryKey windowsKey = hklm.OpenSubKey(@"System\CurrentControlSet\Control\Windows"))
                {
                    if (windowsKey != null)
                    {
                        UInt32 sp = (UInt32) (Int32) windowsKey.GetValue("CSDVersion", 0);

                        servicePack = sp >> 8;
                    }
                }
            }

            return servicePack;
        }

        /// <summary>
        /// Gets the operating system name.
        /// </summary>
        /// <returns>The operating system name.</returns>
        private static string GetName()
        {
            string name = "";
            string productName = "";
            object other = "";

            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            {
                using (RegistryKey currentKey = hklm.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion"))
                {
                    if (currentKey != null)
                    {
                        productName = (string) currentKey.GetValue("ProductName", "");
                        productName =
                            productName.Replace("\u00A9", "")
                                .Replace("\u00AE", "")
                                .Replace("\u2122", "")
                                .Replace("(R)", "")
                                .Replace("(TM)", "")
                                .Replace("  ", " ")
                                .Trim();
                    }
                }
            }

            string query = "SELECT * FROM Win32_OperatingSystem WHERE Primary='true'";

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            {
                using (ManagementObject mgmtObject = searcher.Get().Cast<ManagementObject>().FirstOrDefault())
                {
                    if (mgmtObject != null)
                    {
                        other = mgmtObject.Properties["OtherTypeDescription"].Value;
                    }
                }
            }

            //OtherTypeDescription is only used on Windows Server 2003 R2 to denote "R2"
            name = other != null ? string.Format(CultureInfo.CurrentCulture, "{0} {1}", name, other) : productName;

            return name;
        }

        /// <summary>
        /// Gets the operating system architecture.
        /// </summary>
        /// <returns>The operating system architecture.</returns>
        private static string GetArchitecture()
        {
            string architecture;

            NativeMethods.SYSTEM_INFO info = new NativeMethods.SYSTEM_INFO();

            NativeMethods.GetNativeSystemInfo(ref info);

            switch (info.uProcessorInfo.wProcessorArchitecture)
            {
                case 0:
                    architecture = "x86";
                    break;
                case 1:
                    architecture = "Alpha";
                    break;
                case 2:
                    architecture = "MIPS";
                    break;
                case 3:
                    architecture = "PowerPC";
                    break;
                case 5:
                    architecture = "ARM";
                    break;
                case 6:
                    architecture = "Itanium";
                    break;
                case 9:
                    architecture = "x64";
                    break;
                case 12:
                    architecture = "ARM64";
                    break;
                default:
                    architecture = "unknown";
                    break;
            }

            return architecture;
        }

        /// <summary>
        /// Gets the identifier WELM uses to uniquely identify an operating system.
        /// </summary>
        /// <param name="edition">Edition.</param>
        /// <param name="releaseId">Release identifier.</param>
        /// <param name="version">Version.</param>
        /// <param name="servicePack">Service pack.</param>
        /// <param name="name">Name.</param>
        /// <param name="architecture">Architecture.</param>
        /// <returns>The identifier WELM uses to uniquely identify an operating system.</returns>
        private static string GetWelmId(string edition, UInt32 releaseId, Version version, UInt32 servicePack, string name, string architecture)
        {
            string welmId;

            string osName = name.Replace("Microsoft", "").Replace(edition, "").Trim().Replace(" ", "_");

            if (version.Major >= 10)
            {
                welmId = string.Format(CultureInfo.CurrentCulture, "{0}_{1}_{2}_{3}_{4}", osName, releaseId, edition, architecture, version);
            }
            else
            {
                welmId = servicePack != 0 ? string.Format(CultureInfo.CurrentCulture, "{0}_sp{1}_{2}_{3}_{4}", osName, servicePack, edition, architecture, version) : string.Format(CultureInfo.CurrentCulture, "{0}_{1}_{2}_{3}", osName, edition, architecture, version);
            }

            return welmId.ToLower(CultureInfo.CurrentCulture);
        }
    }
}
