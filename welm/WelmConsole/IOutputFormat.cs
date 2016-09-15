namespace WelmConsole
{
    /// <summary>
    /// Defines common methods for output formats.
    /// </summary>
    public interface IOutputFormat
    {
        /// <summary>
        /// Creates formatted text.
        /// </summary>
        /// <returns>Formatted text.</returns>
        string ToText();

        string ToCsv();

        string ToJson();

        string ToFormat(object data, string format);
    }

    /// <summary>
    /// The different output formats.
    /// </summary>
    public enum OutputFormat
    {
        /// <summary>
        /// No or unknown format.
        /// </summary>
        None,

        /// <summary>
        /// Minimal plain text format.
        /// </summary>
        Txt,

        /// <summary>
        /// Comma Separated Values format.
        /// </summary>
        Csv,

        /// <summary>
        /// JavaScript Object Notation format.
        /// </summary>
        Json,

        /// <summary>
        /// All format.
        /// </summary>
        All
    }
}
