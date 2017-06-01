using System;
using System.Globalization;
using System.Linq.Expressions;
using JLisp.Parsing;
using JLisp.Parsing.Types;

namespace JLisp
{
    class REPL
    {
        const string PROMPT = "-> ";
        const string Heading = "JLisp v 0.0.3, By John Cleary.";

        static JlValue READ(string str) => Reader.ReadStr(str);

        static JlValue EVAL(JlValue ast, string env)
        {
            return ast;
        }

        static string PRINT(JlValue exp)
        {
            return Printer.PrintStr(exp, true) + $", Type: {exp.GetType().Name}";
        }

        static JlValue RE(string env, string str)
        {
            return EVAL(READ(str), env);
        }

        static void Main(string[] args)
        {
            Console.WriteLine(Heading);
            string input;
            while (true)
            {
                Console.Write(PROMPT);
                input = Console.ReadLine();
                if (HandleCmd()) continue;
                try
                {
                    Console.WriteLine(PRINT(RE(null, input)));
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