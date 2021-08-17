using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solnet.Anchor.CodeGen
{
    public static class Utilities
    {
        public static readonly string Lvl1Ident = "    ";

        public static readonly string Lvl2Ident = "        ";

        public static string FixName(string name)
        {
            var chars = name.ToArray();

            if (char.IsLower(chars[0]))
                chars[0] = char.ToUpper(chars[0]);
            return new string(chars);
        }
    }
}
