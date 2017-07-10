using System;

namespace JLisp.Parsing.Types
{
    public class JlThrowable : Exception
    {
        public JlThrowable() { }
        public JlThrowable(string msg): base(msg) { }
    }

    public class ParseError : JlThrowable {
        public ParseError(string msg) : base(msg) { }
    }

    public class JlError : JlThrowable {
        public JlError(string msg) : base(msg) { }
    }

    public class JlContinue : JlThrowable { }

    public class JlException : JlThrowable
    {
        public JlValue Value { get; }

        public JlException(JlValue value) { this.Value = value; }
        public JlException(string value) : this( new JlString( value )) { }

    }
}
