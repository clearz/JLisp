using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Text;
using JLisp.Parsing;
using JLisp.Parsing.Functions;
using JLisp.Parsing.Types;

namespace JLisp
{
    class REPL
    {
        const string PROMPT = "-> ";
        const string Heading = "JLisp v 0.0.3, By John Cleary.";

        static JlValue EvalAst(JlValue ast, Dictionary<string, JlValue> env)
        {
            if (ast is JlSymbol sym) return env[sym.Name];
            if (ast is JlList oldLst)
            {
                var newLst = ast.ListQ() ? new JlList() : new JlVector();
                foreach (var jv in oldLst.Value)
                    newLst.ConjBANG(EVAL(jv, env));
                return newLst;
            }
            if (ast is JlHashMap map)
            {
                var newDict = new Dictionary<string, JlValue>();
                foreach (var jv in map.Value)
                {
                    newDict.Add(jv.Key, EVAL(jv.Value, env));
                }
                return new JlHashMap(newDict);
            }
            return ast;
        }

        static JlValue READ(string str) => Reader.ReadStr(str);

        static JlValue EVAL(JlValue origAst, Dictionary<string, JlValue> env)
        {
            JlValue a0;
            if (!origAst.ListQ()) return EvalAst(origAst, env);

            var ast = (JlList) origAst;
            if (ast.Size() == 0) return ast;
            a0 = ast.Nth(0);
            if (!(a0 is JlSymbol))
                throw new JlError($"attempt to apply on non-symbol '{Printer.PrintStr(a0, true)}'");

            JlValue args = EvalAst(ast.Rest(), env);
            JlSymbol fsym = (JlSymbol) a0;
            var f = (JlFunction) env[fsym.Name];
            if (f == null)
                throw new JlError($"'{fsym.Name}' not found");
            return f.Apply((JlList) args);
        }

        static string PRINT(JlValue exp)
        {
            return Printer.PrintStr(exp, true) + $", Type: {exp.GetType().Name}";
        }

        static JlValue RE(Dictionary<string, JlValue> env, string str)
        {
            return EVAL(READ(str), env);
        }

        static void Main(string[] args)
        {
            Console.WriteLine(Heading);
            var replEnv = new Dictionary<string, JlValue>
            {
                ["+"] = new Plus(),
                ["-"] = new Minus(),
                ["*"] = new Multiply(),
                ["/"] = new Divide()
            };

            string input;
            while (true)
            {
                Console.Write(PROMPT);
                input = Console.ReadLine();
                if (HandleCmd()) continue;
                try
                {
                    Console.WriteLine(PRINT(RE(replEnv, input)));
                }
                catch (JlContinue)
                {
                    continue;
                }
                catch (JlError e)
                {
                    Console.WriteLine("ERROR:" + e.Message);
                }
                catch (ParseError e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            bool HandleCmd()
            {
                switch (input)
                {
                    case "cls":
                        Console.Clear();
                        return true;
                    case "exit":
                    case "quit":
                        Environment.Exit(0);
                        break;
                }
                return false;
            }
        }
    }
}