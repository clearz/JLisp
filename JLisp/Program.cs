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

        static JlValue EvalAst(JlValue ast, Env env)
        {
            if (ast is JlSymbol sym) return env.Get(sym.Name);
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
                foreach ( var jv in map.Value )
                    newDict.Add( jv.Key, EVAL( jv.Value, env ) );

                return new JlHashMap(newDict);
            }
            return ast;
        }

        static JlValue READ(string str) => Reader.ReadStr(str);

        static JlValue EVAL(JlValue origAst, Env env)
        {
            JlValue a0, a1, a2, res;
            if (!origAst.ListQ()) return EvalAst(origAst, env);

            var ast = (JlList) origAst;
            if (ast.Size() == 0) return ast;
            a0 = ast.Nth(0);
            if (!(a0 is JlSymbol))
                throw new JlError($"attempt to apply on non-symbol '{Printer.PrintStr(a0, true)}'");
            switch ( ((JlSymbol)a0).Name )
            {
                case "def!":
                    a1 = ast.Nth( 1 );
                    a2 = ast.Nth( 2 );
                    res = EVAL(a2, env);
                    env.Set( ((JlSymbol)a1).Name, res );
                    return res;
                case "let*":
                    a1 = ast.Nth(1);
                    a2 = ast.Nth(2);
                    JlSymbol key;
                    JlValue val;
                    Env letEnv = new Env( env );
                    for ( int i = 0; i < ((JlList)a1).Size(); i += 2 ) {
                        key = (JlSymbol)((JlList)a1).Nth( i );
                        val = ((JlList)a1).Nth( i + 1 );
                        letEnv.Set( key.Name, EVAL( val, letEnv ) );
                    }
                    return EVAL( a2, letEnv );
                default:
                    var el = (JlList) EvalAst(ast, env);
                    var f = (JlFunction) el.Nth( 0 );
                    if (f == null)
                        throw new JlError($"'{el.Nth(0)}' not found");
                    return f.Apply(el.Rest());
            }
        }

        static string PRINT(JlValue exp)
        {
            return Printer.PrintStr(exp, true) + $", Type: {exp.GetType().Name}";
        }

        static JlValue RE(Env env, string str)
        {
            return EVAL(READ(str), env);
        }

        public static Env _ref(Env env, string name, JlValue val) { return env.Set( name, val ); }

        static void Main(string[] args)
        {
            Console.WriteLine(Heading);
            var replEnv = new Env( null );
            _ref(replEnv, "+", new Plus());
            _ref(replEnv, "-", new Minus());
            _ref(replEnv, "*", new Multiply());
            _ref(replEnv, "/", new Divide());
            

            string input;
            while ( true ) {
                Console.Write( PROMPT );
                input = Console.ReadLine();
                if ( HandleCmd() ) continue;
                try {
                    Console.WriteLine( PRINT( RE( replEnv, input ) ) );
                }
                catch (JlContinue) {
                    continue;
                }
                catch (JlError e) {
                    Console.WriteLine( "ERROR: " + e.Message );
                }
                catch (JlException e) {
                    Console.WriteLine( "ERROR: " + e.Value );
                }
                catch (ParseError e) {
                    Console.WriteLine( e.Message );
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