using NLog;
using System.Collections.Generic;
using System.Globalization;

namespace WelmLibrary.Classic
{
    /// <summary>
    /// Maintains a cache of loaded EventMessageFiles to prevent loading a message file multiple times when it is used across many different event sources.
    /// </summary>
    public sealed class EventMessageFileCache
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private IDictionary<string, EventMessageFile> _messageFiles;

        static EventMessageFileCache() { Instance = new EventMessageFileCache(); }

        private EventMessageFileCache()
        {
            _messageFiles = new Dictionary<string, EventMessageFile>();
        }

        /// <summary>
        /// Adds an event message file to the cache.
        /// </summary>
        /// <param name="messageFile">An event message file.</param>
        public void Add(EventMessageFile messageFile)
        {
            if (messageFile != null)
            {
                string path = messageFile.Path.ToLower(CultureInfo.CurrentCulture);

                if (!string.IsNullOrEmpty(path))
                {
                    if (!_messageFiles.ContainsKey(path))
                    {
                        _messageFiles.Add(path, messageFile);
                    }
                    else
                    {
                        Logger.Info(CultureInfo.CurrentCulture, "Did not add message file '{0}' for '{1}' due to it being a duplicate", path, messageFile.FileName);
                    }
                }
                else
                {
                    Logger.Info(CultureInfo.CurrentCulture, "Did not add message file for '{0}' due to an empty path", messageFile.FileName);
                }
            }
        }

        /// <summary>
        /// Gets an event message file based on its path.
        /// </summary>
        /// <param name="path">The file system path location of the event message file.</param>
        /// <returns>The event message file at the specified location or NULL if it is not in the cache.</returns>
        public EventMessageFile Get(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if (_messageFiles.ContainsKey(path.ToLower(CultureInfo.CurrentCulture)))
                {
                    return _messageFiles[path.ToLower(CultureInfo.CurrentCulture)];
                }
            }

            return null;
        }

        /// <summary>
        /// Tests whether the event message file path is in the cache.
        /// </summary>
        /// <param name="path">The event message file path.</param>
        /// <returns>True if the path is in the cache otherwise false.</returns>
        public bool Contains(string path)
        {
            bool found = false;

            if (!string.IsNullOrEmpty(path))
            {
                found = _messageFiles.ContainsKey(path.ToLower(CultureInfo.CurrentCulture));
            }

            return found;
        }

        /// <summary>
        /// Gets the event message files.
        /// </summary>
        /// <returns>The event message files.</returns>
        public IDictionary<string, EventMessageFile> MessageFiles()
        {
            return _messageFiles;
        }

        /// <summary>
        /// The event message file cache instance.
        /// </summary>
        public static EventMessageFileCache Instance { get; private set; }
    }
}
