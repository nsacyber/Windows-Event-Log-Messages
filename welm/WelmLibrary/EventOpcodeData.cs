using System;
using System.Globalization;

namespace WelmLibrary
{
    /// <summary>
    /// See the System.Diagnostics.Eventing.Reader.StandardEventOpcode enumeration for well-known event opcodes.
    /// </summary>
    public class EventOpcodeData : IEquatable<EventOpcodeData>
    {
        /// <summary>
        /// The opcode name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The opcode value.
        /// </summary>
        public int Value { get; set; }

        public EventOpcodeData()
        {
            Name = string.Empty;
            Value = 0;
        }

        /// <summary>
        /// Create an event opcode based on the specified name and value data.
        /// </summary>
        /// <param name="name">The opcode name.</param>
        /// <param name="value">The opcode value.</param>
        public EventOpcodeData (string name, int value)
        {
            Name = name ?? string.Empty;
            Value = value;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as EventOpcodeData);
        }

        public bool Equals(EventOpcodeData other)
        {
            bool equal = false;

            if(other != null)
            {
                if (Name != null && other.Name != null && Name.Equals(other.Name))
                {
                    if (Value == other.Value)
                    {
                        equal = true;
                    }
                }
            }

            return equal;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() + Value;
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "Name: {0},  Value: {1}", Name, Value);
        }
    }
}
