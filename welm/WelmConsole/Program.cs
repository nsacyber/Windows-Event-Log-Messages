using DocoptNet;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using WelmLibrary;
using WelmLibrary.Classic;

namespace WelmConsole
{
    public static class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private const string Usage = @"Windows Event Log Messages (WELM)

    Usage:
        welm.exe -h 
        welm.exe ([-p | -l | -e]) -f Format

    Options:
        -h --help                   Shows this dialog.
        -p --providers              Retrieve all providers.
        -l --logs                   Retrieve all logs.
        -e --events                 Retrieve all events.
        -f Format, --format Format  Specify format. txt,json,csv, or all.
";

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += HandleCurrentDomainUnhandledException;

            Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);
            
            ParsedArguments arguments = null;

            try
            {
                IDictionary<string, ValueObject> rawArguments = new Docopt().Apply(Usage, args, version: fileVersion.FileVersion, exit: false);
                arguments = new ParsedArguments(rawArguments);
            }
            catch (DocoptExitException)
            {
                Console.WriteLine(Usage);
                Environment.Exit((int)ProcessCode.HelpInvoked);
            }
            catch (DocoptInputErrorException)
            {
                Console.WriteLine(Usage);
                Environment.Exit((int)ProcessCode.CommandLineInvalid);
            }

            Logger.Info(CultureInfo.CurrentCulture, "WELM version: {0}", fileVersion.FileVersion);
            Logger.Info(CultureInfo.CurrentCulture, "Command line arguments: {0}", string.Join(" ", args));

            OutputFormat format = arguments.Format;
            //OperationMode mode = OperationMode.None;

            OperatingSystem os = Environment.OSVersion;

            if (os.Version.Major >= 6)
            {
                IList<EventLogData> logs = new List<EventLogData>();
                IList<EventProviderData> providers = new List<EventProviderData>();
                IList<EventData> events = new List<EventData>();

                if (arguments.Logs)
                {
                    //mode = OperationMode.Logs;

                    logs = EventLogData.GetEventLogs();

                    if (format != OutputFormat.All)
                    {
                        File.WriteAllText("logs." + format.ToString().ToLower(CultureInfo.CurrentCulture), EventLogData.ToFormat(logs, format.ToString()));
                    }
                    else
                    {
                        File.WriteAllText("logs.json", EventLogData.ToFormat(logs, "json"));
                        File.WriteAllText("logs.txt", EventLogData.ToFormat(logs, "txt"));
                        File.WriteAllText("logs.csv", EventLogData.ToFormat(logs, "csv"));
                    }
                }
                else if (arguments.Providers)
                {
                    //mode = OperationMode.Providers;

                    providers = EventProviderData.GetProviders();

                    if (format != OutputFormat.All)
                    {
                        File.WriteAllText("providers." + format.ToString().ToLower(CultureInfo.CurrentCulture), EventProviderData.ToFormat(providers, format.ToString()));
                    }
                    else
                    {
                        File.WriteAllText("providers.json", EventProviderData.ToFormat(providers, "json"));
                        File.WriteAllText("providers.txt", EventProviderData.ToFormat(providers, "txt"));
                        File.WriteAllText("providers.csv", EventProviderData.ToFormat(providers, "csv"));
                    }

                }
                else if (arguments.Events)
                {
                    //mode = OperationMode.Events;

                    events = EventData.GetEvents();

                    if (format != OutputFormat.All)
                    {
                        File.WriteAllText("events." + format.ToString().ToLower(CultureInfo.CurrentCulture), EventData.ToFormat(events, format.ToString()));
                    }
                    else
                    {
                        File.WriteAllText("events.json", EventData.ToFormat(events, "json"));
                        File.WriteAllText("events.txt", EventData.ToFormat(events, "txt"));
                        File.WriteAllText("events.csv", EventData.ToFormat(events, "csv"));
                    }
                }
            }

            if (os.Version.Major >= 5)
            {
                IList<ClassicEventLogData> classicEventLogs = new List<ClassicEventLogData>();
                IList<EventSourceData> eventSources = new List<EventSourceData>();
                IList<ClassicEventData> classicEvents = new List<ClassicEventData>();

                if (arguments.Logs)
                {
                    //mode = OperationMode.Logs;

                    classicEventLogs = ClassicEventLogData.GetEventLogs();

                    if (format != OutputFormat.All)
                    {
                        File.WriteAllText("classiclogs." + format.ToString().ToLower(CultureInfo.CurrentCulture), ClassicEventLogData.ToFormat(classicEventLogs, format.ToString()));
                    }
                    else
                    {
                        File.WriteAllText("classiclogs.json", ClassicEventLogData.ToFormat(classicEventLogs, "json"));
                        File.WriteAllText("classiclogs.txt", ClassicEventLogData.ToFormat(classicEventLogs, "txt"));
                        File.WriteAllText("classiclogs.csv", ClassicEventLogData.ToFormat(classicEventLogs, "csv"));
                    }
                }
                else if (arguments.Providers)
                {
                    //mode = OperationMode.Providers;

                    classicEventLogs = ClassicEventLogData.GetEventLogs();

                    foreach (var eventLog in classicEventLogs)
                    {
                        //foreach (var eventSource in eventLog.Sources)
                        foreach (var eventSource in EventSourceData.GetEventSources(eventLog.Name))
                            eventSources.Add(eventSource);

                        eventLog.Sources = eventSources;
                    }

                    if (format != OutputFormat.All)
                    {
                        File.WriteAllText("classicsources." + format.ToString().ToLower(CultureInfo.CurrentCulture), EventSourceData.ToFormat(eventSources, format.ToString()));
                    }
                    else
                    {
                        File.WriteAllText("classicsources.json", EventSourceData.ToFormat(eventSources, "json"));
                        File.WriteAllText("classicsources.txt", EventSourceData.ToFormat(eventSources, "txt"));
                        File.WriteAllText("classicsources.csv", EventSourceData.ToFormat(eventSources, "csv"));
                    }
                }
                else if (arguments.Events)
                {
                    //mode = OperationMode.Events;

                    classicEventLogs = ClassicEventLogData.GetEventLogs();

                    foreach (var eventLog in classicEventLogs)
                    {
                        //foreach (var eventSource in eventLog.Sources)
                        foreach (var eventSource in EventSourceData.GetEventSources(eventLog.Name))
                            eventSources.Add(eventSource);

                        eventLog.Sources = eventSources;
                    }

                    foreach (var messageFile in EventMessageFileCache.Instance.MessageFiles())
                    {
                        foreach (ClassicEventData eventData in messageFile.Value.GetEvents())
                            classicEvents.Add(eventData);
                    }

                    if (format != OutputFormat.All)
                    {
                        File.WriteAllText("classicevents." + format.ToString().ToLower(CultureInfo.CurrentCulture), ClassicEventData.ToFormat(classicEvents, format.ToString()));
                    }
                    else
                    {
                        File.WriteAllText("classicevents.json", ClassicEventData.ToFormat(classicEvents, "json"));
                        File.WriteAllText("classicevents.txt", ClassicEventData.ToFormat(classicEvents, "txt"));
                        File.WriteAllText("classicevents.csv", ClassicEventData.ToFormat(classicEvents, "csv"));
                    }
                }
            }
        }

        private static void HandleCurrentDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            string message;

            Exception exception = e.ExceptionObject as Exception;

            if (exception != null)
            {
                message = string.Format(CultureInfo.CurrentCulture, "The application encountered an unhandled exception: {0}{1}{2}", exception.Message, Environment.NewLine, exception.StackTrace);
            }
            else
            {
                message = "The application encountered an unknown unhandled exception.";
            }

            Logger.Fatal(exception, message);

            Environment.Exit((int)ProcessCode.UnhandledException);
        }
    }

    /// <summary>
    /// The different process return codes.
    /// </summary>
    internal enum ProcessCode
    {
        /// <summary>
        /// Successful run.
        /// </summary>
        Success = 0,

        /// <summary>
        /// Help command was invoked.
        /// </summary>
        HelpInvoked = -1,

        /// <summary>
        /// One of the command line arguments was invalid.
        /// </summary>
        CommandLineInvalid = -2,

        /// <summary>
        /// An unhandled exception occured.
        /// </summary>
        UnhandledException = -3
    }

    /// <summary>
    /// The modes of operation the tool runs in.
    /// </summary>
    public enum OperationMode
    {
        /// <summary>
        /// No or unknown tool mode.
        /// </summary>
        None,

        /// <summary>
        /// Tool is running in log retrieval mode.
        /// </summary>
        Logs,

        /// <summary>
        /// Tool is running in provider retrieval mode.
        /// </summary>
        Providers,

        /// <summary>
        /// Tool is running in event retrieval mode.
        /// </summary>
        Events
    }
}
