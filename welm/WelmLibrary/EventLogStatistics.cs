using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Linq;
using System.Text;
using WelmLibrary.Classic;

namespace WelmLibrary
{
    public class EventLogStatistics
    {
        public int Logs { get; set; }

        /// <summary>
        /// The number of enabled logs.
        /// </summary>
        public int Enabled { get; set; }

        /// <summary>
        /// The number of logs that use a .mc manifest file to generate event log information.
        /// </summary>
        public int Classic { get; set; }

        /// <summary>
        /// The number of operational logs.
        /// </summary>
        public int Operational { get; set; }

        /// <summary>
        /// The number of analytical logs.
        /// </summary>
        public int Analytical { get; set; }

        /// <summary>
        /// The number of administrative logs.
        /// </summary>
        public int Administrative { get; set; }

        /// <summary>
        /// The number of debug logs.
        /// </summary>
        public int Debug { get; set; }

        /// <summary>
        /// The number of logs that have application level isolation.
        /// </summary>
        public int Application { get; set; }

        /// <summary>
        /// The number of logs that have system level isolation.
        /// </summary>
        public int System { get; set; }

        /// <summary>
        /// The number of logs that have custom level isolation.
        /// </summary>
        public int Custom { get; set; }

        /// <summary>
        /// The number of logs that use the circular retention method.
        /// </summary>
        public int Circular { get; set; }

        /// <summary>
        /// The number of logs that use the retain retention method.
        /// </summary>
        public int Retain { get; set; }

        /// <summary>
        /// The number of logs that use the autobackup retention method.
        /// </summary>
        public int Auto { get; set; }

        /// <summary>
        /// Create new event log statistics based on the specified logs.
        /// </summary>
        /// <param name="logs">The logs get statistics from.</param>
        public EventLogStatistics(IList<EventLogData> logs)
        {
            if (logs != null && logs.Count > 0)
            {
                var enabled = from x in logs where x.IsEnabled select x;
                var classic = from x in logs where x.IsClassic select x;
                var operational = from x in logs where x.LogType.Equals(EventLogType.Operational.ToString(), StringComparison.CurrentCultureIgnoreCase) select x;
                var analytical = from x in logs where x.LogType.Equals(EventLogType.Analytical.ToString(), StringComparison.CurrentCultureIgnoreCase) select x;
                var administrative = from x in logs where x.LogType.Equals(EventLogType.Administrative.ToString(), StringComparison.CurrentCultureIgnoreCase) select x;
                var debug = from x in logs where x.LogType.Equals(EventLogType.Debug.ToString(), StringComparison.CurrentCultureIgnoreCase) select x;
                var application = from x in logs where x.Isolation.Equals(EventLogIsolation.Application.ToString(), StringComparison.CurrentCulture) select x;
                var system = from x in logs where x.Isolation.Equals(EventLogIsolation.System.ToString(), StringComparison.CurrentCulture) select x;
                var custom = from x in logs where x.Isolation.Equals(EventLogIsolation.Custom.ToString(), StringComparison.CurrentCulture) select x;
                var circular = from x in logs where x.Retention.Equals(EventLogMode.Circular.ToString(), StringComparison.CurrentCulture) select x;
                var retain = from x in logs where x.Retention.Equals(EventLogMode.Retain.ToString(), StringComparison.CurrentCulture) select x;
                var auto = from x in logs where x.Retention.Equals(EventLogMode.AutoBackup.ToString(), StringComparison.CurrentCulture) select x;

                int classicCount = classic.Count();

                Logs = logs.Count;
                Enabled = enabled.Count();
                Classic = classicCount;
                Operational = operational.Count();
                Analytical = analytical.Count();
                Administrative = administrative.Count();
                Debug = debug.Count();
                Application = application.Count();
                System = system.Count();
                Custom = custom.Count();
                Circular = circular.Count();
                Retain = retain.Count();
                Auto = auto.Count();
            }
        }

        public EventLogStatistics(IList<ClassicEventLogData> logs)
        {
            if (logs != null && logs.Count > 0)
            {
                Logs = logs.Count;
            }
        }

        public override string ToString()
        {
            StringBuilder output = new StringBuilder(string.Empty);

            output.AppendFormat(CultureInfo.CurrentCulture, "Logs: {0}{1}", Logs, Environment.NewLine);
            output.AppendFormat(CultureInfo.CurrentCulture, "Enabled: {0}{1}", Enabled, Environment.NewLine);
            output.AppendFormat(CultureInfo.CurrentCulture, "Type: Operational: {0}, Analytical: {1}, Administrative: {2}, Debug: {3}{4}", Operational, Analytical, Administrative, Debug, Environment.NewLine);
            output.AppendFormat(CultureInfo.CurrentCulture, "Manifest: .mc: {0}, .xml: {1}{2}", Classic, Logs - Classic, Environment.NewLine);
            output.AppendFormat(CultureInfo.CurrentCulture, "Isolation: Application: {0}, System: {1}, Custom: {2}{3}", Application, System, Custom, Environment.NewLine);
            output.AppendFormat(CultureInfo.CurrentCulture, "Retention: Circular: {0}, Retain: {1}, Autobackup: {2}", Circular, Retain, Auto);

            return output.ToString();
        }
    }
}
