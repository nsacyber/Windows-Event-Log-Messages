using NLog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

namespace WelmLibrary.Classic
{
    public class EventMessageFile
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// A pre-defined resource type of message table entry, RT_MESSAGETABLE.
        /// </summary>
        private const int MessageTableResource = 11;

        /// <summary>
        /// The minimum structure length for the MESSAGE_RESOURCE_ENTRY structure when it contains an ANSI message string.
        /// </summary>
        private const int MinLenAnsi = 0xC;

        /// <summary>
        /// The minimum structure length for the MESSAGE_RESOURCE_ENTRY structure when it contains a Unicode message string.
        /// </summary>
        private const int MinLenUnicode = 0x10;

        /// <summary>
        /// The value of the Flags field in the MESSAGE_RESOURCE_ENTRY structure when the structure contains an ANSI message string.
        /// </summary>
        private const int AnsiFlag = 0;

        /// <summary>
        /// The value of the Flags field in the MESSAGE_RESOURCE_ENTRY structure when the structure contains a Unicode message string.
        /// </summary>
        private const int UnicodeFlag = 1;

        /// <summary>
        /// The events specified in the file.
        /// </summary>
        private IList<ClassicEventData> Events { get; set; }

        /// <summary>
        /// Whether or not events were retrieved from the file.
        /// </summary>
        private bool EventsEnumerated { get; set; }

        /// <summary>
        /// The file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The name of the log the event message file is for.
        /// </summary>
        public string LogName { get; set; }

        /// <summary>
        /// The name of the source the event message file is for.
        /// </summary>
        public string SourceName { get; set; }

        /// <summary>
        /// The file path.
        /// </summary>
        public string Path { get; set; }

        public EventMessageFile()
        {
            LogName = string.Empty;
            SourceName = string.Empty;
            FileName = string.Empty;
            Path = string.Empty;
            Events = new List<ClassicEventData>();
            EventsEnumerated = false;
        }

        /// <summary>
        /// Creates an event message file data based on file name and file path.
        /// </summary>
        /// <param name="logName">The log name.</param>
        /// <param name="sourceName">The source name.</param>
        /// <param name="fileName">The file name.</param>
        /// <param name="path">The file path.</param>
        public EventMessageFile(string logName, string sourceName, string fileName, string path)
        {
            LogName = logName ?? string.Empty;
            SourceName = sourceName ?? string.Empty;
            FileName = fileName ?? string.Empty;
            Path = path == null ? string.Empty : path.ToLower(CultureInfo.CurrentCulture);
            Events = new List<ClassicEventData>();
            EventsEnumerated = false;
        }

