using System;
using System.Collections.Generic;
using System.Linq;
using JLisp.Parsing;
using JLisp.Parsing.Types;

namespace JLisp.REPL
{
    class ConsoleREPL
    {
        const string Heading = "JLisp v 0.7.3, By John Cleary.";

        static string Format(JlValue exp)
        {
            return Printer.PrintStr(exp, true) + $", Type: {exp.GetType().Name}";
        }

        static void Main(string[] args)
        {
            InputReader inputReader = InputReader.Terminal;
            if (!HandleArgs(args.ToList())) return;
            Console.WriteLine(Heading);


            while (true)
            {
                string input = inputReader.Readline();
                if (HandleCmd(input)) continue;
                try
                {
                    try
                    {
                        var jval = Evaluator.Eval(input);
                        Console.WriteLine(Format(jval));
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
                catch (JlError e)
                {
                    Console.WriteLine("ERROR: " + e.Message);
                }
                catch (JlException e)
                {
                    Console.WriteLine("ERROR: " + e.Value);
                }
                catch (ParseError e)
                {
                    Console.WriteLine(e.Message);
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR: " + e.Message);
                    Console.WriteLine(e.StackTrace);
                }
                finally
                {
                    Console.ResetColor();
                }
            }

            bool HandleCmd(string input)
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

            bool HandleArgs(List<string> l)
            {
                try
                {
                    
                    if (l.Contains("--help")) {
                        PrintMsg();
                        return false;
                    }
                    var silent = l.Contains("--silent");
                    if (silent) l.Remove("--silent");
                    if (l.Contains("--raw"))
                    {
                        inputReader = InputReader.Raw;
                        l.Remove("--raw");
                    }
                    if (l.Contains("--include"))
                    {
                        var idx = l.IndexOf("--include") + 1;
                        foreach (var file in l.Skip(idx).TakeWhile(s => s.ToLower().EndsWith(".mal")))
                        {
                            Evaluator.Eval("(load-file \"" + file + "\")");
                        }
                    }
                    foreach (var code in l.Where(s => s[0] == '(' && s.EndsWith(")")))
                    {
                        Evaluator.Eval(code);
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("ERROR: " + e.Message);
                    Console.WriteLine(e.StackTrace);
                    return false;
                }

                return true;
            }

            void PrintMsg()
            {
                Console.WriteLine("Usage: [<cmd> list?] [code]");
                Console.WriteLine("       Commands: ");
                Console.WriteLine("           --raw 'Use the os console input. Otherwise a custom reader will be used.'");
                Console.WriteLine("           --include 'Include list of external *.mal files.'");
                Console.WriteLine("           --help 'Show this help message.'");
                Console.WriteLine("           --silent 'Preform everything in the background.");
                Console.WriteLine("       Strings surrounded in parens will be evaluated in sequence");
                Console.WriteLine("       e.g.'(def! x 5) (+ x 6)' will output'x' then '11'");

            }
        }


    }
}