using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WelmLibrary.Classic;

namespace WelmLibrary
{
    public class EventProviderStatistics
    {
        /// <summary>
        /// The number of providers.
        /// </summary>
        public int Providers { get; set; }

        /// <summary>
        /// The number of providers that have events.
        /// </summary>
        public int ProvidersWithEvents { get; set; }

        /// <summary>
        /// The total number of events.
        /// </summary>
        public int Events { get; set; }

        /// <summary>
        /// Create new provider statistics based on the specified providers.
        /// </summary>
        /// <param name="providers">The providers to get statistics from.</param>
        public EventProviderStatistics(IList<EventProviderData> providers)
        {
            if (providers != null && providers.Count > 0)
            {
                Providers = providers.Count;

                var providersWithEvents = from x in providers where x.Events.Count > 0 select x;
                ProvidersWithEvents = providersWithEvents.Count();

                Events = (from x in providers select x.Events.Count).Sum();
            }
        }

        public EventProviderStatistics(IList<EventSourceData> sources)
        {
            if (sources != null && sources.Count > 0)
            {
                Providers = sources.Count;
            }
        }

        public override string ToString()
        {
            StringBuilder output = new StringBuilder(string.Empty);

            output.AppendFormat("Providers: {0}{1}", Providers, Environment.NewLine);
            output.AppendFormat("Providers with events: {0}{1}", ProvidersWithEvents, Environment.NewLine);
            output.AppendFormat("Events: {0}", Events);

            return output.ToString();
        }
    }
}
