using System.Globalization;
using System.Text;

namespace WelmLibrary
{
    /// <summary>
    /// Wraps the "TypesSupported" registry bitmask value and exposes the relevant masked attributes. These values correspond to various event levels.
    /// </summary>
    public class EventTypeBitMask
    {
        /// <summary>
        /// The raw bitmask value.
        /// </summary>
        public long Bitmask { get; }

        /// <summary>
        /// Specifies whether the bitmask contain a Success event level.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Specifies whether the bitmask contain an Error event level.
        /// </summary>
        public bool IsError { get; }

        /// <summary>
        /// Specifies whether the bitmask contain a Warning event level.
        /// </summary>
        public bool IsWarning { get; }

        /// <summary>
        /// Specifies whether the bitmask contain an Information event level.
        /// </summary>
        public bool IsInformation { get; }

        /// <summary>
        /// Specifies whether the bitmask contain an Audit Success event level.
        /// </summary>
        public bool IsSuccessAudit { get; }

        /// <summary>
        /// Specifies whether the bitmask contain an Audit Failure event level.
        /// </summary>
        public bool IsFailAudit { get; }

        /// <summary>
        /// Creates a new bitmask based on the passed in raw value.
        /// </summary>
        /// <param name="value">The raw bitmask value.</param>
        public EventTypeBitMask(long value)
        {
            Bitmask = value;
            IsSuccess = (Bitmask == 0);
            IsError = (Bitmask & 0x00000001) == 1;
            IsWarning = (Bitmask & 0x00000002) == 2;
            IsInformation = (Bitmask & 0x00000004) == 4;
            IsSuccessAudit = (Bitmask & 0x00000008) == 8;
            IsFailAudit = (Bitmask & 0x00000010) == 16;
        }

        public override string ToString()
        {
            StringBuilder output = new StringBuilder(string.Empty);

            if (Bitmask != long.MaxValue)
            {
                if (IsSuccess)
                {
                    output.Append("Success, ");
                }

                if (IsError)
                {
                    output.Append("Error, ");
                }

                if (IsWarning)
                {
                    output.Append("Warning, ");
                }

                if (IsInformation)
                {
                    output.Append("Information, ");
                }

                if (IsSuccessAudit)
                {
                    output.Append("Success Audit, ");
                }

                if (IsFailAudit)
                {
                    output.Append("Failure Audit, ");
                }
            }

            string s = output.ToString().Trim();

            if (s.EndsWith(",", true, CultureInfo.CurrentCulture))
            {
                s = s.TrimEnd(new [] { ',' });
            }

            return s;
        }
    }
}
