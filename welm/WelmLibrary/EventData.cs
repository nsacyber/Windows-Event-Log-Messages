using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace WelmLibrary
{
    public class EventData : IEquatable<EventData>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// This is the schema used by the Windows Event Log XML for events.
        /// </summary>
        private const string EventSchema = "http://schemas.microsoft.com/win/2004/08/events";

        /// <summary>
        /// The data elements hold the parameter data in the template XML for an event.
        /// </summary>
        private const string DataElement = "data";

        /// <summary>
        /// The name attribute holds the parameter name in the template XML for an event.
        /// </summary>
        private const string NameAttribute = "name";

        /// <summary>
        /// The inType attribute holds the attribute type. These match up with the EVT_VARIANT_TYPE enum.
        /// <see cref="http://msdn.microsoft.com/en-us/library/windows/desktop/aa385616(v=vs.85).aspx"/>
        /// </summary>
        private const string TypeAttribute = "inType";

        /// <summary>
        /// The event provider that the event is associated with.
        /// </summary>
        public string Provider { get; }

        /// <summary>
        /// The event number that corresponds to the Event ID column seen in the Event Viewer.
        /// </summary>
        public EventId Identifier { get; }

        /// <summary>
        /// The event level that corresponds to the Level column seen in the Event Viewer. Generally these are Error, Informational, Warning, Critical, or Verbose.
        /// </summary>
        public EventLevelData Level { get; }

        /// <summary>
        /// The version number of the event.
        /// </summary>
        public byte Version { get; }

        /// <summary>
        /// The event task generally gives more specific information about which component is logging information.
        /// </summary>
        public EventTaskData Task { get; }

        /// <summary>
        /// The event opcode generally gives information about an action being done when logging information.
        /// </summary>
        public EventOpcodeData Opcode { get; }

        /// <summary>
        /// Event keywords are used to classify or group similar types of events. 
        /// </summary>
        public IList<EventKeywordData> Keywords { get; }

        /// <summary>
        /// The event log/channel that the event is logged to.
        /// </summary>
        public EventLogData LoggedTo { get; }

        /// <summary>
        /// The event's message string. It contains substitution variables for its parameters, if any. Some events do not have a description.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// A list of parameter names and types used in the event message. These are ordered in the list according to their substitution position. The position is 1-based.
        /// </summary>
        public OrderedDictionary Parameters { get; }

        /// <summary>
        /// Creates event data associated with a specific provider with the specified event metadata.
        /// </summary>
        /// <param name="providerName">The name of the provider.</param>
        /// <param name="metadata">The event metadata.</param>
        public EventData(string providerName, EventMetadata metadata)
        {
            if (metadata != null)
            {
                Provider = string.IsNullOrEmpty(providerName) ? string.Empty : providerName;
                Identifier = new EventId(metadata.Id);
                Level = new EventLevelData(metadata.Level.DisplayName, metadata.Level.Value);
                Version = metadata.Version;
                Task = new EventTaskData(metadata.Task.Name, metadata.Task.DisplayName, metadata.Task.Value, metadata.Task.EventGuid);
                Opcode = new EventOpcodeData(metadata.Opcode.DisplayName, metadata.Opcode.Value);
                Keywords = EventKeywordData.GetKeywords(metadata.Keywords);
                LoggedTo = string.IsNullOrEmpty(metadata.LogLink.LogName) ? new EventLogData() : new EventLogData(metadata.LogLink.LogName);
                Message = metadata.Description ?? string.Empty;

                // only check the template for parameters if the event has an actual message string
                //Parameters = string.IsNullOrEmpty(metadata.Description) ? new OrderedDictionary() : GetEventParametersFromXmlTemplate(metadata.Template);
                Parameters = string.IsNullOrEmpty(metadata.Template) ? new OrderedDictionary() : GetEventParametersFromXmlTemplate(metadata.Template);

                // use the officially defined level information when it exists, otherwise we try to guess the level from Identifier.Severity which may or may not be accurate
                // previously, there was a question mark added to the end of the level name to denote this case but it was removed since it seems like a valid guess
                if (string.IsNullOrEmpty(Level.Name))
                {
                    Level = new EventLevelData(string.Format(CultureInfo.CurrentCulture, "{0}", Identifier.Severity), (int)Enum.Parse(typeof(NtSeverity), Identifier.Severity));
                }

                // odd case but fairly common
                if (string.IsNullOrEmpty(metadata.Description) && !string.IsNullOrEmpty(metadata.Template))
                {
                    Logger.Debug(CultureInfo.CurrentCulture, "Event did not have a message but had message parameters defined. {0}", this);
                }

                // another odd case but seems to be normal for "Classic" events so log only the non-classic instances
                if (!string.IsNullOrEmpty(metadata.Description) && metadata.Description.Contains("%1") && string.IsNullOrEmpty(metadata.Template) && (Keywords.Count(keyword => keyword.Name.Equals("Classic")) == 0))
                {
                    Logger.Debug(CultureInfo.CurrentCulture, "Event had a message with a parameter but no message parameters were defined. {0}", this);
                }
            }
            else
            {
                Provider = string.Empty;
                Identifier = new EventId();
                Level = new EventLevelData();
                Version = 0;
                Task = new EventTaskData();
                Opcode = new EventOpcodeData();
                Keywords = new List<EventKeywordData>();
                LoggedTo = new EventLogData();
                Message = string.Empty;      
                Parameters = new OrderedDictionary();
            }
        }

        /// <summary>
        /// Retrieves event data from the system based on event metadata.
        /// </summary>
        /// <returns>A list of events.</returns>
        public static IList<EventData> GetEvents()
        {
            IList<EventData> events = new List<EventData>();

            using (EventLogSession session = new EventLogSession())
            {
                foreach (string providerName in session.GetProviderNames())
                {
                    ProviderMetadata provider = null;

                    try
                    {
                        provider = new ProviderMetadata(providerName);

                        string provName = providerName; // prevents "Access to foreach variable in a closure" warning

                        foreach (EventData eventData in provider.Events.Select(eventMetadata => new EventData(provName, eventMetadata)).Where(eventData => !events.Contains(eventData)))
                        {
                            events.Add(eventData);
                        }
                    }
                    catch (EventLogNotFoundException elnfe)
                    {
                        // Microsoft-Windows-TerminalServices-ServerUSBDevice = The system cannot find the file specified.
                        // Microsoft-Windows-WPD-MTPClassDriver = The system cannot find the file specified
                        // Microsoft-Windows-Sdbus-SQM = The system cannot find the files specified

                        Logger.Error(elnfe, CultureInfo.CurrentCulture, "Event provider '{0}' not found while processing events: {1}{2}{3}", providerName, elnfe.Message, Environment.NewLine, elnfe.StackTrace);

                    }
                    catch (EventLogException ele)
                    {
                        // Microsoft-Windows-MsiServer = The specified resource type cannot be found in the image file
                        // Microsoft-Windows-CAPI2 = The data is invalid

                        Logger.Error(ele, CultureInfo.CurrentCulture, "Event provider '{0}' threw a generic event log exception while processing events: {1}{2}{3}", providerName, ele.Message, Environment.NewLine, ele.StackTrace);
                    }
                    catch (UnauthorizedAccessException uae)
                    {
                        // thrown when running as a normal user and accessing these:
                        // Microsoft-Windows-Security-Auditing 
                        // Microsoft-Windows-Eventlog

                        Logger.Error(uae, CultureInfo.CurrentCulture, "Access denied to event provider '{0}' while processing events: {1}{2}{3}", providerName, uae.Message, Environment.NewLine, uae.StackTrace);
                    }
                    finally
                    {
                        provider?.Dispose();
                    }
                }
            }

            return events;
        }

        public static string ToFormat(IList<EventData> events, string format)
        {
            string data = string.Empty;

            if (string.IsNullOrEmpty(format))
            {
                format = "json";
            }

            if (events != null && events.Any())
            {
                switch (format.ToLower(CultureInfo.CurrentCulture))
                {
                    case "txt":
                        StringBuilder txtBuilder = new StringBuilder(string.Empty);

                        foreach (EventData eventData in events)
                        {
                            txtBuilder.AppendFormat("{0}{1}", eventData, Environment.NewLine);
                        }

                        data = txtBuilder.ToString();
                        break;
                    case "json":
                        JsonSerializerSettings settings = new JsonSerializerSettings
                        {
                            MaxDepth = int.MaxValue,
                            Formatting = Formatting.None,
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

                            foreach (EventData evt in events)
                            {

                                csvWriter.WriteField<string>(evt.Provider);
                                csvWriter.WriteField<ushort>(evt.Identifier.Code);
                                csvWriter.WriteField<string>(string.Format(CultureInfo.CurrentCulture, "0x{0:X}", evt.Identifier.Value));
                                csvWriter.WriteField<string>(evt.Level.Name);
                                csvWriter.WriteField<byte>(evt.Version);
                                csvWriter.WriteField<string>(evt.Task.Name);
                                csvWriter.WriteField<string>(evt.Opcode.Name);
                                csvWriter.WriteField<string>(CreateKeywordOutput(evt.Keywords));
                                csvWriter.WriteField<string>(evt.LoggedTo.Name);
                                csvWriter.WriteField<string>(FormattingTools.FormatMessageForPlaintext(evt.Message));
                                csvWriter.WriteField<string>(CreateParameterOutput(evt.Parameters));
                                csvWriter.NextRecord();
                            }

                            sw.Flush();
                        }

                        csvBuilder.Insert(0, "\"Provider\",\"Code\",\"Value\",\"Level\",\"Version\",\"Task\",\"Opcode\",\"Keywords\",\"LoggedTo\",\"Message\",\"Parameters\"" + Environment.NewLine);
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

            output.AppendFormat("Provider: {0}|", Provider);
            output.AppendFormat("Code: {0}|", Identifier.Code);
            output.AppendFormat("Value: {0} (0x{0:X})|", Identifier.Value);
            output.AppendFormat("Level: {0}|", Level.Name);
            output.AppendFormat("Version: {0}|", Version);

            if (!string.IsNullOrEmpty(Task.Name))
            {
                output.AppendFormat("Task: {0}|", Task.Name);
            }

            if (!string.IsNullOrEmpty(Opcode.Name))
            {
                output.AppendFormat("Opcode: {0}|", Opcode.Name);
            }

            if (Keywords != null && Keywords.Count > 0)
            {
                output.AppendFormat("Keywords: {0}|", CreateKeywordOutput(Keywords));
            }

            if (!string.IsNullOrEmpty(LoggedTo.Name))
            {
                output.AppendFormat("Logged To: {0}|", LoggedTo.Name);
            }

            if (!string.IsNullOrEmpty(Message))
            {
                Message = FormattingTools.FormatMessageForPlaintext(Message);

                output.AppendFormat("Message: {0}|", Message);

                if (Parameters != null && Parameters.Count > 0)
                {
                    output.AppendFormat("Parameters: {0}", CreateParameterOutput(Parameters));
                }
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
            return Equals(obj as EventData);
        }

        public bool Equals(EventData other)
        {
            bool equal = false;

            if (other != null)
            {
                if (Provider != null && other.Provider != null && Provider.Equals(other.Provider))
                {
                    if (Identifier != null && other.Identifier != null && Identifier.Equals(other.Identifier))
                    {
                        if (Level != null && other.Level != null && Level.Equals(other.Level))
                        {
                            equal = true;
                        }
                    }
                }
            }

            return equal;
        }

        public override int GetHashCode()
        {
            // event IDs should be unique per provider but just in case they aren't, use the event ID and level too 
            return Provider.GetHashCode() + Identifier.Code.GetHashCode() + Level.Value.GetHashCode();
        }

        /// <summary>
        /// Gets the parameters from the template XML metadata and returns them in the correct order as they are used in the event message.
        /// </summary>
        /// <param name="template">The template XML string.</param>
        /// <returns>The ordered parameter list.</returns>
        private static OrderedDictionary GetEventParametersFromXmlTemplate(string template)
        {
            /**
               Example
                                  
               Message: 
               "The time provider '%1' logged the following error: %2"
                
               Template:
               <template xmlns="http://schemas.microsoft.com/win/2004/08/events">
                   <data name="TimeProvider" inType="win:UnicodeString" outType="xs:string"/>
                   <data name="ErrorMessage" inType="win:UnicodeString" outType="xs:string"/>
               </template>
                  
             The order of the data elements in the XML match up EXACTLY with %1, %2, %3 in the message. 
             Because of that reason, we can't use any LINQ and dictionary conversion:
                  
                parameters = parameterXml.Root.Elements("{http://schemas.microsoft.com/win/2004/08/events}data").Select(
                    p => new
                        {
                            Name = p.Attributes("name").Single().Value,
                            DataType = p.Attributes("inType").Single().Value
                        }).ToDictionary(p => p.Name, p => p.DataType);
                  
                     
             Dictionary<string,string> is NOT ordered so the above code resulted in parameters not matching up to their substitution number              
             **/

            OrderedDictionary parameters = new OrderedDictionary();

            if (!string.IsNullOrEmpty(template))
            {
                int nameCount = 0;

                XDocument parameterXml = XDocument.Parse(template, LoadOptions.None);

                if (parameterXml.Root != null)
                {
                    // must specify the namespace when selecting elements otherwise no elements will be returned
                    foreach (XElement element in parameterXml.Root.Elements(string.Format(CultureInfo.CurrentCulture, "{{{0}}}{1}", EventSchema, DataElement)))
                    {
                        string name = element.Attributes(NameAttribute).Single().Value;
                        string type = element.Attributes(TypeAttribute).Single().Value;

                        // some developers re-used the same parameter name in the same message so we need to give it a unique name to avoid duplicates in the dictionary
                        // key = original parameter name + "-" + count
                        // value = original parameter name,data type
                        if (parameters.Contains(name))
                        {
                            nameCount++;
                            parameters.Add(name + "-" + nameCount, name + "," + type);
                        }
                        else
                        {
                            parameters.Add(name, name + "," + type);
                        }
                    }
                }
            }

            return parameters;
        }

        /// <summary>
        /// Creates a formatted string of parameters.
        /// </summary>
        /// <param name="parameters">The parameters to format.</param>
        /// <returns>A formatted parameter string.</returns>
        private static string CreateParameterOutput(OrderedDictionary parameters)
        {
            StringBuilder output = new StringBuilder(string.Empty);

            int paramNumber = 0;

            // key = unique parameter name (name + "-" + count)
            // value = original parameter name, data type
            foreach (string key in parameters.Keys)
            {
                paramNumber++;

                string[] parts = parameters[key].ToString().Split(new [] { "," }, StringSplitOptions.None);

                output.Append("%" + paramNumber + "=" + parts[0]);
                output.Append("(" + parts[1] + ")");
                output.Append(", ");
            }

            string s = output.ToString().Trim();

            if (s.EndsWith(",", true, CultureInfo.CurrentCulture))
            {
                s = s.TrimEnd(',');
            }

            return s;
        }

        /// <summary>
        /// Creates a formatted string of keywords
        /// </summary>
        /// <param name="keywords">The keywords to format.</param>
        /// <returns>The formatted keywords.</returns>
        private static string CreateKeywordOutput(IList<EventKeywordData> keywords)
        {
            StringBuilder output = new StringBuilder(string.Empty);

            if (keywords != null && keywords.Count > 0)
            {
                foreach (EventKeywordData keyword in keywords)
                {
                    output.Append(string.IsNullOrEmpty(keyword.Name)
                        ? string.Format(CultureInfo.CurrentCulture, "0x{0:X}", keyword.Value)
                        : keyword.Name);

                    output.Append(", ");
                }
            }

            string s = output.ToString().Trim();

            if (s.EndsWith(",", true, CultureInfo.CurrentCulture))
            {
                s = s.TrimEnd(',');
            }

            return s;
        }
    }
}
