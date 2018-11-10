using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Win32;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;

namespace WelmLibrary.Classic
{
    public class EventSourceData : IEquatable<EventSourceData>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The registry path where Windows Event Log data resides for old event log information.
        /// </summary>
        private const string EventLogKey = @"SYSTEM\CurrentControlSet\Services\Eventlog";

        /// <summary>
        /// The name of the source.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The name of the log for this source.
        /// </summary>
        public string LogName { get; }

        /// <summary>
        /// The event message files specified in this source.
        /// </summary>
        public IList<EventMessageFile> EventMessageFiles { get; }

        /// <summary>
        /// The category message file for this source.
        /// </summary>
        public string CategoryMessageFile { get; }

        /// <summary>
        /// The substitution parameter message file for this source.
        /// </summary>
        public string ParameterMessageFile { get; }

        /// <summary>
        /// The provider GUID for this source.
        /// </summary>
        public Guid ProviderGuid { get; }

        /// <summary>
        /// The event levels used by this source.
        /// </summary>
        public EventTypeBitMask EventLevels { get; }

        /// <summary>
        /// The number of categories.
        /// </summary>
        public Int32 CategoryCount { get; }

        /// <summary>
        /// Creates a new event source by parsing certain registry value names for the source.
        /// </summary>
        /// <param name="logName">The log name for this source.</param>
        /// <param name="sourceName">The source name.</param>
        public EventSourceData(string logName, string sourceName)
        {
            LogName = logName ?? string.Empty;
            Name = sourceName ?? string.Empty;
            EventLevels = new EventTypeBitMask(long.MaxValue);
            CategoryCount = Int32.MaxValue;
            CategoryMessageFile = string.Empty;
            ParameterMessageFile = string.Empty;
            ProviderGuid = Guid.Empty;
            EventMessageFiles = new List<EventMessageFile>();

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(EventLogKey + @"\" + logName + @"\" + sourceName))
            {
                if (key != null)
                {
                    foreach (string valueName in key.GetValueNames())
                    {
                        // filters out any (Default) registry values that actually have a value other than (value not set)
                        if (!string.IsNullOrEmpty(valueName))
                        {
                            switch (valueName.ToLower(CultureInfo.CurrentCulture))
                            {
                                case "providerguid":
                                    ProviderGuid = new Guid((string)key.GetValue(valueName));
                                    break;
                                case "publisherguid":
                                    Logger.Info(CultureInfo.CurrentCulture, "Unsupported registry value name PublisherGuid was converted to ProviderGuid for source {0}", sourceName);
                                    ProviderGuid = new Guid((string)key.GetValue(valueName));
                                    break;
                                case "eventmessagefile":
                                    string eventMessageFiles = (string)key.GetValue(valueName);
                                    ProcessEventMessageFiles(logName, sourceName, eventMessageFiles);
                                    break;
                                case "typessupported":
                                    if (key.GetValueKind(valueName) == RegistryValueKind.DWord)
                                    {
                                        EventLevels = new EventTypeBitMask((Int32)key.GetValue(valueName));
                                    }
                                    break;
                                case "categorycount":
                                    CategoryCount = (Int32)key.GetValue(valueName);
                                    break;
                                case "categorymessagefile":
                                    CategoryMessageFile = (string)key.GetValue(valueName);
                                    break;
                                case "parametermessagefile":
                                    ParameterMessageFile = (string)key.GetValue(valueName);
                                    break;
                                case "eventsourceflags":
                                    // might be dwFlags in  AUTHZ_SOURCE_SCHEMA_REGISTRATION: https://msdn.microsoft.com/en-us/library/windows/desktop/aa376325(v=vs.85).aspx
                                    Int32 flags = (Int32)key.GetValue(valueName);
                                    Logger.Info(CultureInfo.CurrentCulture, "Unsupported registry value name EventSourceFlags has a value of {0} (0x{1:X}) for source {2}", flags, flags, sourceName);
                                    break;
                                default:
                                    Logger.Warn(CultureInfo.CurrentCulture, "Unsupported registry value name of {0} for source {1}", valueName, sourceName);
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    Logger.Error(CultureInfo.CurrentCulture, @"Failed to open registry key {0}\{1}\{2}", EventLogKey, logName, sourceName);
                }
            }
        }

        /// <summary>
        /// Process all the entries in the EventMessageFile registry value.
        /// </summary>
        /// <param name="logName"></param>
        /// <param name="sourceName"></param>
        /// <param name="eventMessageFiles"></param>
        private void ProcessEventMessageFiles(string logName, string sourceName, string eventMessageFiles)
        {
            if (!string.IsNullOrEmpty(eventMessageFiles))
            {
                string systemRootPath = Environment.GetEnvironmentVariable("systemroot").ToLower(CultureInfo.CurrentCulture);
                string winDirPath = Environment.GetEnvironmentVariable("windir").ToLower(CultureInfo.CurrentCulture);

                // fix up paths to be literal paths
                eventMessageFiles = eventMessageFiles.ToLower(CultureInfo.CurrentCulture);
                eventMessageFiles = eventMessageFiles.Replace("%systemroot%", systemRootPath);
                eventMessageFiles = eventMessageFiles.Replace(@"\systemroot", systemRootPath);
                eventMessageFiles = eventMessageFiles.Replace("%windir%", winDirPath);
                eventMessageFiles = eventMessageFiles.Replace("$(runtime.system32)", Environment.GetFolderPath(Environment.SpecialFolder.System)); //seen on Windows 8+ for WinHttpAutoProxySvc

                foreach (string messageFilePath in eventMessageFiles.Split(new [] {";"}, StringSplitOptions.RemoveEmptyEntries))
                {
                    // can't directly modify messageFilePath since it is part of the foreach
                    string modifiedMessageFilePath = messageFilePath.Trim();

                    // some paths are missing a slash between %systemroot% and the rest of the path
                    // one example is the EventMessageFile registry value for the Eventlog\System\vsmraid\ on Windows Vista
                    // we do the same check for %windir% just to be safe

                    if (modifiedMessageFilePath.StartsWith(systemRootPath, StringComparison.CurrentCultureIgnoreCase) &&
                        !modifiedMessageFilePath.StartsWith(systemRootPath + @"\", StringComparison.CurrentCultureIgnoreCase))
                    {
                        modifiedMessageFilePath = modifiedMessageFilePath.Replace(systemRootPath, systemRootPath + @"\");
                    }

                    if (modifiedMessageFilePath.StartsWith(winDirPath, StringComparison.CurrentCultureIgnoreCase) &&
                        !modifiedMessageFilePath.StartsWith(winDirPath + @"\", StringComparison.CurrentCultureIgnoreCase))
                    {
                        modifiedMessageFilePath = modifiedMessageFilePath.Replace(winDirPath, winDirPath + @"\");
                    }

                    // check to see if the messagefile has already been parsed
                    // otherwise parse the messagefile and add it to the cache
                    if (EventMessageFileCache.Instance.Contains(modifiedMessageFilePath))
                    {
                        EventMessageFiles.Add(EventMessageFileCache.Instance.Get(modifiedMessageFilePath));
                    }
                    else
                    {
                        string[] messageFilePaths = modifiedMessageFilePath.Split(new [] {@"\"}, StringSplitOptions.RemoveEmptyEntries);

                        if (messageFilePaths.Length > 0)
                        {
                            string file = messageFilePaths.Last();

                            if (!string.IsNullOrEmpty(file))
                            {
                                EventMessageFile messageFile = new EventMessageFile(logName, sourceName, file, modifiedMessageFilePath);
                                EventMessageFileCache.Instance.Add(messageFile);
                                EventMessageFiles.Add(messageFile);
                            }
                            else
                            {
                                Logger.Debug(CultureInfo.CurrentCulture, "Message file name is empty '{0}'='{1}' for log '{2}' and source '{3}'", modifiedMessageFilePath, eventMessageFiles, logName, sourceName);
                            }
                        }
                        else
                        {
                            Logger.Debug(CultureInfo.CurrentUICulture, "Message file path has no elements '{0}'='{1}' for log '{2}' and source '{3}'", modifiedMessageFilePath, eventMessageFiles, logName, sourceName);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the event sources from the system based on the specified log name.
        /// </summary>
        /// <param name="logName">The log name.</param>
        /// <returns>A list of event sources for the specified log.</returns>
        public static IList<EventSourceData> GetEventSources(string logName)
        {
            var sources = new List<EventSourceData>();
            using (var eventLogKey = Registry.LocalMachine.OpenSubKey(EventLogKey))
            {
                if (eventLogKey != null)
                {
                    try
                    {
                        using (var eventSourceKey = eventLogKey.OpenSubKey(logName))
                        {
                            if (eventSourceKey != null)
                            {
                                //sources.AddRange(eventSourceKey.GetSubKeyNames().Select(sourceName => new EventSource(logName, sourceName)));

                                foreach (string sourceName in eventSourceKey.GetSubKeyNames())
                                {
                                    EventSourceData source = new EventSourceData(logName, sourceName);

                                    if (!sources.Contains(source))
                                    {
                                        sources.Add(source);
                                    }
                                    else
                                    {
                                        Logger.Debug(CultureInfo.CurrentCulture, @"Did not add '{0}\{1}' for {2} due to being a duplicate", EventLogKey, logName, sourceName);
                                    }
                                }
                            }
                            else
                            {
                                Logger.Error(CultureInfo.CurrentCulture, @"Failed to open registry key '{0}\{1}' while enumerating event sources", EventLogKey, logName);
                            }
                        }
                    }
                    catch (SecurityException)
                    {
                        Logger.Error(CultureInfo.CurrentCulture, @"Access denied to registry key '{0}\{1}' while enumerating event sources", EventLogKey, logName);
                    }
                }
                else
                {
                    Logger.Error(CultureInfo.CurrentCulture, "Unable to open registry key '{0}'", EventLogKey);
                }
            }

            return sources;
        }

        public static string ToFormat(IList<EventSourceData> sources, string format)
        {
            string data = string.Empty;

            if (string.IsNullOrEmpty(format))
            {
                format = "json";
            }

            if (sources != null && sources.Count > 0)
            {
                switch (format.ToLower(CultureInfo.CurrentCulture))
                {
                    case "txt":
                        StringBuilder txtBuilder = new StringBuilder(string.Empty);

                        foreach (EventSourceData source in sources)
                        {
                            txtBuilder.AppendFormat("{0}{1}", source, Environment.NewLine);
                        }

                        data = txtBuilder.ToString();
                        break;
                    case "json":
                        JsonSerializerSettings settings = new JsonSerializerSettings
                        {
                            MaxDepth = int.MaxValue,
                            Formatting = Formatting.Indented
                        };

                        data = JsonConvert.SerializeObject(sources, settings);
                        break;
                    case "csv":
                        CsvConfiguration config = new CsvConfiguration
                        {
                            AllowComments = false,
                            DetectColumnCountChanges = true,
                            IgnoreQuotes = true,
                            QuoteAllFields = true,
                            TrimFields = true
                        };

                        StringBuilder csvBuilder = new StringBuilder(sources.Count);

                        using (StringWriter sw = new StringWriter(csvBuilder, CultureInfo.InvariantCulture))
                        {
                            CsvWriter csvWriter = new CsvWriter(sw, config);

                            foreach (EventSourceData source in sources)
                            {
                                csvWriter.WriteField<string>(source.Name);
                                csvWriter.WriteField<string>(source.LogName);
                                csvWriter.WriteField<string>(string.Join("; ", source.EventMessageFiles.ToArray().Select(f => f.FileName).ToArray()).Trim().TrimEnd(';'));
                                csvWriter.WriteField<string>(source.CategoryMessageFile);
                                csvWriter.WriteField<string>(source.ParameterMessageFile);
                                csvWriter.WriteField<Guid>(source.ProviderGuid);
                                csvWriter.WriteField<string>((source.EventLevels.Bitmask != long.MaxValue ? source.EventLevels.ToString() : ""));
                                csvWriter.WriteField<int>((source.CategoryCount != int.MaxValue ? source.CategoryCount : 0));
                                csvWriter.NextRecord();
                            }

                            sw.Flush();
                        }

                        csvBuilder.Insert(0, "\"Name\",\"EventLog\",\"EventMessageFiles\",\"CategoryMessageFile\",\"ParameterMessageFile\",\"ProviderGuid\",\"EventLevels\",\"CategoryCount\"" + Environment.NewLine);
                        data = csvBuilder.ToString();
                        break;
                    default:
                        break;
                }
            }

            return data;
        }

        public override string ToString()
        {
            StringBuilder output = new StringBuilder(string.Empty);
            output.AppendFormat("Name: {0}|", Name);
            output.AppendFormat("EventLog: {0}|", LogName);

            if (EventMessageFiles.Any())
            {
                output.AppendFormat("EventMessageFiles: {0}|", string.Join("; ", EventMessageFiles.ToArray().Select(f => f.FileName).ToArray()).Trim().TrimEnd(';'));
            }

            if (!string.IsNullOrEmpty(CategoryMessageFile))
            {
                output.AppendFormat("CategoryMessageFile: {0}|", CategoryMessageFile);
            }

            if (!string.IsNullOrEmpty(ParameterMessageFile))
            {
                output.AppendFormat("ParameterMessageFile: {0}|", ParameterMessageFile);
            }

            if (ProviderGuid != Guid.Empty)
            {
                output.AppendFormat("ProviderGuid: {0}|", ProviderGuid);
            }

            if (EventLevels.Bitmask != long.MaxValue)
            {
                output.AppendFormat("EventLevels: {0}|", EventLevels);
            }

            if (CategoryCount != Int32.MaxValue)
            {
                output.AppendFormat("CategoryCount: {0}|", CategoryCount);
            }

            string s = output.ToString().Trim();

            if (s.EndsWith("|", true, CultureInfo.CurrentCulture))
            {
                s = s.TrimEnd('|');
            }

            return s;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as EventSourceData);
        }

        public bool Equals(EventSourceData other)
        {
            bool equal = false;

            if (other != null)
            {
                if (Name != null && other.Name != null && Name.Equals(other.Name))
                {
                    if (LogName != null && other.LogName != null && LogName.Equals(other.LogName))
                    {
                        equal = true;
                    }
                }
            }

            return equal;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() + LogName.GetHashCode();
        }
    }
}
