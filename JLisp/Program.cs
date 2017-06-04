using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using JLisp.Parsing;
using JLisp.Parsing.Functions;
using JLisp.Parsing.Types;
using static JLisp.Parsing.Types.JlConstant;

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

        static JlValue EVAL(JlValue origAst, Env env) {
            JlValue a0, a1, a2, a3;
            JlList el;
            while ( true ) {
                if ( !origAst.ListQ() ) return EvalAst( origAst, env );

                var ast = (JlList)origAst;
                if ( ast.Size == 0 ) return ast;
                a0 = ast[0];
                string a0sym = a0 is JlSymbol ? ((JlSymbol)a0).Name : "__<*fn*>__";

                switch ( a0sym ) {
                    case "def!":
                        a1 = ast[1];
                        a2 = ast[2];
                        var res = EVAL( a2, env );
                        env.Set( ((JlSymbol)a1).Name, res );
                        return res;
                    case "let*":
                        a1 = ast[1];
                        a2 = ast[2];
                        var letEnv = new Env( env );
                        for ( int i = 0; i < ((JlList)a1).Size; i += 2 ) {
                            var key = (JlSymbol)((JlList)a1)[i];
                            var val = ((JlList)a1)[i + 1];
                            letEnv.Set( key.Name, EVAL( val, letEnv ) );
                        }
                        return EVAL( a2, letEnv );
                    case "do":
                        el = (JlList)EvalAst( ast.Rest(), env );
                        return el[el.Size - 1];
                    case "if":
                        a1 = ast[1];
                        var cond = EVAL( a1, env );
                        if ( cond == Nil || cond == False ) {
                            if ( ast.Size > 3 ) 
                                origAst = ast[3];
                            else return Nil;
                        }
                        else origAst = ast[2];
                        break;
                    case "fn*":
                        var a1f = (JlList)ast[1];
                        var a2f = ast[2];
                        return new JlFunction( a2f, env, a1f,
                            args => EVAL( a2f, new Env( env, a1f, args ) ) );
                    default:
                        el = (JlList)EvalAst( ast, env );
                        var f = (JlFunction)el[0];
                        var fnast = f.Ast;
                        if ( fnast != null ) {
                            origAst = fnast;
                            env = f.GenEnv( el.Rest() );
                        }
                        else
                            return f.Apply( el.Rest() );
                        break;
                }
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

        static void Main(string[] args) {
            Console.WriteLine(Heading);
            var replEnv = new Env( null );
            foreach ( var entry in Core.Ns )
                _ref( replEnv, entry.Key, entry.Value );

            RE( replEnv, "(def! not (fn* [a] (if a false true)))" );

            while ( true )
            {
                string input = String.Empty;
                Console.Write( PROMPT );
                do {
                    input += Console.ReadLine();
                } while ( CheckBalanced( ref input ) );
                if ( HandleCmd(input) ) continue;

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
                //catch (Exception e) {
                //    Console.WriteLine("ERROR: " + e.Message);
                //}
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
        }

        private static bool CheckBalanced(ref string s, string ident = "   ") {
            if ( string.IsNullOrWhiteSpace( s ) ) return false;

            int bal = 0;
            bal = (s.Count( c => c == '(' || c == '[' || c == '{' ) - s.Count( c => c == ')' || c == ']' || c == '}' ));
            bool b = bal != 0;
            if ( b ) Console.Write( ident );
            return b;
        }
    }
}