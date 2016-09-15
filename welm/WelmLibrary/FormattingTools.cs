namespace WelmLibrary
{
    public static class FormattingTools
    {
        /// <summary>
        /// Replaces special characters in a string so lines will not break across multiple lines for plaintext output.
        /// </summary>
        /// <param name="message">The message to format.</param>
        /// <returns>The formmatted message.</returns>
        public static string FormatMessageForPlaintext(string message)
        {
            if (message == null)
            {
                message = string.Empty;
            }

            if (!string.IsNullOrEmpty(message))
            {
                message = message.Replace("\r", " ");
                message = message.Replace("\n", " ");
                message = message.Replace("\t", " ");
            }

            return message;
        }
    }
}