        /// <summary>
        /// Gets the event data from the event message file.
        /// </summary>
        /// <returns>A list of events.</returns>
        public IList<ClassicEventData> GetEvents()
        {
            if (EventsEnumerated)
            {
                return Events;
            }

            string originalPath = Path;

            IntPtr hModule = NativeMethods.LoadLibraryEx(Path, IntPtr.Zero, (uint)NativeMethods.LoadLibraryFlags.LoadLibraryAsDataFile);

            int errorCode = Marshal.GetLastWin32Error();
            string errorMessage = new Win32Exception(errorCode).Message;

            /** 
            * there are a bunch of strategies below to fix "errors" in the collected path data
            * the paths come from the registry and sometimes have odd data that the system loader must know how to deal with
            * these could be moved into EventSource's constructor in the case where it processes the EventMessageFile registry value name
            * the advantage of doing it here is that it only ends up changing the paths absolutely needed to because it only modifies paths 
            * that have failed loading already. Moving these checks to EventSource's constructor could result in making incorrect path changes
            **/

            if (hModule == IntPtr.Zero)
            {
                // rare case where a path was missing everything
                // aaedge.dll on Server 2008 R2 SP1 and later, Server 2012 is in C:\Windows\System32
                // replprov.dll on Server 2008 SP1 and later is in C:\Windows\System32
                // assume that other unrooted files paths may have the same issue
                if (!System.IO.Path.IsPathRooted(Path))
                {
                    Path = System.IO.Path.Combine(Environment.GetEnvironmentVariable("windir"), System.IO.Path.Combine("system32", Path)).ToLower(CultureInfo.CurrentCulture);

                    hModule = NativeMethods.LoadLibraryEx(Path, IntPtr.Zero, (uint)NativeMethods.LoadLibraryFlags.LoadLibraryAsDataFile);
                    errorCode = Marshal.GetLastWin32Error();

                    if (hModule != IntPtr.Zero)
                    {
                        Logger.Info(CultureInfo.CurrentCulture, "Unrooted path fix worked. Before: '{0}' After: '{1}'", originalPath, Path);
                    }
                }
            }

            string programFilesx86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");

            if (hModule == IntPtr.Zero)
            {
                // if Path is Program Files (x86) and the load fails, then try again with the Path as Program Files. this fixes 2-4 file load failures on x64         
                // might able to load the file on 64-bit systems if the Path was set to the "wrong" Program Files path so attempt to "fix" it
                if (!string.IsNullOrEmpty(programFilesx86) && Path.StartsWith(programFilesx86, StringComparison.CurrentCultureIgnoreCase))
                {
                    // can't use Environment.GetEnvironmentVariable("ProgramFiles") because it returns the same values as ProgramFiles(x86) on 64-bit Windows processes
                    Path = Path.Replace(programFilesx86.ToLower(CultureInfo.CurrentCulture), Environment.GetEnvironmentVariable("ProgramW6432").ToLower(CultureInfo.CurrentCulture));

                    hModule = NativeMethods.LoadLibraryEx(Path, IntPtr.Zero, (uint)NativeMethods.LoadLibraryFlags.LoadLibraryAsDataFile);
                    errorCode = Marshal.GetLastWin32Error();

                    if (hModule != IntPtr.Zero)
                    {
                        Logger.Info(CultureInfo.CurrentCulture, "Modified program files fix worked. Before: '{0}' After: '{1}'", originalPath, Path);
                    }
                }
            }

            if (hModule == IntPtr.Zero)
            {
                // XP x64 and Server 2003 R2 x64 needs to have path redirection temporarily disabled to find many of the files in system32 and system32\drivers folders
                if (!string.IsNullOrEmpty(programFilesx86) && Path.StartsWith(System.IO.Path.Combine(Environment.GetEnvironmentVariable("windir"), "system32"), StringComparison.CurrentCultureIgnoreCase))
                {
                    if (Path.StartsWith(System.IO.Path.Combine(Environment.GetEnvironmentVariable("windir"), "system32"), StringComparison.CurrentCultureIgnoreCase))
                    {
                        IntPtr ptr = new IntPtr();
                        bool disableResult = NativeMethods.Wow64DisableWow64FsRedirection(ref ptr);

                        hModule = NativeMethods.LoadLibraryEx(Path, IntPtr.Zero, (uint)NativeMethods.LoadLibraryFlags.LoadLibraryAsDataFile);
                        errorCode = Marshal.GetLastWin32Error();

                        bool revertResult = NativeMethods.Wow64RevertWow64FsRedirection(ref ptr);

                        if (!disableResult || !revertResult)
                        {
                            Logger.Warn(CultureInfo.CurrentCulture, "File system redirection suppression operation failed. Wow64DisableWow64FsRedirection returned {0}. Wow64RevertWow64FsRedirection returned {1}", disableResult, revertResult);
                        }

                        if (hModule != IntPtr.Zero)
                        {
                            Logger.Info(CultureInfo.CurrentCulture, "Wow64 file system redirection disabling fix worked. Before: '{0}' After: '{1}'", originalPath, Path);
                        }
                    }
                }

            }

            // if couldn't load the library, then just return without enumerating
            if (hModule == IntPtr.Zero)
            {
                Logger.Error(CultureInfo.CurrentCulture, "Unable to load {0} due to error code {1}: {2}. Log: {3} Source: {4}", originalPath, errorCode, errorMessage, LogName, SourceName);
                return Events;
            }

            try
            {
                NativeMethods.EnumResourceTypes(hModule, EnumResTypes, (IntPtr)(-1));
            }
            finally
            {
                EventsEnumerated = true;
                NativeMethods.FreeLibrary(hModule);
            }
            return Events;
        }

