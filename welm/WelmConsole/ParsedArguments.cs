using DocoptNet;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WelmConsole
{
    public class ParsedArguments
    {
        /// <summary>
        /// Raw program arguments.
        /// </summary>
        private readonly IDictionary<string, ValueObject> _args;

        /// <summary>
        /// True if the program arguments contain the providers option, otherwise false.
        /// </summary>
        public bool Providers => _args.ContainsKey("--providers") && (bool.Parse(_args["--providers"].ToString()));

        /// <summary>
        /// True if the program arguments contain the logs option, otherwise false.
        /// </summary>
        public bool Logs => _args.ContainsKey("--logs") && (bool.Parse(_args["--logs"].ToString()));

        /// <summary>
        /// True if the program arguments contain the events option, otherwise false.
        /// </summary>
        public bool Events => _args.ContainsKey("--events") && (bool.Parse(_args["--events"].ToString()));

        /// <summary>
        /// The output format.
        /// </summary>
        public string Format { get; }

        public ParsedArguments(IDictionary<string, ValueObject> arguments)
        {
            if (arguments == null || arguments.Count == 0)
            {
                throw new DocoptExitException("No arguments specified");
            }

            _args = arguments;

            if (!_args.ContainsKey("--format"))
            {
                throw new DocoptExitException("No format specified");
            }

            string format = _args["--format"].ToString().ToLower(CultureInfo.CurrentCulture);

            if (!(new [] {"csv", "json", "txt", "all"}).Contains(format))
            {
                throw new DocoptExitException("Invalid format of " + format);
            }

            Format = format;
        }
    }
}
