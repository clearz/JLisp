using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JLisp.Parsing.Types;
using static JLisp.Parsing.Types.JlConstant;
using Func = JLisp.Parsing.Types.JlFunction;
using F = System.Func<JLisp.Parsing.Types.JlList, JLisp.Parsing.Types.JlValue>;

namespace JLisp
{

    public interface IProcess
    {
        void Init();
        string Process(string line);
    }

}
namespace JLisp.Parsing.Functions
{
    public static class Core
    {
        private static Func F(F f) => new Func(f);
        //Errors/Exceptions
        private static Func MalThrow 
            = F(a => throw new JlException(a[0]));

        // Scalar Functions
        private static readonly Func NilQ = F(a => a[0] == Nil);
        private static readonly Func TrueQ = F(a => a[0] == True);
        private static readonly Func FalseQ = F(a => a[0] == False);
        private static readonly Func SymbolQ = F(a => a[0] is JlSymbol);

        private static readonly Func StringQ = F(a =>
        {
            if (a[0] is JlString)
            {
                var str = ((JlString) a[0]).Value;
                return str.Length == 0 || str[0] != '\u029e';
            }

            return False;
        });

        private static readonly Func Keyword = F(a =>
        {
            if (a[0] is JlString && ((JlString)a[0]).Value[0] == '\u029e')
                return a[0];

            return $"\u029e{((JlString)a[0]).Value}";
        });

        private static readonly Func KeywordQ = F(a =>
        {
            if (a[0] is JlString)
            {
                var str = ((JlString)a[0]).Value;
                return str.Length > 0 && str[0] == '\u029e';
            }

            return False;
        });
        // String Functions
        private static readonly Func PrStr = F(a => Printer.PrintStrArgs( a, " ", true ));
        private static readonly Func Str = F(a => Printer.PrintStrArgs( a, "", false ) );

        private static readonly JlFunction JlReadLine = F(a =>
        {
            Console.Write(((JlString)a[0]).Value);
            var line =  Console.ReadLine();
            if (line == null) return Nil;
            return line;
        });

        private static readonly JlFunction ReadString = F(a => Reader.ReadStr(((JlString) a[0]).Value));
        private static readonly JlFunction Slurp = F(a => File.ReadAllText(((JlString) a[0]).Value));

        //List/Vector Functions
        private static readonly Func ListQ = F(a => a[0].GetType() == typeof(JlList) );
        private static readonly Func VectorQ = F(a => a[0].GetType() == typeof(JlVector) );
        private static readonly Func HashMapQ = F(a => a[0].GetType() == typeof(JlHashMap) );
        private static readonly Func ContainsQ = F(a =>
        {
            string key = ((JlString)a[1]).Value;
            var dict = ((JlHashMap)a[0]).Value;
            return dict.ContainsKey(key);
        });
        private static readonly Func Assoc = F(a =>
        {
            var newHm = ((JlHashMap)a[0]).Copy();
            return newHm.Add(a.Slice(1));
        });
        private static readonly Func Dissoc = F(a =>
        {
            var newHm = ((JlHashMap)a[0]).Copy();
            return newHm.Remove(a.Slice(1));
        });
        private static readonly Func Get = F(a =>
        {
            if (a[0] == Nil) return Nil;

            string key = ((JlString)a[1]).Value;
            var dict = ((JlHashMap)a[0]).Value;
            return dict.ContainsKey(key) ? dict[key] : Nil;
        });
        private static readonly Func Keys = F(a =>
        {
            var dict = ((JlHashMap)a[0]).Value;
            var keyList = new JlList();
            foreach (var key in dict.Keys)
                keyList.AddRange(key);
            return keyList;
        });
        private static readonly Func Vals = F(a =>
        {
            var dict = ((JlHashMap)a[0]).Value;
            var valList = new JlList();
            foreach (var val in dict.Values)
                valList.AddRange(val);
            return valList;
        });

        // Sequence Functions
        private static readonly Func SequentialQ = F(a => a[0] is JlList);

        private static readonly Func Nth = F(a =>
        {
            var idx = (JlInteger) a[1];
            if (idx < ((JlList) a[0]).Count)
                return ((JlList) a[0])[idx];

            throw new JlException("nth: index out of range");
        } );

        private static readonly Func Cons = F(a =>
        {
            var lst = new List<JlValue> {a[0]};
            lst.AddRange(((JlList) a[1]).Value);
            return new JlList(lst) as JlValue;
        });

        private static readonly Func Concat = F(a =>
        {
            if (a.Count == 0) return new JlList();
            var lst = new List<JlValue>();
            lst.AddRange(((JlList) a[0]).Value);
            for (int i = 1; i < a.Count; i++)
                lst.AddRange(((JlList) a[i]).Value);
            return new JlList(lst) as JlValue;

        });
        private static readonly Func First = F(a => a[0] == Nil ? Nil : ((JlList) a[0])[0]);
        private static readonly Func GetTail = F(a => a[0] == Nil ? new JlList() : ((JlList) a[0]).GetTail());
        private static readonly Func EmptyQ = F(a => a[0] == Nil ? (JlValue) Nil : ((JlList)a[0]).Count == 0);
        private static readonly Func Count = F(a => a[0] == Nil ? (JlValue)Nil : ((JlList)a[0]).Count);
        private static readonly Func ConJ = F(a =>
        {
            var srcLst = ((JlList) a[0]).Value;
            var newLst = new List<JlValue>();
            newLst.AddRange(srcLst);
            if (a[0] is JlVector)
            {
                for (int i = 1; i < a.Count; i++)
                    newLst.Add(a[i]);
                return new JlVector(newLst);
            }
            for (int i = 1; i < a.Count; i++)
                newLst.Insert(0, a[i]);
            return new JlList(newLst);
        });

