using DocoptNet;
using System;
using System.Collections.Generic;

namespace WelmConsole
{
    public class ParsedArguments
    {
        private readonly IDictionary<string, ValueObject> _args = null;

        public bool Providers => _args.ContainsKey("--providers") && (bool.Parse(_args["--providers"].ToString()));

        public bool Logs => _args.ContainsKey("--logs") && (bool.Parse(_args["--logs"].ToString()));

        public bool Events => _args.ContainsKey("--events") && (bool.Parse(_args["--events"].ToString()));

        public OutputFormat Format
        {
            get
            {
                if (!_args.ContainsKey("--format"))
                {
                    throw new DocoptExitException("No format specified");
                }

                string rawFormat = _args["--format"].ToString();
                OutputFormat format;

                if (!Enum.TryParse<OutputFormat>(rawFormat, true, out format))
                {
                    throw new DocoptExitException("Invalid format of " + rawFormat);
                }

                return format;             
            }
        }

        public ParsedArguments(IDictionary<string, ValueObject> arguments)
        {
            _args = arguments;
        }
    }
}
