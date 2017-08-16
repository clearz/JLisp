using System.Collections.Generic;
using JLisp.Parsing;
using JLisp.Parsing.Types;

namespace JLisp.Tests.Repls
{
    class Repl3 : IProcess
    {
        // read
        static JlValue Read(string str)
        {
            return Reader.ReadStr(str);
        }

        // eval
        static JlValue eval_ast(JlValue ast, Env env)
        {
            if (ast is JlSymbol)
            {
                return env.Get((JlSymbol)ast);
            }
            else if (ast is JlList)
            {
                JlList oldLst = (JlList)ast;
                JlList newLst = ast.IsList ? new JlList()
                    : (JlList)new JlVector();
                foreach (JlValue mv in oldLst.Value)
                {
                    newLst.AddRange(Eval(mv, env));
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


        static JlValue Eval(JlValue origAst, Env env)
        {
            JlValue a0, a1, a2, res;
            JlList el;
            //Console.WriteLine("EVAL: " + Printer.PrintStr(orig_ast, true));
            if (!origAst.IsList)
            {
                return eval_ast(origAst, env);
            }

            // Apply list
            JlList ast = (JlList)origAst;
            if (ast.Count == 0) { return ast; }
            a0 = ast[0];
            if (!(a0 is JlSymbol))
            {
                throw new JlError("attempt to Apply on non-symbol '"
                                             + Printer.PrintStr(a0, true) + "'");
            }

            switch (((JlSymbol)a0).Name)
            {
                case "def!":
                    a1 = ast[1];
                    a2 = ast[2];
                    res = Eval(a2, env);
                    env.Set((JlSymbol)a1, res);
                    return res;
                case "let*":
                    a1 = ast[1];
                    a2 = ast[2];
                    JlSymbol key;
                    JlValue val;
                    Env letEnv = new Env(env);
                    for (int i = 0; i < ((JlList)a1).Count; i += 2)
                    {
                        key = (JlSymbol)((JlList)a1)[i];
                        val = ((JlList)a1)[i + 1];
                        letEnv.Set(key, Eval(val, letEnv));
                    }
                    return Eval(a2, letEnv);
                default:
                    el = (JlList)eval_ast(ast, env);
                    var f = (JlFunction)el[0];
                    return f.Invoke(el.GetTail());
            }
        }

        // print
        static string Print(JlValue exp)
        {
            return Printer.PrintStr(exp, true);
        }

        private Env _rootEnv = null;
        public void Init()
        {
            _rootEnv = new Env(null);
            _rootEnv.Set(new JlSymbol("+"), new JlFunction(
                a => (JlInteger)a[0] + (JlInteger)a[1]));
            _rootEnv.Set(new JlSymbol("-"), new JlFunction(
                a => (JlInteger)a[0] - (JlInteger)a[1]));
            _rootEnv.Set(new JlSymbol("*"), new JlFunction(
                a => (JlInteger)a[0] * (JlInteger)a[1]));
            _rootEnv.Set(new JlSymbol("/"), new JlFunction(
                a => (JlInteger)a[0] / (JlInteger)a[1]));
        }
        // repl
        public string Process(string line)
        {
            if (_rootEnv == null) Init();
            JlValue Re(string str) => Eval(Read(str), _rootEnv);

            return Print(Re(line));
        }

    }
}
