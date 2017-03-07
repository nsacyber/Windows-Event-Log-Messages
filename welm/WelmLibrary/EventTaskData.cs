using System;
using System.Globalization;

namespace WelmLibrary
{
    /// <summary>
    /// See the System.Diagnostics.Eventing.Reader.StandardEventTask enumeration for well-known event tasks.
    /// </summary>
    public class EventTaskData : IEquatable<EventTaskData>
    {
        /// <summary>
        /// The event task name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The event task message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The event task value.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// The event task GUID.
        /// </summary>
        public Guid Guid { get; }

        public EventTaskData()
        {
            Name = string.Empty;
            Message = string.Empty;
            Value = 0;
            Guid = Guid.Empty;
        }

        /// <summary>
        /// Create an event task based on the specified information.
        /// </summary>
        /// <param name="name">The task name.</param>
        /// <param name="message">The task message.</param>
        /// <param name="value">The task value.</param>
        /// <param name="guid">The task GUID.</param>
        public EventTaskData(string name, string message, int value, Guid guid)
        {
            if (!string.IsNullOrEmpty(name))
            {
                Name = name.Contains(":") ? name.Split(':')[1] : name;
            }
            else
            {
                Name = string.Empty;
            }

            Message = !string.IsNullOrEmpty(message) ? message : string.Empty;

            /**
            if(!string.IsNullOrEmpty(message) && !message.Equals(name))
                Name = message;
            else if(!string.IsNullOrEmpty(name))
                Name = name.Contains(":") ? name.Split(new char[] {':'})[1] : name;
            else
                Name = string.Empty;
            **/

            Value = value;
            Guid = guid;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as EventTaskData);
        }

        public bool Equals(EventTaskData other)
        {
            bool equal = false;

            if (other != null)
            {
                if (Name != null && other.Name != null && Name.Equals(other.Name))
                {
                    if (Value == other.Value)
                    {
                        if (Guid.Equals(other.Guid))
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
            return Name.GetHashCode() + Value + Guid.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "Name: {0},  Value: {1}", Name, Value);
        }
    }
}
