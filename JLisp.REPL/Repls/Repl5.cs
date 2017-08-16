﻿using System;
using System.Collections.Generic;
using JLisp.Parsing;
using JLisp.Parsing.Functions;
using JLisp.Parsing.Types;
using static JLisp.Parsing.Types.JlConstant;

namespace JLisp.Tests.Repls
{
    class Repl5 : IProcess
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

            // core.mal: defined using the language itself
            Re("(def! not (fn* (a) (if a false true)))");
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