        /// <summary>
        /// Gets the number of events found in the event message file.
        /// </summary>
        /// <returns>The number of events found in the event message file.</returns>
        public int NumberOfEvents()
        {
            return EventsEnumerated ? Events.Count : GetEvents().Count();
        }

        /// <summary>
        /// An application-defined callback function used with the EnumResourceTypes and EnumResourceTypesEx functions. It receives resource types. The ENUMRESTYPEPROC type defines a pointer to this callback function. EnumResTypeProc is a placeholder for the application-defined function name. Equivalent to EnumResTypeProc
        /// </summary>
        /// <param name="hModule">A handle to the module whose executable file contains the resources for which the types are to be enumerated. If this parameter is NULL, the function enumerates the resource types in the module used to create the current process.</param>
        /// <param name="lpType">The type of resource for which the type is being enumerated. Alternately, rather than a pointer, this parameter can be MAKEINTRESOURCE (ID), where ID is the integer identifier of the given resource type. For standard resource types, see Resource Types.</param>
        /// <param name="lParam">An application-defined parameter passed to the EnumResourceTypes or EnumResourceTypesEx function. This parameter can be used in error checking.</param>
        /// <returns>Returns TRUE to continue enumeration or FALSE to stop enumeration.</returns>
        private bool EnumResTypes(IntPtr hModule, IntPtr lpType, IntPtr lParam)
        {
            if (lParam != new IntPtr(-1))
            {
                Logger.Error(CultureInfo.CurrentCulture, "EnumResTypes called with incorrect lParam value of {0}", lParam);
                return false;
            }

            if (NativeMethods.IsIntresource(lpType))
            {
                if (lpType.ToInt32() == MessageTableResource)
                {
                    NativeMethods.EnumResourceNames(hModule, lpType, EnumResNames, (IntPtr)(-2));
                    return false;
                }
            }
            return true;
        }


