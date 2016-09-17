using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace WelmLibrary.Classic
{
    public class ClassicEventData
    {
        /// <summary>
        /// The event log that the event is associated with.
        /// </summary>
        public string Log { get; set; }

        /// <summary>
        /// The source that the event is associated with.
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// The event ID.
        /// </summary>
        public EventId Id { get; set; }

        /// <summary>
        /// The event's message string. It contains substition variables for its parameters if it has any. Some events do not have a message.
        /// </summary>
        public string Message { get; set; }

        public ClassicEventData()
        {
            Log = string.Empty;
            Source = string.Empty;
            Id = new EventId();
            Message = string.Empty;
        }

        /// <summary>
        /// Creates event data associated with a specific log with the specified ID and message.
        /// </summary>
        /// <param name="logName">The name of the log for the event.</param>
        /// <param name="sourceName">The name of the source for the event.</param>
        /// <param name="id">The event ID.</param>
        /// <param name="message">The event message</param>
        public ClassicEventData(string logName, string sourceName, EventId id, string message)
        {
            Log = logName ?? string.Empty;
            Source = sourceName ?? string.Empty;
            Id = id;
            Message = message ?? string.Empty;
        }

        public static string ToFormat(IList<ClassicEventData> events, string format)
        {
            string data = string.Empty;

            if (string.IsNullOrEmpty(format))
            {
                format = "json";
            }

            if (events != null && events.Count > 0)
            {
                switch (format.ToLower(CultureInfo.CurrentCulture))
                {
                    case "txt":
                        StringBuilder txtBuilder = new StringBuilder(string.Empty);

                        foreach (ClassicEventData eventData in events)
                        {
                            txtBuilder.AppendFormat("{0}{1}", eventData, Environment.NewLine);
                        }

                        data = txtBuilder.ToString();
                        break;
                    case "json":
                        JsonSerializerSettings settings = new JsonSerializerSettings
                        {
                            Formatting = Formatting.None,
                            MaxDepth = int.MaxValue
                        };

                        data = JsonConvert.SerializeObject(events, settings);
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

                        StringBuilder csvBuilder = new StringBuilder(events.Count);

                        using (StringWriter sw = new StringWriter(csvBuilder, CultureInfo.InvariantCulture))
                        {
                            CsvWriter csvWriter = new CsvWriter(sw, config);

                            foreach (ClassicEventData evt in events)
                            {
                                csvWriter.WriteField<string>(evt.Log);
                                csvWriter.WriteField<string>(evt.Source);
                                csvWriter.WriteField<ushort>(evt.Id.Code);
                                csvWriter.WriteField<string>(string.Format(CultureInfo.CurrentCulture, "0x{0:X}",evt.Id.Value));
                                csvWriter.WriteField<string>(FormattingTools.FormatMessageForPlaintext(evt.Message));
                                csvWriter.NextRecord();
                            }

                            sw.Flush();
                        }

                        csvBuilder.Insert(0, "\"Log\",\"Source\",\"ID\",\"Value\",\"Message\"" + Environment.NewLine);
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
            output.AppendFormat("Log: {0}|", Log);
            output.AppendFormat("Source: {0}|", Source);
            output.AppendFormat("ID: {0}|", Id.Code);
            output.AppendFormat("Value: {0} (0x{0:X})|", Id.Value);

            if (!string.IsNullOrEmpty(Message))
            {
                Message = FormattingTools.FormatMessageForPlaintext(Message);

                output.AppendFormat("Message: {0}|", Message);
            }

            string s = output.ToString().Trim();

            if (s.EndsWith("|", true, CultureInfo.CurrentCulture))
            {
                s = s.TrimEnd(new char[] { '|' });
            }

            return s;
        }

        public override int GetHashCode()
        {
            return Message.GetHashCode() + Id.GetHashCode();
        }
    }
}
