using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using JLisp.Parsing.Types;

namespace JLisp.Parsing
{
    class Reader
    {
        private readonly List<string> _tokens;
        private int _position;

        public Reader(List<string> tokens)
        {
            _tokens = tokens;
            _position = 0;
        }

        public string Peek()
        {
            if (_position >= _tokens.Count) return null;
            else return _tokens[_position];
        }

        public string Next() => _tokens[_position++];

        public static List<string> Tokenize(string str)
        {
            var tokens = new List<string>();
            const string pattern = @"[zs ,]*(~@|[\[\]{}()'`~@]|""(?:[\\].|[^\\""])*""|;.*|[^\s \[\]{}()'""`~@,;]*)";
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

        public static JlValue ReadAtom(Reader rdr)
        {
            var token = rdr.Next();
            const string pattern = @"(^-?[0-9]+$)|(^-?[0-9][0-9.]*$)|(^nil$)|(^true$)|(^false$)|^("".*"")$|(^[^""]*$)";
            var regex = new Regex(pattern);
            Match match = regex.Match(token);
            //Console.WriteLine($"token: ^{token}$");
            if (!match.Success)
                throw new ParseError($"unrecognized token '{token}'");

            if(match.Groups[1].Value != String.Empty)
                return new JlInteger(int.Parse(match.Groups[1].Value));
            else if (match.Groups[3].Value != String.Empty)
                return JlConstant.Nil;
            else if (match.Groups[4].Value != String.Empty)
                return JlConstant.True;
            else if (match.Groups[5].Value != String.Empty)
                return JlConstant.False;
            else if ( match.Groups[6].Value != String.Empty ) {
                var str = match.Groups[6].Value;
                str = str.Substring( 1, str.Length - 2 )
                         .Replace( "\\\"", "\"" )
                         .Replace( "\\n", "\n" );
                return new JlString(str);
            }
            else if (match.Groups[7].Value != String.Empty)
                return new JlSymbol(match.Groups[7].Value);
            else
                throw new ParseError($"unrecognized '{match.Groups[0]}'");
        }

        public static JlValue ReadList(Reader rdr, JlList lst, char start, char end)
        {
            var token = rdr.Next();
            if (token[0] != start)
                throw new ParseError($"expected '{start}'");

            while((token = rdr.Peek()) != null && token[0] != end)
                lst.ConjBANG(ReadForm(rdr));

            if (token == null)
                throw new ParseError($"expected '{end}' get EOF");
            rdr.Next();

            return lst;
        }
        public static JlValue ReadForm(Reader rdr)
        {
            string token = rdr.Peek();
            if(token == null) throw new JlContinue();

            JlValue form = null;
            switch (token[0])
            {
                case '(': form = ReadList(rdr, new JlList(), '(', ')'); break;
                case ')': throw new ParseError("unexpected ')'");
                case '[': form = ReadList(rdr, new JlVector(), '[', ']'); break;
                case ']': throw new ParseError("unexpected ']'");
                case '{': form = ReadHashMap(rdr); break;
                case '}': throw new ParseError("unexpected '}'");
                default:  form = ReadAtom(rdr); break;
            }

            return form;
        }

        public static JlValue ReadStr(string str)
        {
            return ReadForm(new Reader(Tokenize(str)));
        }

        public static JlValue ReadHashMap(Reader rdr)
        {
            JlList lst = (JlList)ReadList(rdr, new JlList(), '{', '}');
            return new JlHashMap(lst);
        }

    }
}