        /// <summary>
        /// An application-defined callback function used with the EnumResourceLanguages and EnumResourceLanguagesEx functions. It receives the type, name, and language of a resource item. The ENUMRESLANGPROC type defines a pointer to this callback function. EnumResLangProc is a placeholder for the application-defined function name. Equivalent to EnumResLangsProc.
        /// </summary>
        /// <param name="hModule">A handle to the module whose executable file contains the resources for which the languages are being enumerated. If this parameter is NULL, the function enumerates the resource languages in the module used to create the current process.</param>
        /// <param name="lpType">he type of resource for which the language is being enumerated. Alternately, rather than a pointer, this parameter can be MAKEINTRESOURCE(ID), where ID is an integer value representing a predefined resource type. For standard resource types, see Resource Types.</param>
        /// <param name="lpName">The name of the resource for which the language is being enumerated. Alternately, rather than a pointer, this parameter can be MAKEINTRESOURCE (ID), where ID is the integer identifier of the resource.</param>
        /// <param name="wIDLanguage">The language identifier for the resource for which the language is being enumerated. The EnumResourceLanguages or EnumResourceLanguagesEx function provides this value. For a list of the primary language identifiers and sublanguage identifiers that constitute a language identifier, see MAKELANGID.</param>
        /// <param name="lParam">The application-defined parameter passed to the EnumResourceLanguages or EnumResourceLanguagesEx function. This parameter can be used in error checking.</param>
        /// <returns>Returns TRUE to continue enumeration or FALSE to stop enumeration.</returns>
        private bool EnumResLangs(IntPtr hModule, IntPtr lpType, IntPtr lpName, ushort wIDLanguage, IntPtr lParam)
        {
            if (wIDLanguage != CultureInfo.CurrentUICulture.LCID)
            {
                return true;
            }

            if (lParam != (IntPtr) (-3))
            {
                Logger.Error(CultureInfo.CurrentCulture, "EnumResLangs called with incorrect lParam value of {0} for {1}", lParam, Path);
                return false;
            }

            IntPtr hResInfo = NativeMethods.FindResourceEx(hModule, lpType, lpName, wIDLanguage);

            if (hResInfo == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                Logger.Error(CultureInfo.CurrentCulture, "FindResourcesEx failed for {0} with error {1}: {2}", Path, errorCode, (new Win32Exception(errorCode)).Message);
                return false;
            }

            IntPtr hResource = NativeMethods.LoadResource(hModule, hResInfo);

            if (hResource == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                Logger.Error(CultureInfo.CurrentCulture, "LoadResource failed for {0} with error {1}: {2}", Path, errorCode, (new Win32Exception(errorCode)).Message);
                return false;
            }

            IntPtr pResource = NativeMethods.LockResource(hResource);

            if (pResource == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                Logger.Error(CultureInfo.CurrentCulture, "LockResource failed for {0} with error {1}: {2}", Path, errorCode, (new Win32Exception(errorCode)).Message);
                return false;
            }

            /**
             typedef struct {
               DWORD                  NumberOfBlocks;
               MESSAGE_RESOURCE_BLOCK Blocks[1];
             } MESSAGE_RESOURCE_DATA, *PMESSAGE_RESOURCE_DATA; 
             **/

            IntPtr messageResourceData = pResource;
            Int32 numberOfBlocks = Marshal.ReadInt32(messageResourceData);

            // Get MESSAGE_RESOURCE_DATA.Blocks so we can iterate over the MESSAGE_RESOURCE_BLOCK array
            IntPtr blocks = new IntPtr(messageResourceData.ToInt32() + 4);

            // iterate over the MESSAGE_RESOURCE_BLOCK array
            /**
              typedef struct {
                DWORD LowId;
                DWORD HighId;
                DWORD OffsetToEntries;
              } MESSAGE_RESOURCE_BLOCK;
             **/
            for (int blockNum = 0; blockNum < numberOfBlocks; blockNum++)
            {
                NativeMethods.MESSAGE_RESOURCE_BLOCK block = (NativeMethods.MESSAGE_RESOURCE_BLOCK)Marshal.PtrToStructure(blocks,
                    typeof(NativeMethods.MESSAGE_RESOURCE_BLOCK));

                // MESSAGE_RESOURCE_BLOCK.OffsetToEntries is the offset from the MESSAGE_RESOURCE_DATA to the MESSAGE_RESROURCE_ENTRY array
                IntPtr entries = new IntPtr(messageResourceData.ToInt32() + block.OffsetToEntries);

                // can probably skip this block since there are no message IDs or is there only 1?
                if (block.LowId == 0 && block.HighId == 0)
                {
                    Logger.Info(CultureInfo.CurrentCulture, "MESSAGE_RESOURCE_BLOCK LowId and HighId were both 0 for {0}", Path);
                    //continue;
                }

                // can probably skip this block, smallest observed legit value has been 16 so far
                if (block.OffsetToEntries == 0)
                {
                    Logger.Info(CultureInfo.CurrentCulture, "MESSAGE_RESOURCE_BLOCK OffsetToEntries was 0 for {0}", Path);
                    //continue;
                }

                // Iterate over the MESSAGE_RESOURCE_ENTRY array
                /**
                  typedef struct {
                    WORD Length;
                    WORD Flags;
                    BYTE Text[1];
                  } MESSAGE_RESOURCE_ENTRY, *PMESSAGE_RESOURCE_ENTRY; 
                 **/
                for (uint id = block.LowId; id <= block.HighId; id++)
                {
                    short length = Marshal.ReadInt16(entries);
                    short flags = Marshal.ReadInt16(entries, sizeof(Int16));
                    string message = string.Empty;

                    // check if the message was stored as UNICODE or ANSI
                    // older versions of mc.exe stored strings as ANSI
                    if (flags == AnsiFlag)
                    {
                        message = Marshal.PtrToStringAnsi(new IntPtr(entries.ToInt32() + 4));
                    }
                    else if (flags == UnicodeFlag)
                    {
                        message = Marshal.PtrToStringUni(new IntPtr(entries.ToInt32() + 4));
                    }
                    else
                    {
                        Logger.Warn(CultureInfo.CurrentCulture, "MESSAGE_RESOURCE_ENTRY.Flags field should be 0 or 1 but was {0} in {1} so the event message will be empty", flags, Path);
                    }

                    EventId eventId = new EventId(id);

                    ClassicEventData eventData = new ClassicEventData(LogName, SourceName, eventId, message);

                    // TODO: check min lengths to see if they are correct. MinLenAnsi = 12, MinLenUnicode = 16? shouldn't it be 5 bytes? what's coded right now is probably incorrect
                    // TODO: < instead of <= ?

                    // advance to the next RESOURCE_ENTRY in the array
                    // ignore anything that doesn't meet the minimum structure length for a message resource
                    if ((length <= MinLenAnsi && flags == AnsiFlag) || (length <= MinLenUnicode && flags == UnicodeFlag))
                    {
                        // log this?
                        continue;
                    }

                    // TODO: double check the assumption below

                    // not a Microsoft event from observation, probably just some random string resource
                    if (!eventId.IsCustomer && eventId.IsReserved)
                    {
                        // log this?
                        continue;
                    }

                    //Customer. This specifies if the value is customer or Microsoft defined. This bit is set for customer values and clear for Microsoft values.
                    //Reserved. Must be 0 so that it is possible to map an NTSTATUS value to an equivalent HRESULT value.

                    //per MS-ERREF, ok
                    //if(!eventId.IsCustomer && !eventId.IsReserved)
                        //ok and is valid

                    //per MS-ERREF, not ok, skip
                    //if (eventId.IsCustomer || eventId.IsReserved)
                    //    continue;

                    Events.Add(eventData);

                    entries = new IntPtr(entries.ToInt32() + length);
                }

                blocks = new IntPtr(blocks.ToInt32() + Marshal.SizeOf(typeof (NativeMethods.MESSAGE_RESOURCE_BLOCK)));
            }

            // all done so return false to stop enumerating
            return false;
        }

