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
    public class EventLogData : IEquatable<EventLogData>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The name of the event log.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The owning event provider name that creates the log.
        /// </summary>

        public string Provider { get; set; }
        /// <summary>
        /// Maximum log size in kilobytes.
        /// </summary>
        public long MaximumSize { get; set; }

        /// <summary>
        /// The log retention policy. It can Circular, AutoBackup, or Retain.
        /// <see cref="System.Diagnostics.Eventing.Reader.EventLogIsolation"/>
        /// </summary>
        public string Retention { get; set; }

        /// <summary>
        /// Specifies if the log is enabled or not.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>

        /// Specifies if the log metadata was defined with a .mc file manifest or an XML file manifest. The .mc file is the classic format.
        /// </summary>
        public bool IsClassic { get; set; }

        /// <summary>
        /// The type of log file. It can be Administrative, Operational, Analytical, or Debug.
        /// <see cref="System.Diagnostics.Eventing.Reader.EventLogType"/>
        /// </summary>
        public string LogType { get; set; }

        /// <summary>
        /// The security isolation of the log file. It can be Application, System, or Custom.
        /// <see cref="System.Diagnostics.Eventing.Reader.EventLogIsolation"/>
        /// </summary>
        public string Isolation { get; set; }

        /// <summary>
        /// For a debug type log this will be a GUID to identify the log. The is null GUID when it is not a debug log type.
        /// </summary>
        public Guid DebugGuid { get; set; }

        /// <summary>
        /// The names of providers that can publish information to this log.
        /// </summary>
        public IList<string> Providers { get; private set; }

        /// <summary>
        /// The path of the file that log stores its data.
        /// </summary>
        public string FilePath { get; set; }

        public EventLogData()
        {
            Name = string.Empty;
            Provider = string.Empty;
            MaximumSize = 0;
            Retention = string.Empty;
            IsEnabled = false;
            IsClassic = false;
            LogType = string.Empty;
            Isolation = string.Empty;
            DebugGuid = Guid.Empty;
            // maybe change this to a list of ProviderData objects in the future since that'd give more information 
            // it will add the overhead of retrieving all the events though which significantly slows log retrieval
            Providers = new List<string>();
            FilePath = string.Empty;
        }

        /// <summary>
        /// Create event log data based on the log name.
        /// </summary>
        /// <param name="logName">The name of the log</param>
        public EventLogData(string logName)
        {
            try
            {
                using (EventLogConfiguration logConfig = new EventLogConfiguration(logName))
                {
                    Name = logConfig.LogName ?? string.Empty;
                    Provider = logConfig.OwningProviderName ?? string.Empty;
                    MaximumSize = logConfig.MaximumSizeInBytes / 1024;
                    Retention = Enum.GetName(typeof(EventLogMode), logConfig.LogMode);
                    IsEnabled = logConfig.IsEnabled;
                    IsClassic = logConfig.IsClassicLog;
                    LogType = Enum.GetName(typeof(EventLogType), logConfig.LogType);
                    Isolation = Enum.GetName(typeof(EventLogIsolation), logConfig.LogIsolation);
                    DebugGuid = logConfig.ProviderControlGuid ?? Guid.Empty;
                    Providers = logConfig.ProviderNames == null ? new List<string>() : logConfig.ProviderNames.ToList<string>();
                    FilePath = logConfig.LogFilePath ?? string.Empty;
                }
            }
            catch (UnauthorizedAccessException uae)
            {
                // trying to access the Security log while running as non-admin causes this exception.

                Name = string.Empty;
                Provider = string.Empty;
                MaximumSize = 0;
                Retention = string.Empty;
                IsEnabled = false;
                IsClassic = false;
                LogType = string.Empty;
                Isolation = string.Empty;
                DebugGuid = Guid.Empty;
                Providers = new List<string>();
                FilePath = string.Empty;

                Logger.Warn(uae, CultureInfo.CurrentCulture, "Access denied to event log '{0}' while processing event logs: {1}{2}{3}", logName, uae.Message, Environment.NewLine, uae.StackTrace);
            }
        }

        /// <summary>
        /// Retrieves event log data from the system based on event log metadata.
        /// </summary>
        /// <returns>A list of event log data.</returns>
        public static IList<EventLogData> GetEventLogs()
        {
            IList<EventLogData> logs = new List<EventLogData>();

            using (EventLogSession session = new EventLogSession())
            {
                foreach (EventLogData logData in session.GetLogNames().Select(logName => new EventLogData(logName)).Where(logData => !logs.Contains(logData)))
                {
                    logs.Add(logData);
                }
            }

            return logs;
        }

        public static string ToFormat(IList<EventLogData> logs, string format)
        {
            string data = string.Empty;
            //string statsOutput = string.Empty;

            if (string.IsNullOrEmpty(format))
            {
                format = "json";
            }

            if (logs != null && logs.Any())
            {
                switch (format.ToLower(CultureInfo.CurrentCulture))
                {
                    case "txt":
                        StringBuilder txtBuilder = new StringBuilder(string.Empty);

                        foreach (EventLogData logData in logs)
                        {
                            txtBuilder.AppendFormat("{0}{1}", logData, Environment.NewLine);
                        }

                        //output.AppendFormat("{0}{1}", Environment.NewLine, new EventLogStatistics(logs));

                        data = txtBuilder.ToString();
                        break;
                    case "json":
                        JsonSerializerSettings settings = new JsonSerializerSettings
                        {
                            MaxDepth = int.MaxValue,
                            Formatting = Formatting.None
                        };

                        data = JsonConvert.SerializeObject(logs, settings);
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

                        StringBuilder csvBuilder = new StringBuilder(logs.Count);

                        using (StringWriter sw = new StringWriter(csvBuilder, CultureInfo.InvariantCulture))
                        {
                            CsvWriter csvWriter = new CsvWriter(sw, config);

                            foreach(EventLogData log in logs)
                            {
                                csvWriter.WriteField<string>(log.Name);
                                csvWriter.WriteField<string>(log.Provider);
                                csvWriter.WriteField<long>(log.MaximumSize);
                                csvWriter.WriteField<string>(log.Retention);
                                csvWriter.WriteField<bool>(log.IsEnabled);
                                csvWriter.WriteField<bool>(log.IsClassic);
                                csvWriter.WriteField<string>(log.LogType);
                                csvWriter.WriteField<string>(log.Isolation);
                                csvWriter.WriteField<Guid>(log.DebugGuid);
                                csvWriter.WriteField<int>(log.Providers.Count); // no header due to type mismatch for property
                                csvWriter.WriteField<string>(log.FilePath);
                                csvWriter.NextRecord();
                            }

                            sw.Flush();
                        }

                        csvBuilder.Insert(0, "\"Name\",\"Provider\",\"MaximumSize\",\"Retention\",\"IsEnabled\",\"IsClassic\",\"LogType\",\"Isolation\",\"DebugGuid\",\"Providers\",\"FilePath\"" + Environment.NewLine);
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

            if (!string.IsNullOrEmpty(Provider))
            {
                output.AppendFormat("Provider: {0}|", Provider);
            }

            output.AppendFormat("MaximumSize: {0}", MaximumSize);
            output.AppendFormat("Retention: {0}|", Retention);
            output.AppendFormat("Enabled: {0}|", IsEnabled);
            output.AppendFormat("Classic: {0}|", IsClassic);
            output.AppendFormat("LogType: {0}|", LogType);
            output.AppendFormat("Isolation: {0}|", Isolation);

            if (!Guid.Empty.Equals(DebugGuid))
            {
                output.AppendFormat("DebugGuid: {0}|", DebugGuid);
            }

            output.AppendFormat("Providers: {0}|", Providers.Count);
            output.AppendFormat("FilePath: {0}", FilePath);

            string s = output.ToString().Trim();

            if (s.EndsWith("|", true, CultureInfo.CurrentCulture))
            {
                s = s.TrimEnd(new char[] { '|' });
            }

            return s;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as EventLogData);
        }

        public bool Equals(EventLogData other)
        {
            bool equal = false;

            if (other != null)
            {
                if (Name != null && other.Name != null && Name.Equals(other.Name))
                {
                    equal = true;
                }
            }

            return equal;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
