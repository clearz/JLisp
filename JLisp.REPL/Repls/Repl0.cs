using System;
using System.IO;

namespace JLisp
{
    public class Repl0 : IProcess
    {
        // read
        static string Read(string str)
        {
            return str;
        }

        // eval
        static string Eval(string ast, string env)
        {
            return ast;
        }

        // print
        static string Print(string exp)
        {
            return exp;
        }

        // repl
        static string Re(string env, string str)
        {
            return Eval(Read(str), env);
        }

        public void Init()
        {
        }

        public string Process(string line) => Print(Re(null, line));
#if IS_MAIN
        public static void Main(string[] args)
        {
            var p = new Repl0();
            // repl loop
            while (true)
            {
                string line;
                try
                {
                    Console.Write("user> ");
                    line = Console.ReadLine();
                    if (line == null)
                    {
                        break;
                    }
                    if (line == "")
                    {
                        continue;
                    }
                }
                catch (IOException e)
                {
                    Console.Write("IOException: " + e.Message + "\n");
                    break;
                }
                Console.Write(p.Process(line) + "\n");

            }
        }
#endif
    }


}