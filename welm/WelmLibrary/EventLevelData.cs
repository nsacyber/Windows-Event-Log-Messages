using System;
using System.Globalization;

namespace WelmLibrary
{
    /// <summary>
    /// See the System.Diagnostics.Eventing.Reader.StandardEventLevel enumeration for well-known event levels.
    /// </summary>
    public class EventLevelData: IEquatable<EventLevelData>
    {
        /// <summary>
        /// The event level name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The event level value.
        /// </summary>
        public int Value { get; }

        public EventLevelData()
        {
            Name = string.Empty;
            Value = 0;
        }

        /// <summary>
        /// Create an event level based on the specified input.
        /// </summary>
        /// <param name="name">The event level name.</param>
        /// <param name="value">The event level value.</param>
        public EventLevelData(string name, int value)
        {
            Name = name ?? string.Empty;
            Value = value;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "Name: {0}, Value: {1}", Name, Value);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as EventLevelData);
        }

        public bool Equals(EventLevelData other)
        {
            bool equals = false;

            if (other != null)
            {
                if (Name != null && other.Name != null && Name.Equals(other.Name))
                {
                    equals = true;
                }
            }

            return equals;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
