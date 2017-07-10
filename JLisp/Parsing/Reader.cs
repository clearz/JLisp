using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using JLisp.Parsing.Types;

namespace JLisp.Parsing
{
    public class Reader
    {
        private readonly List<string> _tokens;
        private int _position;
        private static JlValue _prev = null;

        private Reader(List<string> tokens)
        {
            _tokens = tokens;
            _position = 0;
        }

        private string Peek()
        {
            if (_position >= _tokens.Count) return null;
            return _tokens[_position];
        }

        private string Next() => _tokens[_position++];

        private static List<string> Tokenize(string str)
        {
            var tokens = new List<string>();
            string pattern = @"[\s ,]*(~@|[\[\]{}()'`~@]|""(?:[\\].|[^\\""])*""|;.*|[^\s \[\]{}()'""`~@,;]*)";
            var regex = new Regex(pattern);
            foreach (Match match in regex.Matches(str))
            {
                string token = match.Groups[1].Value;
                if (!string.IsNullOrEmpty(token) && token[0] != ';')
                {
                    tokens.Add(token);
                }
            }
            return tokens;
        }

        private static JlValue ReadAtom(Reader rdr)
        {
            var token = rdr.Next();
            const string pattern = @"(^-?[0-9]+$)|(^-?[0-9][0-9.]*$)|(^nil$)|(^true$)|(^false$)|^("".*"")$|:(.*)|(^[^""]*$)";
            var regex = new Regex(pattern);
            Match match = regex.Match(token);

            if (!match.Success)
                throw new ParseError($"unrecognized token '{token}'");

            if(match.Groups[1].Value != String.Empty)
                return int.Parse(match.Groups[1].Value);
            if (match.Groups[3].Value != String.Empty)
                return JlConstant.Nil;
            if (match.Groups[4].Value != String.Empty)
                return true;
            if (match.Groups[5].Value != String.Empty)
                return false;
            if ( match.Groups[6].Value != String.Empty ) {
                var str = match.Groups[6].Value;
                str = str.Substring( 1, str.Length - 2 )
                    .Replace( "\\\"", "\"")
                    .Replace("\\n", "\n")
                    .Replace("\\\\", "\\");
                return str;
            }
            if (match.Groups[7].Value != String.Empty)
                return $"\u029e{match.Groups[7].Value}";
            if (match.Groups[8].Value != String.Empty)
                return new JlSymbol(match.Groups[8].Value);
            throw new ParseError($"unrecognized '{match.Groups[0]}'");
        }

        private static JlList ReadList(Reader rdr, JlList lst, char end)
        {
            var token = rdr.Next();
            while((token = rdr.Peek()) != null && token[0] != end)
                lst.ConjBang(ReadForm(rdr));

            if (token == null)
                throw new ParseError($"expected '{end}' get EOF");
            rdr.Next();

            return lst;
        }

        private static JlValue ReadForm(Reader rdr)
        {
            string token = rdr.Peek();
            if (token == null)
                throw new JlContinue();

            JlValue form = null;
            switch (token)
            {
                case "'":
                    rdr.Next();
                    return new JlList((JlSymbol)"quote", ReadForm(rdr));
                case "`":
                    rdr.Next();
                    return new JlList((JlSymbol)"quasiquote", ReadForm(rdr));
                case "~":
                    rdr.Next();
                    return new JlList((JlSymbol)"unquote", ReadForm(rdr));
                case "~@":
                    rdr.Next();
                    return new JlList((JlSymbol)"splice-unquote", ReadForm(rdr));
                case "^":
                    rdr.Next();
                    var meta = ReadForm(rdr);
                    return new JlList((JlSymbol)"with-meta", ReadForm(rdr), meta);
                case "@":
                    rdr.Next();
                    return new JlList((JlSymbol)"deref", ReadForm(rdr));
                case "(": form = ReadList(rdr, new JlList(), ')'); break;
                case ")": throw new ParseError("unexpected ')'");
                case "[": form = ReadList(rdr, new JlVector(), ']'); break;
                case "]": throw new ParseError("unexpected ']'");
                case "{": form = ReadHashMap(rdr); break;
                case "}": throw new ParseError("unexpected '}'");
                default:  form = ReadAtom(rdr); break;
            }
            _prev = form;
            Debug.Assert(form != null);
            return form;
        }

        public static JlValue ReadStr(string str)
        {
            var t = Tokenize(str);
            var r = new Reader(t);
            var f =  ReadForm(r);
            return f;
        }

        private static JlValue ReadHashMap(Reader rdr)
        {
            var lst = ReadList(rdr, new JlList(), '}');
            return new JlHashMap(lst);
        }

    }
}
