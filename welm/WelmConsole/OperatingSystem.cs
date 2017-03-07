using Microsoft.Win32;
using System;
using System.Globalization;
using System.Linq;
using System.Management;

namespace WelmConsole
{
    public class OperatingSystem
    {
        public string Edition { get; }

        public UInt32 ReleaseId { get; }

        public Version Version { get; }

        public UInt32 ServicePack { get; }

        public string Name { get; }

        public string Architecture { get; }

        public string WelmId { get; }

        public OperatingSystem()
        {
            Edition = GetEdition();
            ReleaseId = GetReleaseId();
            Version = GetVersion();
            ServicePack = GetServicePack();
            Name = GetName();
            Architecture = GetArchitecture();
            WelmId = GetWelmId(Edition, ReleaseId, Version, ServicePack, Name, Architecture);
        }

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
                            UInt32 version = (UInt32) currentKey.GetValue("CurrentMajorVersionNumber", 0);
                            isWindows10OrLater = version >= 10;
                        }
                    }
                }
            }

            return isWindows10OrLater;
        }

        private static string GetEdition()
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
                            string product = (string) currentKey.GetValue("ProductName", "");

                            product =
                                product.Replace("Microsoft", "")
                                    .Replace("Windows", "")
                                    .Replace("Vista", "")
                                    .Replace("XP", "")
                                    .Replace("Service Pack", "")
                                    .Replace("Server", "")
                                    .Replace("\u00A9", "")
                                    .Replace("\u00AE", "")
                                    .Replace("\u2122", "")
                                    .Replace("(R)", "")
                                    .Replace("(TM)", "")
                                    .Trim();

                            string[] productParts = product.Split(new []{' '},
                                StringSplitOptions.RemoveEmptyEntries);

                            foreach (string part in productParts)
                            {
                                UInt32 garbage = 0;

                                if (!UInt32.TryParse(part, out garbage))
                                {
                                    edition = part;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return edition;
        }

        private static UInt32 GetReleaseId()
        {
            UInt32 release = 0;

            if (IsWindows10OrLater())
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

                            Int32 build = 0;

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

                            Int32 major = 0;
                            Int32.TryParse(currentVersion.Split('.')[0], out major);

                            Int32 minor = 0;
                            Int32.TryParse(currentVersion.Split('.')[1], out minor);

                            Int32 build = 0;

                            Int32.TryParse((string) currentKey.GetValue("CurrentBuild", ""), out build);

                            osVersion = new Version(major, minor, build, 0);
                        }
                    }
                }
            }

            return osVersion;
        }

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

        private static string GetArchitecture()
        {
            string architecture = "";

            string query = "SELECT * FROM Win32_OperatingSystem WHERE Primary='true'";

            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
            {
                using (ManagementObject mgmtObject = searcher.Get().Cast<ManagementObject>().FirstOrDefault())
                {
                    if (mgmtObject != null)
                    {
                        string arch = mgmtObject.Properties["OSArchitecture"].Value.ToString();
                        arch = arch.Replace("-bit", "");
                        architecture = string.Format(CultureInfo.CurrentCulture, "x{0}", arch);
                    }
                }
            }

            return architecture;
        }

        private static string GetWelmId(string edition, UInt32 releaseId, Version version, UInt32 servicePack, string name, string architecture)
        {
            string welmId = "";

            string osName = name.Replace("Microsoft", "").Replace(edition, "").Trim().Replace(" ", "_");

            if (IsWindows10OrLater())
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
