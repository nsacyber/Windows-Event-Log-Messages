using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace WelmLibrary
{
    public class EventProviderData : IEquatable<EventProviderData>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The provider name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The provider display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// The events associated with this provider.
        /// </summary>
        public IList<EventData> Events { get; private set; }

        /// <summary>
        /// The GUID for the provider.
        /// </summary>
        public Guid Guid { get; set; }

        /// <summary>
        /// The file used for help information.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// The file that contains the string metadata used by this provider.
        /// </summary>
        public string MessageFile { get; set; }

        /// <summary>
        /// The file that contains the metadata used for parameter substitution in the event log messages logged by this provider.
        /// </summary>
        public string SubstitutionFile { get; set; }

        /// <summary>
        /// The file that contains the metadata for this provider.
        /// </summary>
        public string ResourceFile { get; set; }

        /// <summary>
        /// The event levels defined for this provider.
        /// </summary>
        public IList<EventLevelData> Levels { get; private set; }

        /// <summary>
        /// The event keywords defined for this provider.
        /// </summary>
        public IList<EventKeywordData> Keywords { get; private set; }

        /// <summary>
        /// The logs that this provider sends events to.
        /// </summary>
        public IList<EventLogData> SendsEventsTo { get; private set; }

        public EventProviderData()
        {
            Name = string.Empty;
            DisplayName = string.Empty;
            Events = new List<EventData>();
            Guid = Guid.Empty;
            FileName = string.Empty;
            MessageFile = string.Empty;
            SubstitutionFile = string.Empty;
            ResourceFile = string.Empty;
            Levels = new List<EventLevelData>();
            Keywords = new List<EventKeywordData>();
            SendsEventsTo = new List<EventLogData>();
        }

        /// <summary>
        /// Retrieves event provider data from the system based on event provider metadata.
        /// </summary>
        /// <returns></returns>
        public static IList<EventProviderData> GetProviders()
        {
            IList<EventProviderData> providers = new List<EventProviderData>();

            using (EventLogSession session = new EventLogSession())
            {
                foreach (string providerName in session.GetProviderNames())
                {
                    ProviderMetadata providerMetadata = null;

                    try
                    {
                        providerMetadata = new ProviderMetadata(providerName);

                        EventProviderData providerData = new EventProviderData();
                        providerData.Name = providerMetadata.Name ?? string.Empty;
                        providerData.DisplayName = GetProviderDisplayName(providerMetadata) ?? string.Empty;
                        providerData.Guid = providerMetadata.Id;
                        providerData.FileName = GetHelpFileNameFromUri(providerMetadata.HelpLink) ?? string.Empty;
                        providerData.MessageFile = providerMetadata.MessageFilePath ?? string.Empty;
                        providerData.SubstitutionFile = providerMetadata.ParameterFilePath ?? string.Empty;
                        providerData.ResourceFile = providerMetadata.ResourceFilePath ?? string.Empty;
                        providerData.Levels = GetProviderEventLevels(providerMetadata);
                        providerData.SendsEventsTo = providerMetadata.LogLinks.Select(link => new EventLogData(link.LogName)).ToList();

                        try
                        {
                            IList<EventData> events = new List<EventData>();

                            string provName = providerName; // prevents "Access to foreach variable in a closure" warning

                            foreach (EventData eventData in providerMetadata.Events.Select(eventMetadata => new EventData(provName, eventMetadata)).Where(eventData => !events.Contains(eventData)))
                            {
                                events.Add(eventData);
                            }

                            providerData.Events = events;
                        }
                        catch (EventLogException ele)
                        {
                            providerData.Events = new List<EventData>();

                            Logger.Error(ele, CultureInfo.CurrentCulture, "Event provider '{0}' threw a generic event log exception while accessing the event provider Events field: {1}{2}{3}", providerName, ele.Message, Environment.NewLine, ele.StackTrace);
                        } //something is weird with Windows-MsiServer 

                        try
                        {
                            providerData.Keywords = EventKeywordData.GetKeywords(providerMetadata.Keywords);
                        }
                        catch (EventLogException ele)
                        {
                            providerData.Keywords = new List<EventKeywordData>();

                            Logger.Error(ele, CultureInfo.CurrentCulture, "Event provider '{0}' threw a generic event log exception while accessing the event provider Keywords field: {1}{2}{3}", providerName, ele.Message, Environment.NewLine, ele.StackTrace);
                        } //something is weird with Windows-MsiServer 

                        // Ntfs has 2 entries instead of 1 so make sure we don't add it twice
                        if (!providers.Contains(providerData))
                        {
                            providers.Add(providerData);
                        }
                    }
                    catch (EventLogNotFoundException elnfe)
                    {
                        // Microsoft-Windows-TerminalServices-ServerUSBDevice = The system cannot find the file specified.
                        // Microsoft-Windows-WPD-MTPClassDriver = The system cannot find the file specified
                        // Microsoft-Windows-Sdbus-SQM = The system cannot find the files specified

                        Logger.Error(elnfe, CultureInfo.CurrentCulture, "Event provider '{0}' not found during initial access of the provider while processing providers: {1}{2}{3}", providerName, elnfe.Message, Environment.NewLine, elnfe.StackTrace);
                    }
                    catch (UnauthorizedAccessException uae)
                    {
                        // thrown when running as a normal user and accessing these:
                        // Microsoft-Windows-Security-Auditing 
                        // Microsoft-Windows-Eventlog

                        Logger.Error(uae, CultureInfo.CurrentCulture, "Access denied to event provider '{0}' during initial access of the provider while processing providers: {1}{2}{3}", providerName, uae.Message, Environment.NewLine, uae.StackTrace);
                    }
                    catch (EventLogException ele)
                    {
                        // unfortunately vista x64 needs this generic catch statement
                        Logger.Error(ele, CultureInfo.CurrentCulture, "Event provider '{0}' threw a generic event log exception during initial access of the provider while processing providers: {1}{2}{3}", providerName, ele.Message, Environment.NewLine, ele.StackTrace);
                    }
                    finally
                    {
                        providerMetadata?.Dispose();
                    }
                }
            }

            return providers;
        }

        public static string ToFormat(IList<EventProviderData> providers, string format)
        {
            string data = string.Empty;

            if (string.IsNullOrEmpty(format))
            {
                format = "json";
            }

            if (providers != null && providers.Any())
            {
                switch (format.ToLower(CultureInfo.CurrentCulture))
                {
                    case "txt":

                        StringBuilder txtBuilder = new StringBuilder(string.Empty);

                        foreach (EventProviderData providerData in providers)
                        {
                            txtBuilder.AppendFormat("{0}{1}", providerData, Environment.NewLine);
                        }

                        data = txtBuilder.ToString();
                        break;
                    case "json":
                        JsonSerializerSettings settings = new JsonSerializerSettings
                        {
                            MaxDepth = int.MaxValue,
                            Formatting = Formatting.None
                        };

                        data = JsonConvert.SerializeObject(providers, settings);
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

                        StringBuilder csvBuilder = new StringBuilder(providers.Count);

                        using (StringWriter sw = new StringWriter(csvBuilder, CultureInfo.InvariantCulture))
                        {
                            CsvWriter csvWriter = new CsvWriter(sw, config);

                            //csvWriter.WriteHeader<EventProviderData>(); // writes incorrect header

                            foreach (EventProviderData provider in providers)
                            {
                                csvWriter.WriteField<string>(provider.Name);
                                csvWriter.WriteField<string>(provider.DisplayName);
                                csvWriter.WriteField<int>(provider.Events.Count); // no header due to type mismatch for property
                                csvWriter.WriteField<Guid>(provider.Guid);
                                csvWriter.WriteField<string>(provider.FileName);
                                csvWriter.WriteField<string>(provider.MessageFile);
                                csvWriter.WriteField<string>(provider.SubstitutionFile);
                                csvWriter.WriteField<string>(provider.ResourceFile);
                                csvWriter.WriteField<string>(string.Join(", ", provider.Levels.ToArray().Select(l => l.Name).ToArray()).Trim().TrimEnd(new [] { ',' })); // no header due to type mismatch for property
                                csvWriter.WriteField<string>(string.Join(", ", provider.Keywords.ToArray().Select(k => k.Name).ToArray()).Trim().TrimEnd(new [] { ',' })); // no header due to type mismatch for property
                                csvWriter.WriteField<string>(string.Join(", ", provider.SendsEventsTo.ToArray().Select(st => st.Name).ToArray()).Trim().TrimEnd(new [] { ',' })); // no header due to type mismatch for property
                                csvWriter.NextRecord();
                            }

                            sw.Flush();
                        }

                        // workaround issue with CsvWrite.WriteHeader by manually writing the header
                        // it only wrote this as a header: "Name","DisplayName","Guid","FileName","MessageFile","SubstitutionFile","ResourceFile"
                        // basically any WriteField that doesn't match the corresponding property's type is not written in the header
                        csvBuilder.Insert(0, "\"Name\",\"DisplayName\",\"Events\",\"Guid\",\"FileName\",\"MessageFile\",\"SubstitutionFile\",\"ResourceFile\",\"Levels\",\"Keywords\",\"SendsTo\"" + Environment.NewLine);
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
            StringBuilder output = new StringBuilder();
            output.AppendFormat("Name: {0}|", Name);

            if (!string.IsNullOrEmpty(DisplayName))
            {
                output.AppendFormat("DisplayName: {0}|", DisplayName);
            }

            if (Events.Count > 0)
            {
                output.AppendFormat("Events: {0}|", Events.Count);
            }

            if (!Guid.Empty.Equals(Guid))
            {
                output.AppendFormat("Guid: {0}|", Guid);
            }

            if (!string.IsNullOrEmpty(FileName))
            {
                output.AppendFormat("FileName: {0}|", FileName);
            }

            if (!string.IsNullOrEmpty(MessageFile))
            {
                output.AppendFormat("MessageFile: {0}|", MessageFile);
            }

            if (!string.IsNullOrEmpty(SubstitutionFile))
            {
                output.AppendFormat("SubtitutionFile: {0}|", SubstitutionFile);
            }

            if (!string.IsNullOrEmpty(ResourceFile))
            {
                output.AppendFormat("ResourceFile: {0}|", ResourceFile);
            }

            if (Levels != null && Levels.Count > 0)
            {
                output.AppendFormat("Levels: {0}|", string.Join(", ", Levels.ToArray().Select(l => l.Name).ToArray()).Trim().TrimEnd(new [] { ',' }));
            }

            if (Keywords != null && Keywords.Count > 0)
            {
                output.AppendFormat("Keywords: {0}|", string.Join(", ", Keywords.ToArray().Select(k => k.Name).ToArray()).Trim().TrimEnd(new [] { ',' }));
            }

            if (SendsEventsTo != null && SendsEventsTo.Count > 0)
            {
                output.AppendFormat("SendsTo: {0}|", string.Join(", ", SendsEventsTo.ToArray().Select(st => st.Name).ToArray()).Trim().TrimEnd(new [] { ',' }));
            }

            string s = output.ToString().Trim();

            if (s.EndsWith("|", true, CultureInfo.CurrentCulture))
            {
                s = s.TrimEnd(new [] { '|' });
            }

            return s;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as EventProviderData);
        }

        public bool Equals(EventProviderData other)
        {
            bool equal = false;

            if (other != null)
            {
                if (Name != null && other.Name != null && Name.Equals(other.Name))
                {
                    if (Guid.Equals(other.Guid))
                    {
                        equal = true;
                    }
                }
            }

            return equal;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() + Guid.GetHashCode();
        }

        /// <summary>
        /// Gets the provider display name if it can be retrieved.
        /// </summary>
        /// <param name="providerMetadata">The provider metadata.</param>
        /// <returns>The provider display name or an empty string if the display name can't be retrieved.</returns>
        private static string GetProviderDisplayName(ProviderMetadata providerMetadata)
        {
            string value = string.Empty;

            try
            {
                value = string.IsNullOrEmpty(providerMetadata.DisplayName) ? string.Empty : providerMetadata.DisplayName;
            }
            catch (EventLogException ele)
            {
                Logger.Warn(ele, CultureInfo.CurrentCulture, "Event provider '{0}' threw a generic event log exception while accessing the event provider DisplayName field: {1}{2}{3}", providerMetadata.Name, ele.Message, Environment.NewLine, ele.StackTrace);
            } //something is weird with Windows-MsiServer 

            return value;
        }

        /// <summary>
        /// Gets the provider event level information if it can be retrieved.
        /// </summary>
        /// <param name="providerMetadata">The provider metadata.</param>
        /// <returns>The provider event levels or an empty list of levels if the leve can't be retrieved.</returns>
        private static IList<EventLevelData> GetProviderEventLevels(ProviderMetadata providerMetadata)
        {
            IList<EventLevelData> levels = new List<EventLevelData>();

            try
            {
                foreach (EventLevelData levelData in providerMetadata.Levels.Select(level => new EventLevelData(level.DisplayName, level.Value)).Where(levelData => !levels.Contains(levelData)))
                    levels.Add(levelData);
            }
            catch (EventLogException ele)
            {
                Logger.Error(ele, CultureInfo.CurrentCulture, "Event provider '{0}' threw a generic event log exception while accessing the event provider Levels field: {1}{2}{3}", providerMetadata.Name, ele.Message, Environment.NewLine, ele.StackTrace);
            } //something is weird with Windows-MsiServer 

            return levels;
        }

        /// <summary>
        /// Get's the help file name from a URI.
        /// </summary>
        /// <param name="helpLink">The URI used as a help link.</param>
        /// <returns>Just the filename embedded in the help link.</returns>
        private static string GetHelpFileNameFromUri(Uri helpLink)
        {
            string link = string.Empty;

            if (helpLink != null)
            {
                string query = helpLink.Query;
                if (query.Contains("FileName="))
                {
                    int offset = "FileName=".Length;
                    int startIndex = query.IndexOf("FileName=", StringComparison.CurrentCultureIgnoreCase);
                    int endIndex = query.IndexOf("&", startIndex, StringComparison.CurrentCultureIgnoreCase);
                    link = query.Substring(startIndex + offset, endIndex - startIndex - offset);
                }
            }

            return link;
        }
    }
}
