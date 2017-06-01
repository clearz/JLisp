using System;
using System.Collections.Generic;
using System.Text;

namespace JLisp.Parsing
{
    public sealed class ParserError : Exception
    {
        private static Dictionary<ErrorCode, string> ErrorStrings { get; } = new Dictionary<ErrorCode, string>()
        {
            {ErrorCode.InvalidToken, $"Invalid token \"$IDENT$\" found at position $POS$"},
            {ErrorCode.MismatchedParens, "Mismatched Parens \"$IDENT$\" found at position $POS$"},
            {ErrorCode.NoClosingParen, "No Closing Bracket found at position $POS$"},
            {ErrorCode.UnexpectedToken, ""},
            {ErrorCode.UnexpectedType, ""},
            {ErrorCode.ParseExpectingToken, "Expecting Token of type '$TYPE1$' but found token '$TYPE2$' at position $POS$"},
        };

        public static ParserError Throw(ErrorCode errorCode, string identity = "", string hint = "", Enum type1 = null, Enum type2 = null, int index = -1, int position = -1, string message = "")
        {
            throw new ParserError(errorCode, identity, hint, type1, type2, index, position, message);
        }

        private ParserError(ErrorCode errorCode, string identity, string hint, Enum type1, Enum type2, int index, int position, string message)
        {
            ErrorCode = errorCode; Identity = identity; Hint = hint; Type1 = type1; Type2 = type2; Index = index; Pos = position; Msg = message;
        }
        public ErrorCode ErrorCode { get; }
        public string Identity { get;}
        public string Hint { get; }
        public Enum Type1 { get; }
        public Enum Type2 { get;  }
        public int Index { get;}
        public int Pos { get; }
        public string Msg { get; }

        
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(' ', Pos + 3);
            sb.Append("^\n");
            var str = ErrorStrings[ErrorCode];
            str = str.Replace("$IDENT$", Identity);
            str = str.Replace("$HINT$", Hint.ToString());
            str = str.Replace("$TYPE1$", Type1?.ToString());
            str = str.Replace("$TYPE2$", Type2?.ToString());
            str = str.Replace("$INDEX$", Index.ToString());
            str = str.Replace("$POS$", Pos.ToString());
            str = str.Replace("$IDENT$", Identity);
            sb.Append(str).Append(", ErrorCode: ").Append((int)ErrorCode);
            return sb.ToString();
        }
    }

    public enum ErrorCode
    {
        InvalidToken = 1201,
        UnexpectedToken,
        UnexpectedType,
        MismatchedParens,
        NoClosingParen,
        ParseExpectingToken
    }
}
