using DocoptNet;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WelmConsole
{
    public class ParsedArguments
    {
        private readonly IDictionary<string, ValueObject> _args;

        public bool Providers => _args.ContainsKey("--providers") && (bool.Parse(_args["--providers"].ToString()));

        public bool Logs => _args.ContainsKey("--logs") && (bool.Parse(_args["--logs"].ToString()));

        public bool Events => _args.ContainsKey("--events") && (bool.Parse(_args["--events"].ToString()));

        public string Format { get; private set; }

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