        /// <summary>
        /// An application-defined callback function used with the EnumResourceNames and EnumResourceNamesEx functions. It receives the type and name of a resource. The ENUMRESNAMEPROC type defines a pointer to this callback function. EnumResNameProc is a placeholder for the application-defined function name. Equivalent to EnunResNamesProc.
        /// </summary>
        /// <param name="hModule">A handle to the module whose executable file contains the resources that are being enumerated. If this parameter is NULL, the function enumerates the resource names in the module used to create the current process.</param>
        /// <param name="lpType">The type of resource for which the name is being enumerated. Alternately, rather than a pointer, this parameter can be MAKEINTRESOURCE(ID), where ID is an integer value representing a predefined resource type. For standard resource types, see Resource Types.</param>
        /// <param name="lpName">The name of a resource of the type being enumerated. Alternately, rather than a pointer, this parameter can be MAKEINTRESOURCE(ID), where ID is the integer identifier of the resource.</param>
        /// <param name="lParam">An application-defined parameter passed to the EnumResourceNames or EnumResourceNamesEx function. This parameter can be used in error checking.</param>
        /// <returns>Returns TRUE to continue enumeration or FALSE to stop enumeration.</returns>
        private bool EnumResNames(IntPtr hModule, IntPtr lpType, IntPtr lpName, IntPtr lParam)
        {
            if (lParam != (IntPtr) (-2))
            {
                Logger.Error(CultureInfo.CurrentCulture, "EnumResNames called with incorrect lParam value of {0} for {1}", lParam, Path);
                return false;
            }

            NativeMethods.EnumResourceLanguages(hModule, lpType, lpName, EnumResLangs, (IntPtr)(-3));

            return true;
        }
    }
}
