using System;
using System.Collections.Generic;
using System.Text;

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

    public class JlError : JlThrowable
    {
        public JlError(string msg) : base(msg) { }
    }

    public class JlContinue : JlThrowable { }
}
