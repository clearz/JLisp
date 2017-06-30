using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using JLisp.Parsing.Types;
using static JLisp.Parsing.Types.JlConstant;
using Func = JLisp.Parsing.Types.JlFunction;

namespace JLisp.Parsing.Functions
{
    public static class Core
    {
        // String Functions
        public static Func AsReadableString = new Func(
               a => new JlString( Printer.PrintStrArgs( a, " ", true ) ) );
        public static Func AsString = new Func(
               a => new JlString( Printer.PrintStrArgs( a, " ", false ) ) );


        //Sequence Functions
        public static Func ListQ = new Func(
               a => a[0].GetType() == typeof(JlList) ? True : False );

        private static readonly Func nth = new Func( a => ((JlList)a[0])[((JlInteger)a[1]).Value] );
        private static readonly Func cons =
            new Func( a => {
                                var lst = new List<JlValue> { a[0] };
                                lst.AddRange( ((JlList)a[1]).Value );
                                return (JlValue)new JlList( lst );
                            } );

        private static readonly Func concat =
            new Func( a => {
                                if(a.Size == 0) return new JlList();
                                var lst = new List<JlValue>();
                                lst.AddRange( ((JlList)a[0]).Value );
                                for ( int i = 1; i < a.Size; i++ ) 
                                    lst.AddRange( ((JlList)a[i]).Value );
                                return (JlValue)new JlList( lst );

                            });
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
                ["cons"] = cons,
                ["concat"] = concat,
                ["nth"] = nth,
                ["first"] = new Func( a => ((JlList)a[0])[0] ),
                ["rest"] = new Func( a => ((JlList)a[0]).Rest() ),
                ["count"] = new Func( a => new JlInteger( ((JlList)a[0]).Size ) ),
                ["empty?"] = new Func( a => ((JlList)a[0]).Size == 0 ? True : False ),

            };
        class Print : Func {
            public override JlValue Apply(JlList args) {
                Console.WriteLine( Printer.PrintStrArgs( args, " ", true ) );
                return Nil;
            }
        }

        class PrintLine : Func {
            public override JlValue Apply(JlList args)
            {
                System.Diagnostics.Debugger.Launch();
                Console.WriteLine( Printer.PrintStrArgs( args, " ", false ) );
                return Nil;
            }
        }
    }
}
