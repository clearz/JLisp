using System;

namespace JLisp
{
    
    public abstract class InputReader
    {
        internal const string PROMPT = "-> ";
        public static InputReader Raw { get; }= new ConsoleReader();
        public static InputReader Custom { get; } = new CustomReader();

        public abstract string Readline();

        public virtual string Readline(string prompt)
        {
            throw new NotImplementedException();
        }

        private class CustomReader : InputReader
        {
            private static LineEditor _lineedit;
            public CustomReader() {
                _lineedit =  new LineEditor( "JlParser" );
            }
            public override string Readline() => _lineedit.Edit(PROMPT, "");
            public override string Readline(string prompt) => _lineedit.Edit(prompt, "");
        }

        private class ConsoleReader : InputReader
        {
            public override string Readline() {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.Write(PROMPT);
                Console.ForegroundColor = ConsoleColor.White;
                var inStr =  Console.ReadLine();
                Console.ForegroundColor = ConsoleColor.Green;
                return inStr;
            }
        }
    }
}
