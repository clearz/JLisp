using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JLisp.Parsing.Types;

namespace JLisp
{
    class Printer
    {
        public static string Join(List<JlValue> value, string delim, bool printReadably) =>
            String.Join(delim, value.Select(v => v.ToString(printReadably)));

        public static string Join(Dictionary<string,JlValue> value, string delim, bool printReadably)
        {
            var strs =
                Interleave(value.Keys.Select(v => printReadably ? $"\"{v.ToString()}\"" : v.ToString()),
                    value.Values.Select(v => v.ToString(printReadably)));
            return String.Join(delim, strs);
        }

        public static string PrintStr(JlValue jv, bool printReadably) => jv.ToString(printReadably);

        public static string PrintStrArgs(JlList jv, String sep, bool printReadably) =>
            Join( jv.Value, sep, printReadably );
        public static string EscapeString(string str) => Regex.Escape(str);
        internal static IEnumerable<T> Interleave<T>(IEnumerable<T> first, IEnumerable<T> second)
        {
            using (IEnumerator<T>
                enumerator1 = first.GetEnumerator(),
                enumerator2 = second.GetEnumerator())
            {
                while (enumerator1.MoveNext() && enumerator2.MoveNext())
                {
                    yield return enumerator1.Current;
                    yield return enumerator2.Current;
                }
            }
        }
    }
}
