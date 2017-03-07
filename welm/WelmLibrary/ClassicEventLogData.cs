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
using System.Text;

namespace WelmLibrary.Classic
{
    public class ClassicEventLogData
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The registry path where Windows Event Log data resides for old event log information.
        /// </summary>
        private const string EventLogKey = @"SYSTEM\CurrentControlSet\services\eventlog";

        /// <summary>
        /// The name of the event log.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The list of event sources registered for the event log. 
        /// </summary>
        public IList<EventSourceData> Sources { get; set; }

        /// <summary>
        /// Creates pre-Vista event log data for the specified log name.
        /// </summary>
        /// <param name="name">The event log name.</param>
        public ClassicEventLogData(string name)
        {
            Name = name;
            Sources = EventSourceData.GetEventSources(name);
        }

        /// <summary>
        /// Retrieves pre-Vista event log data from the system.
        /// </summary>
        /// <returns>A list of event log data.</returns>
        public static IList<ClassicEventLogData> GetEventLogs()
        {
            List<ClassicEventLogData> eventLogs = new List<ClassicEventLogData>();

            using (var eventLogKey = Registry.LocalMachine.OpenSubKey(EventLogKey))
            {
                if (eventLogKey != null)
                {
                    eventLogs.AddRange(eventLogKey.GetSubKeyNames().Select(name => new ClassicEventLogData(name)));
                }
                else
                {
                    Logger.Error(CultureInfo.CurrentCulture, "Failed to open registry key '{0}' while enumerating classic event logs", EventLogKey);
                }
            }

            return eventLogs;
        }

        public static string ToFormat(IList<ClassicEventLogData> logs, string format)
        {
            string data = string.Empty;

            if (string.IsNullOrEmpty(format))
            {
                format = "json";
            }

            if (logs != null && logs.Count > 0)
            {
                switch (format.ToLower(CultureInfo.CurrentCulture))
                {
                    case "txt":
                        StringBuilder txtBuilder = new StringBuilder(string.Empty);

                        foreach (ClassicEventLogData logData in logs)
                        {
                            txtBuilder.AppendFormat("{0}{1}", logData, Environment.NewLine);
                        }

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

                            foreach (ClassicEventLogData log in logs)
                            {
                                csvWriter.WriteField<string>(log.Name);
                                csvWriter.WriteField<int>(log.Sources.Count); // no header due to type mismatch for property
                                csvWriter.NextRecord();
                            }

                            sw.Flush();
                        }

                        csvBuilder.Insert(0, "\"Name\",\"Sources\"" + Environment.NewLine);
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
            output.AppendFormat("Sources: {0}", Sources.Count);

            return output.ToString();
        }
    }
}
