using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Linq;

namespace WelmLibrary
{
    /// <summary>
    /// Holds data associated with an event keyword. Event keywords are used to classify or group similar types of events. The values are typically used as bit masks.
    /// See the System.Diagnostics.Eventing.Reader.StandardEventKeywords enumeration for well-known event keywords.
    /// </summary>
    public class EventKeywordData: IEquatable<EventKeywordData>
    {
        /// <summary>
        /// The keyword name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The keyword value.
        /// </summary>
        public long Value { get; }

        /// <summary>
        /// Gets a list of keywords based on the specified metadata.
        /// </summary>
        /// <param name="keywords">The keyword metadata.</param>
        /// <returns>A list of keywords.</returns>
        public static IList<EventKeywordData> GetKeywords(IEnumerable<EventKeyword> keywords)
        {
            IList<EventKeywordData> keywordData = new List<EventKeywordData>();

            /**
             IList<EventKeywordData> keywords = new List<EventKeywordData>();
             
             foreach (EventKeywordData k in provider.Keywords.Select(keyword => new EventKeywordData(keyword.Name, keyword.DisplayName, keyword.Value)).Where(k => !keywords.Contains(k)))
                 keywords.Add(k);
             **/

            if (keywords != null)
            {
                // convert to a list so we can use Count property on the List instead of Count()/Any() method on the IEnumberable which would result in having to enumerate twice (including foreach)
                IList<EventKeyword> eventKeywords = keywords as IList<EventKeyword> ?? keywords.ToList();

                if (eventKeywords.Count > 0)
                {
                    foreach (EventKeyword keyword in eventKeywords)
                    {
                        keywordData.Add(new EventKeywordData(keyword.Name, keyword.DisplayName, keyword.Value));
                    }
                }
            }

            return keywordData;
        }

        /// <summary>
        /// Create an event keyword based on the specified data.
        /// </summary>
        /// <param name="name">The internal name.</param>
        /// <param name="displayName">The displayed name.</param>
        /// <param name="value">The keyword value.</param>
        public EventKeywordData(string name, string displayName, long value)
        {
            Name = string.Empty;

            // name has a value much more often than displayName but there are cases where both are null
            // we just want to have one common name for the keyword so use the name the doesn't have a colon or parse out the colon portion if necessary

            if (name != null && displayName != null && name.Contains(':'))
            {
                Name = displayName;
            }
            else if (name != null && displayName != null && !name.Contains(':'))
            {
                Name = name;
            }
            else if (name != null && displayName == null && !name.Contains(':'))
            {
                Name = name;
            }
            else if (name != null && displayName == null && name.Contains(':'))
            {
                Name = name.Split(':')[1];
            }

            Value = value;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "Name: {0}, Value: {1:X}", Name, Value);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() + Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as EventKeywordData);
        }

        public bool Equals(EventKeywordData other)
        {
            bool equal = false;

            if (other != null)
            {
                if (Name != null && other.Name != null & Name.Equals(other.Name))
                {
                    if (Value == other.Value)
                    {
                        equal = true;
                    }
                }
            }

            return equal;
        }
    }
}
