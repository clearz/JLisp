using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using JLisp.Parsing.Types;
using static JLisp.Parsing.Types.JlConstant;
using Func = JLisp.Parsing.Types.JlFunction;

namespace JLisp.Parsing.Functions
{

    static class Core
    {
        // String Functions
        public static Func AsReadableString = new Func(
               a => new JlString( Printer.PrintStrArgs( a, " ", true ) ) );
        public static Func AsString = new Func(
               a => new JlString( Printer.PrintStrArgs( a, " ", false ) ) );


        //Sequence Functions
        public static Func ListQ = new Func(
               a => a[0].GetType() == typeof(JlList) ? True : False );

        public static Dictionary<string, JlValue> Ns = new Dictionary<string, JlValue>
            {
                ["="] = new Func( a => a[0] == a[1] ? True : False ),
                ["pr-str"] = AsReadableString,
                ["str"] = AsString,
                ["prn"] = new Print(),
                ["println"] = new PrintLine(),
                ["<"] = new Func( a => (JlInteger)a[0] < (JlInteger)a[1] ),
                ["<="] = new Func( a => (JlInteger)a[0] <= (JlInteger)a[1] ),
                [">"] = new Func( a => (JlInteger)a[0] > (JlInteger)a[1] ),
                [">="] = new Func( a => (JlInteger)a[0] >= (JlInteger)a[1] ),
                ["+"] = new Func( a => (JlInteger)a[0] + (JlInteger)a[1] ),
                ["-"] = new Func( a => (JlInteger)a[0] - (JlInteger)a[1] ),
                ["*"] = new Func( a => (JlInteger)a[0] * (JlInteger)a[1] ),
                ["/"] = new Func( a => (JlInteger)a[0] / (JlInteger)a[1] ),
                ["list"] = new Func( a => new JlList( a.Value ) ),
                ["list?"] = ListQ,
                ["first"] = new Func( a => ((JlList)a[0]).Value[0] ),
                ["rest"] = new Func( a => ((JlList)a[0]).Rest() ),
                ["count"] = new Func( a => new JlInteger( ((JlList)a[0]).Size ) ),
                ["empty"] = new Func( a => ((JlList)a[0]).Size == 0 ? True : False ),

            };
        class Print : JlFunction {
            public override JlValue Apply(JlList args) {
                Console.WriteLine( Printer.PrintStrArgs( args, " ", true ) );
                return Nil;
            }
        }

        class PrintLine : JlFunction {
            public override JlValue Apply(JlList args) {
                Console.WriteLine( Printer.PrintStrArgs( args, " ", false ) );
                return Nil;
            }
        }
    }
}
