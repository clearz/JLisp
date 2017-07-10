using System;
using System.Collections.Generic;
using JLisp.Parsing;
using JLisp.Parsing.Types;

namespace JLisp.Tests.Repls
{
    class Repl2 : IProcess
    {
        // read
        static JlValue Read(string str)
        {
            return Reader.ReadStr(str);
        }

        // eval
        static JlValue eval_ast(JlValue ast, Dictionary<string, JlValue> env)
        {
            if (ast is JlSymbol)
            {
                JlSymbol sym = (JlSymbol)ast;
                return (JlValue)env[sym.Name];
            }
            else if (ast is JlList)
            {
                JlList oldLst = (JlList)ast;
                JlList newLst = ast.ListQ() ? new JlList()
                    : (JlList)new JlVector();
                foreach (JlValue mv in oldLst.Value)
                {
                    newLst.ConjBang(Eval(mv, env));
                }
                return newLst;
            }
            else if (ast is JlHashMap)
            {
                var newDict = new Dictionary<string, JlValue>();
                foreach (var entry in ((JlHashMap)ast).Value)
                {
                    newDict.Add(entry.Key, Eval((JlValue)entry.Value, env));
                }
                return new JlHashMap(newDict);
            }
            else
            {
                return ast;
            }
        }


        static JlValue Eval(JlValue origAst, Dictionary<string, JlValue> env)
        {
            JlValue a0;
            //Console.WriteLine("EVAL: " + Printer.PrintStr(orig_ast, true));
            if (!origAst.ListQ())
            {
                return eval_ast(origAst, env);
            }

            // Apply list
            JlList ast = (JlList)origAst;
            if (ast.Size == 0) { return ast; }
            a0 = ast[0];
            if (!(a0 is JlSymbol))
            {
                throw new JlError("attempt to Apply on non-symbol '"
                                             + Printer.PrintStr(a0, true) + "'");
            }
            var el = (JlList)eval_ast(ast, env);
            var f = (JlFunction)el[0];
            return f.Apply(el.Rest());

        }

        // print
        static string Print(JlValue exp)
        {
            return Printer.PrintStr(exp, true);
        }

        // repl
        public void Init()
        {
            throw new NotImplementedException();
        }

        public string Process(string line)
        {
            var replEnv = new Dictionary<string, JlValue> {
                {"+", new JlFunction(a => (JlInteger)a[0] + (JlInteger)a[1]) },
                {"-", new JlFunction(a => (JlInteger)a[0] - (JlInteger)a[1]) },
                {"*", new JlFunction(a => (JlInteger)a[0] * (JlInteger)a[1]) },
                {"/", new JlFunction(a => (JlInteger)a[0] / (JlInteger)a[1]) },
            };
            JlValue Re(string str) => Eval(Read(str), replEnv);

            return Print(Re(line));
        }
    }
}
