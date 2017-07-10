using JLisp.Parsing;
using JLisp.Parsing.Types;

namespace JLisp.Tests.Repls
{
    public class Repl1 : IProcess
    {
        // read
        static JlValue Read(string str)
        {
            return Reader.ReadStr(str);
        }

        // eval
        static JlValue Eval(JlValue ast, string env)
        {
            return ast;
        }

        // print
        static string Print(JlValue exp)
        {
            return Printer.PrintStr(exp, true);
        }

        public void Init() { }
        // repl
        public string Process(string line) => Print(Eval(Read(line), ""));
    }
}
