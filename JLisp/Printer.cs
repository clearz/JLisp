using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JLisp.Parsing.Types;

namespace JLisp
{
    public class Printer
    {
        public static string Join(List<JlValue> value, string delim, bool printReadably) =>
            String.Join(delim, value.Select(v => v.ToString(printReadably)));

        public static string Join(Dictionary<string,JlValue> value, string delim, bool printReadably)
        {
            var strs = new List<string>();
            foreach (var kv in value)
            {
                if(kv.Key.Length > 0 && kv.Key[0] == '\u029e')
                    strs.Add($":{kv.Key.Substring(1)}");
                else if (printReadably)
                    strs.Add($"\"{kv.Key}\"");
                else 
                    strs.Add(kv.Key);

                strs.Add(kv.Value.ToString(printReadably));
            }
            return String.Join(delim, strs);
        }

        public static string PrintStr(JlValue jv, bool printReadably) => jv.ToString(printReadably);

        public static string PrintStrArgs(JlList jv, String sep, bool printReadably) =>
            Join( jv.Value, sep, printReadably );

        public static string EscapeString(string str) => Regex.Escape(str);

    }
}
