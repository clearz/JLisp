using System;
using System.Collections.Generic;
using JLisp.Parsing;
using JLisp.Parsing.Functions;
using JLisp.Parsing.Types;
using static JLisp.Parsing.Types.JlConstant;

namespace JLisp
{
    class ConsoleREPL7
    {

        static string Format(JlValue exp)
        {
            return Printer.PrintStr(exp, true);// + $", Type: {exp.GetType().Name}";
        }
#if IS_MAIN
        static void Main(string[] args)
        {
            // IProcess p = new Repl8();
            InputReader inputReader = InputReader.Raw;
            while (true)
            {
                try
                {
                    string input = inputReader.Readline();
                    try
                    {
                        //Console.WriteLine(p.Process(input)); 
                        var jval = Evaluator.Eval(input);
                        Console.Write(Format(jval) + "\n");
                    }
                    finally
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                }
                catch (JlContinue)
                {
                    continue;
                }
                catch (JlException e)
                {
                    Console.Write("ERROR: " + Printer.PrintStr(e.Value, false) + "\n");
                }
                catch (Exception e)
                {
                    Console.Write("ERROR: " + e.Message);
                    Console.Write(e.StackTrace);
                }
                finally
                {
                    Console.ResetColor();
                }
            }


        }
#endif

    }
    class Repl7 : IProcess
    {
        // read
        static JlValue Read(string str)
        {
            return Reader.ReadStr(str);
        }

        // eval
        public static bool is_pair(JlValue x)
        {
            return x is JlList && ((JlList)x).Size > 0;
        }

        public static JlValue Quasiquote(JlValue ast)
        {
            if (!is_pair(ast))
            {
                return new JlList(new JlSymbol("quote"), ast);
            }
            else
            {
                JlValue a0 = ((JlList)ast)[0];
                if ((a0 is JlSymbol) &&
                    (((JlSymbol)a0).Name == "unquote"))
                {
                    return ((JlList)ast)[1];
                }
                else if (is_pair(a0))
                {
                    JlValue a00 = ((JlList)a0)[0];
                    if ((a00 is JlSymbol) &&
                        (((JlSymbol)a00).Name == "splice-unquote"))
                    {
                        return new JlList(new JlSymbol("concat"),
                            ((JlList)a0)[1],
                            Quasiquote(((JlList)ast).Rest()));
                    }
                }
                return new JlList(new JlSymbol("cons"),
                    Quasiquote(a0),
                    Quasiquote(((JlList)ast).Rest()));
            }
        }

        static JlValue eval_ast(JlValue ast, Env env)
        {
            if (ast is JlSymbol)
            {
                return env.Get((JlSymbol)ast);
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


        static JlValue Eval(JlValue origAst, Env env)
        {
            JlValue a0, a1, a2, res;
            JlList el;

            while (true)
            {

                //Console.WriteLine("EVAL: " + Printer.PrintStr(orig_ast, true));
                if (!origAst.ListQ())
                {
                    return eval_ast(origAst, env);
                }

                // Apply list
                JlList ast = (JlList)origAst;
                if (ast.Size == 0) { return ast; }
                a0 = ast[0];

                String a0Sym = a0 is JlSymbol ? ((JlSymbol)a0).Name
                    : "__<*fn*>__";

                switch (a0Sym)
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
                        for (int i = 0; i < ((JlList)a1).Size; i += 2)
                        {
                            key = (JlSymbol)((JlList)a1)[i];
                            val = ((JlList)a1)[i + 1];
                            letEnv.Set(key, Eval(val, letEnv));
                        }
                        origAst = a2;
                        env = letEnv;
                        break;
                    case "quote":
                        return ast[1];
                    case "quasiquote":
                        origAst = Quasiquote(ast[1]);
                        break;
                    case "do":
                        eval_ast(ast.Slice(1, ast.Size - 1), env);
                        origAst = ast[ast.Size - 1];
                        break;
                    case "if":
                        a1 = ast[1];
                        JlValue cond = Eval(a1, env);
                        if (cond == Nil || cond == False)
                        {
                            // eval false slot form
                            if (ast.Size > 3)
                            {
                                origAst = ast[3];
                            }
                            else
                            {
                                return Nil;
                            }
                        }
                        else
                        {
                            // eval true slot form
                            origAst = ast[2];
                        }
                        break;
                    case "fn*":
                        JlList a1F = (JlList)ast[1];
                        JlValue a2F = ast[2];
                        Env curEnv = env;
                        return new JlFunction(a2F, env, a1F,
                            args => Eval(a2F, new Env(curEnv, a1F, args)));
                    default:
                        el = (JlList)eval_ast(ast, env);
                        var f = (JlFunction)el[0];
                        JlValue fnast = f.Ast;
                        if (fnast != null)
                        {
                            origAst = fnast;
                            env = f.GenEnv(el.Rest());
                        }
                        else
                        {
                            return f.Apply(el.Rest());
                        }
                        break;
                }

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
            void Re(string str) => Eval(Read(str), _rootEnv);

            // core.cs: defined using C#
            foreach (var entry in Core.Ns)
            {
                _rootEnv.Set(new JlSymbol(entry.Key), entry.Value);
            }
            _rootEnv.Set("eval", new JlFunction(
                a => Eval(a[0], _rootEnv)));


            // core.mal: defined using the language itself
            Re("(def! not (fn* (a) (if a false true)))");
            Re("(def! load-file (fn* (f) (eval (read-string (str \"(do \" (slurp f) \")\")))))");

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