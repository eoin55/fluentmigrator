using System;
using FluentMigrator.Runner.Generators.Generic;

namespace FluentMigrator.Runner.Generators.Informix
{
    public class InformixQuoter : GenericQuoter
    {
        public readonly char[] SpecialChars = "\"%'()*+|,{}-./:;<=>?^[]".ToCharArray();

        public override string FormatDateTime(DateTime value)
        {
            return ValueQuote + value.ToString("yyyy-MM-dd HH:mm:ss.fff") + ValueQuote;
        }

        public override string Quote(string name)
        {
            // Quotes are only included if the name contains a special character, in order to preserve case insensitivity where possible.
            return name.IndexOfAny(SpecialChars) != -1 ? base.Quote(name) : name;
        }
    }
}