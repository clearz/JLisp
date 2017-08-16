using System;
using System.Collections.Generic;
using JLisp.Parsing;
using JLisp.Parsing.Functions;
using JLisp.Parsing.Types;
using static JLisp.Parsing.Types.JlConstant;

namespace JLisp
{
    class ConsoleREPL6
    {
        const string Heading = "JLisp v 0.7.3, By John Cleary.";

        static string Format(JlValue exp)
        {
            return Printer.PrintStr(exp, true);// + $", Type: {exp.GetType().Name}";
        }
#if IS_MAIN
        static void Main(string[] args)
        {
            // IProcess p = new Repl8();
            InputReader inputReader = InputReader.Raw;
            var _argv = new JlList();
            for (int i = 1; i < args.Length; i++)
            {
                _argv.ConjBANG(new JlString(args[i]));
            }
            Evaluator.ENV_ROOT.Set(new JlSymbol("*ARGV*"), _argv);
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
    class Repl6 : IProcess
    {
        static JlValue Read(string str)
        {
            return Reader.ReadStr(str);
        }

        // eval
        static JlValue eval_ast(JlValue ast, Env env)
        {
            if (ast is JlSymbol sym)
            {
                return env.Get(sym);
            }
            if (ast is JlList oldLst)
            {
                JlList newLst = ast.IsList ? new JlList() : new JlVector();
                foreach (JlValue mv in oldLst.Value)
                {
                    newLst.AddRange(Eval(mv, env));
                }
                return newLst;
            }
            if (ast is JlHashMap hmap)
            {
                var newDict = new Dictionary<string, JlValue>();
                foreach (var entry in hmap.Value)
                {
                    newDict.Add(entry.Key, Eval(entry.Value, env));
                }
                return new JlHashMap(newDict);
            }
            return ast;
        }


        static JlValue Eval(JlValue origAst, Env env)
        {
            JlValue a0, a1, a2, res;
            JlList el;

            while (true)
            {

                //Console.WriteLine("EVAL: " + Printer.PrintStr(orig_ast, true));
                if (!origAst.IsList)
                {
                    return eval_ast(origAst, env);
                }

                // Apply list
                JlList ast = (JlList)origAst;
                if (ast.Count == 0) { return ast; }
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
                        for (int i = 0; i < ((JlList)a1).Count; i += 2)
                        {
                            key = (JlSymbol)((JlList)a1)[i];
                            val = ((JlList)a1)[i + 1];
                            letEnv.Set(key, Eval(val, letEnv));
                        }
                        origAst = a2;
                        env = letEnv;
                        break;
                    case "do":
                        eval_ast(ast.Slice(1, ast.Count - 1), env);
                        origAst = ast[ast.Count - 1];
                        break;
                    case "if":
                        a1 = ast[1];
                        JlValue cond = Eval(a1, env);
                        if (cond == Nil || cond == False)
                        {
                            // eval false slot form
                            if (ast.Count > 3)
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
                            env = f.CreateChildEnv(el.GetTail());
                        }
                        else
                        {
                            return f.Invoke(el.GetTail());
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