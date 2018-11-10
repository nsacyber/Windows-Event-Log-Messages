using NLog;
using System;
using System.Globalization;

namespace WelmLibrary
{
    /// <summary>
    /// Holds data for an event ID. Event IDs are 32-bit values. Event ID values are NTSTATUSes. The value that corresponds to the Event ID in Event Viewer is the lower 16 bits value.
    /// See the MS-ERREF open specification for the definition of NTSTATUS.
    /// <see cref="http://msdn.microsoft.com/en-us/library/cc231196.aspx"/> //MS-ERREF
    /// <see cref="http://msdn.microsoft.com/en-us/library/aa363651.aspx"/> //Event Identifiers
    /// <see cref="http://msdn.microsoft.com/en-us/library/cc231200.aspx"/> //NSTATUS in MS-ERREF
    /// </summary>
    public class EventId: IEquatable<EventId>
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Severity of the event. Not to be confused with the Level column in the Event Log Viewer. Severity of the NTSTATUS.
        /// </summary>
        public string Severity { get; }

        /// <summary>
        /// Specifies if the value is customer or Microsoft defined. It is true for customer values and false for Microsoft values.
        /// </summary>
        public bool IsCustomer { get; }

        /// <summary>
        /// Must be false for Microsoft values.
        /// </summary>
        public bool IsReserved { get; }

        /// <summary>
        /// Indicates the system service responsible for the error.
        /// </summary>
        public string Facility { get; }

        /// <summary>
        /// The last 4 bytes of the Value which is what is corresponds to value in the Event ID column in the Event Log Viewer.
        /// </summary>
        public ushort Code { get; }

        /// <summary>
        /// The original value.
        /// </summary>
        public long Value { get; }

        /// <summary>
        /// The event ID interpreted as an NTSTATUS.
        /// </summary>
        private NtStatus NtStatus { get; }

        public EventId()
        {
            Severity = string.Empty;
            IsCustomer = false;
            IsReserved = false;
            Facility = string.Empty;
            Code = 0;
            Value = 0;
        }

        /// <summary>
        /// Creates an event ID based on the specified value.
        /// </summary>
        /// <param name="value">The value for the event ID.</param>
        public EventId(long value)
        {
            if (value >= 0)
            {
                NtStatus = new NtStatus(value);

                Severity = NtStatus.Severity.ToString(); 
                IsReserved = NtStatus.IsReserved; // should be false for NTSTATUS values, which is what an event ID is. 
                IsCustomer = NtStatus.IsCustomer; // should always be false for Microsoft events
 
                // some values for NtStatus.Facility do not map to the enum
                // for that case the EventId.Facility will be null
                Facility = Enum.GetName(typeof (NtFacility), NtStatus.Facility) ?? string.Empty;

                if (string.IsNullOrEmpty(Facility))
                {
                    // this could made OS version aware since the maximum changes per major OS release
                    if (NtStatus.Facility <= ((uint)NtFacility.Maximum))
                    {
                        Logger.Warn(CultureInfo.CurrentCulture, "Undocumented facility: {0} (0x{1:X}) ", NtStatus.Facility, NtStatus.Facility);
                    }
                    else
                    {
                        //TODO: consider throwing an exception so it can be handle in parent context so this event is NOT added to list of valid events, not sure if this is reliable
                        // if the value is greater than the max then this probably isn't a valid event ID
                        Logger.Warn(CultureInfo.CurrentCulture, "Invalid facility: {0} (0x{1:X}) ", NtStatus.Facility, NtStatus.Facility);
                    }
                }

                Code = NtStatus.Code;
                Value = value;
            }
            else
            {
                // not sure what to do when the value is less than 0 and haven't encountered this yet for Microsoft event IDs
                Logger.Warn(CultureInfo.CurrentCulture, "Invalid event ID value {0} (0x{1:X})", value, value);
            }
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "Code: {0},  Value: {1}, Severity: {2}, Customer: {3}, Reserved: {4}, Facility: {5}", Code, Value, Severity, IsCustomer, IsReserved, Facility);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as EventId);
        }

        public bool Equals(EventId other)
        {
            bool equal = false;

            if (other != null)
            {
                if (Value == other.Value)
                {
                    equal = true;
                }
            }

            return equal;
        }

        public override int GetHashCode()
        {
            // probably just needs to be Value.GetHashCode();
            return Value.GetHashCode() + Code.GetHashCode();
        }
    }
}
