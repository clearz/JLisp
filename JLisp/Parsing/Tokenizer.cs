using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace JLisp.Parsing
{
    sealed class Tokinizer
    {
        private static Tokinizer _tokinizer;
        private static readonly Regex _errorCheck = new Regex(@"[^0-9(){}\-*\+/\s\%]+");
        private readonly IDictionary<TokenType, Regex> _tokenRegexes = new Dictionary<TokenType, Regex>()
        {
            {TokenType.Operator, new Regex(@"^(\+|\-|\*|\/|\%)$", RegexOptions.Compiled)},
            {TokenType.Number, new Regex(@"^\-?\d+?$", RegexOptions.Compiled)},
            {TokenType.Character, new Regex(@"^.$", RegexOptions.Compiled)}
        };

        private Tokinizer() { }

        public static Tokinizer GetInstance()
        {
            return _tokinizer ?? (_tokinizer = new Tokinizer());
        }

        public IEnumerable<Token> Tokenize(string exp)
        {
            PreCheck(exp);
            exp = exp.TrimEnd();
            var len = exp.Length;
            var prevTokenMatch = TokenType.None;
            yield return new Token("(", TokenType.Character, 0);

            for (int i = 0, j = len; i < j; j--)
            {
                while(Char.IsWhiteSpace(exp[i])) i++;
                var subExp = exp.Substring(i, j - i);
                foreach (var pair in _tokenRegexes)
                {
                    TokenType tokenType = pair.Key;
                    Regex regex = _tokenRegexes[tokenType];
                    var match = regex.Match(subExp);
                    if (match.Success)
                    {
                        try
                        {
                            yield return new Token(subExp, tokenType, i+1);
                        }
                        finally
                        {
                            prevTokenMatch = tokenType;
                            i += subExp.Length;
                            j = len + 1;
                        }
                        break;
                    }
                }
            }
            yield return new Token(")", TokenType.Character, exp.Length+1);
            yield return new Token(string.Empty, TokenType.EOF, exp.Length+2);
        }

        private void PreCheck(string exp)
        {
            CheckBalanced(exp);
            var len = exp.Length;
            var m = _errorCheck.Match(exp);
            if (m.Success)
                ParserError.Throw(ErrorCode.InvalidToken, identity: m.Value, position: m.Index);
        }

        private static void CheckBalanced(string input)
        {
            Stack<KeyValuePair<char, int>> st = new Stack<KeyValuePair<char, int>>();

            for (int i = 0; i < input.Length; i++)
            {
                var variable = input[i];
                switch (variable)
                {
                    case '[':
                    case '{':
                    case '(':
                        st.Push(new KeyValuePair<char, int>(variable, i));
                        break;
                    case ']':
                    case '}':
                    case ')':
                        if (st.Count == 0 || !CheckMatching(st.Peek().Key, variable))
                        {
                            int p = (st.Any() ? st.Peek().Value : 0) + 1;
                            ParserError.Throw(ErrorCode.MismatchedParens, identity: input.Substring(i, 1), position: i);
                        }
                        if (st.Any()) // (1)(
                            st.Pop();
                        break;
                }
            }

            if (st.Count > 0)
                ParserError.Throw(ErrorCode.NoClosingParen, position: st.Peek().Value);
            
        }

        private static bool CheckMatching(char a, char b)
        {
            return a == '[' && b == ']' || a == '{' && b == '}' || a == '(' && b == ')';
        }
    }
    public class Token
    {
        public string Identity { get; }
        public TokenType TokenType { get; set; }
        public int Position { get; }

        public Token(string identity, TokenType tokenType, int position)
        {
            Identity = identity;
            TokenType = tokenType;
            Position = position;
        }
        public static bool operator ==(Token t, TokenType tt)
        {
            if (ReferenceEquals(t, null)) return false;
            return tt == t.TokenType;
        }

        public static bool operator !=(Token t, TokenType tt)
        {
            return !(t == tt);
        }

        public override string ToString() => $"TokenType = {TokenType}, Identity = \"{Identity}\", Position = {Position}";
    }
    public enum TokenType
    {
        None, Number, Operator,
        Whitespace, EOF,
        Character
    }
}
