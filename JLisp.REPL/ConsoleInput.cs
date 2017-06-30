using System;
using System.Collections.Generic;
using System.Text;
using JLisp.REPL;

namespace JLisp.Parsing
{
    
    public abstract class InputReader
    {
        internal const string PROMPT = "-> ";
        public static InputReader Raw { get; }= new ConsoleReader();
        public static InputReader Terminal { get; } = new CustomReader();

        public abstract string Readline();

        private class CustomReader : InputReader
        {
            private static LineEditor _lineedit;
            public CustomReader() {
                _lineedit =  new LineEditor( "JlParser" );
            }
            public override string Readline() => _lineedit.Edit(PROMPT, "");
            
        }

        private class ConsoleReader : InputReader
        {
            public override string Readline() {

                Console.Write(PROMPT);
                return Console.ReadLine();
            }
        }
    }
}