        private static readonly Func Seq = new JlFunction(a =>
        {
            if (a[0] == Nil) return Nil;
            if (a[0] is JlVector)
                return ((JlVector)a[0]).Count == 0 ? (JlValue)Nil : new JlList(((JlVector) a[0]).Value);
            if(a[0] is JlList)
                return ((JlList)a[0]).Count == 0 ? (JlValue)Nil : a[0];
            if (a[0] is JlString)
            {
                var s = ((JlString) a[0]).Value;
                if (s.Length == 0) return Nil;

                var charList = new List<JlValue>();
                foreach (var c in s)
                    charList.Add(c.ToString());

                return new JlList(charList);
            }

            return Nil;
        });
        // General List related functions
        private static readonly Func Apply = F(a =>
        {
            var f = (JlFunction)a[0];
            var lst = new List<JlValue>();
            lst.AddRange(a.Slice(1, a.Count - 1).Value);
            lst.AddRange(((JlList)a[a.Count - 1]).Value);
            return f.Invoke(new JlList(lst));
        });

        private static readonly Func Map = F(a =>
        {
            var f = (JlFunction)a[0];
            var srcLst = ((JlList)a[1]).Value;
            return new JlList(srcLst.Select(t => f.Invoke(new JlList(t))));
        });
        // Metadata Functions
        private static readonly Func Meta = F(a => a[0].Meta);
        private static readonly Func WithMeta = F(a => a[0].Copy().SetMeta(a[1]));

        // Atom Functions
        private static readonly Func AtomQ = F(a => a[0] is JlAtom);
        private static readonly Func Deref = F(a => ((JlAtom)a[0]).Value);
        private static readonly Func ResetBang = F(a => ((JlAtom)a[0]).Value = a[1]);
        private static readonly Func SwapBang = F(a =>
        {
            var atm = (JlAtom) a[0];
            var f = (JlFunction) a[1];
            var newLst = new List<JlValue> {atm.Value};
            newLst.AddRange(a.Slice(2).Value);
            return atm.Value = f.Invoke(new JlList(newLst));
        });

        // Number Functions
        private static readonly JlFunction TimeMs =
            F(a => new JlInteger((int) (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond)));
        public static Dictionary<string, JlValue> Ns =
            new Dictionary<string, JlValue>
            {
                ["="] = new Func(a => a[0] == a[1]),
                ["throw"] = MalThrow,
                ["nil?"] = NilQ,
                ["true?"] = TrueQ,
                ["false?"] = FalseQ,
                ["symbol"] = new JlFunction(a => new JlSymbol(((JlString)a[0]).Value)),
                ["symbol?"] = SymbolQ,
                ["string?"] = StringQ,
                ["keyword"] = Keyword,
                ["keyword?"] = KeywordQ,

                ["pr-str"] = PrStr,
                ["str"] = Str,
                ["prn"] = new Print(),
                ["println"] = new PrintLine(),
                ["readline"] = JlReadLine,
                ["read-string"] = ReadString,
                ["slurp"] = Slurp,
                ["<"] = F(a => (JlInteger) a[0] < (JlInteger) a[1]),
                ["<="] = F(a => (JlInteger) a[0] <= (JlInteger) a[1]),
                [">"] = F(a => (JlInteger) a[0] > (JlInteger) a[1]),
                [">="] = F(a => (JlInteger) a[0] >= (JlInteger) a[1]),
                ["+"] = F(a => (JlInteger) a[0] + (JlInteger) a[1]),
                ["-"] = F(a => (JlInteger) a[0] - (JlInteger) a[1]),
                ["*"] = F(a => (JlInteger) a[0] * (JlInteger) a[1]),
                ["/"] = F(a => (JlInteger) a[0] / (JlInteger) a[1]),
                ["time-ms"] = TimeMs,

                ["list"] = new Func(a => new JlList(a.Value)),
                ["list?"] = ListQ,
                ["vector"] = new Func(a => new JlVector(a.Value)),
                ["vector?"] = VectorQ,
                ["hash-map"] = new Func(a => new JlHashMap(a)),
                ["map?"] = HashMapQ,
                ["contains?"] = ContainsQ,
                ["assoc"] = Assoc,
                ["dissoc"] = Dissoc,
                ["get"] = Get,
                ["keys"] = Keys,
                ["vals"] = Vals,

                ["sequential?"] = SequentialQ,
                ["cons"] = Cons,
                ["concat"] = Concat,
                ["nth"] = Nth,
                ["first"] = First,
                ["rest"] = GetTail,
                ["count"] = Count,
                ["empty?"] = EmptyQ,
                ["conj"] = ConJ,
                ["seq"] = Seq,
                ["apply"] = Apply,
                ["map"] = Map,

                ["with-meta"] = WithMeta,
                ["meta"] = Meta,
                ["atom"] = F(a => new JlAtom(a[0])),
                ["atom?"] = AtomQ,
                ["deref"] = Deref,
                ["reset!"] = ResetBang,
                ["swap!"] = SwapBang,

            };
        class Print : Func {
            public override JlValue Invoke(JlList args) {
                Console.Write( Printer.PrintStrArgs( args, " ", true) + "\n");
                return Nil;
            }
        }

        class PrintLine : Func {
            public override JlValue Invoke(JlList args)
            {
                System.Diagnostics.Debugger.Launch();
                Console.Write( Printer.PrintStrArgs( args, " ", false ) + "\n");
                return Nil;
            }
        }
    }
}
