
using System;
using System.Collections.Generic;
using System.Linq;

namespace JLisp.Parsing.Types
{

    public abstract class JlValue : IEquatable<JlValue>
    {
        public JlValue Meta { get; private set; } = JlConstant.Nil;
        public JlValue SetMeta(JlValue meta)
        {
            Meta = meta;
            return this;
        }
        public bool Equals(JlValue other) => _EqualQ(this, other);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JlValue) obj);
        }

        public override int GetHashCode() => base.GetHashCode();

        public virtual string ToString(bool printReadably) => ToString();
        public virtual bool ListQ() => false;

        private static bool _EqualQ(JlValue a, JlValue b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null)) return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            if (ReferenceEquals(a, b)) return true;
            Type ota = a.GetType(), otb = b.GetType();
            if ( !((ota == otb) || (a is JlList && b is JlList)) ) return false;
            if ( a is JlInteger ji )
                return ji.Value == ((JlInteger)b).Value;
            if ( a is JlSymbol js )
                return js.Name == ((JlSymbol)b).Name;
            if ( a is JlString jt )
                return jt.Value == ((JlString)b).Value;
            if ( a is JlList lista ) {
                var listb = (JlList)b;
                if ( lista.Size != listb.Size ) return false;
                for ( int i = 0; i < lista.Size; i++ ) {
                    if ( !_EqualQ( lista[i], listb[i] ) )
                        return false;
                }
                return true;
            }
            if (a is JlHashMap hm1 && b is JlHashMap hm2)
            {
                var d1 = hm1.Value;
                var d2 = hm2.Value;
                return d1.Count == d2.Count &&
                       d1.Keys.All(k1 => d2.Keys.Any(k2 => k1 == k2) && d1[k1].Equals(d2[k1]));
            }
            return false;
        }

        public virtual JlValue Copy()
        {
            return (JlValue) MemberwiseClone();
        }
        public static bool operator ==(JlValue a, JlValue b) => _EqualQ(a, b);
        public static bool operator !=(JlValue a, JlValue b) => !_EqualQ(a, b);

        public static implicit operator JlValue(string s) => new JlString(s);
        public static implicit operator JlValue(bool b) => b ? JlConstant.True : JlConstant.False;
        public static implicit operator JlValue(int i) => new JlInteger(i);

    }

    public class JlConstant : JlValue
    {
        private readonly string _value;
        private JlConstant(string value) { _value = value; }
        public static JlConstant Nil { get; } = new JlConstant("nil");
        public static JlConstant True { get; } = new JlBoolean("true");
        public static JlConstant False { get; } = new JlBoolean("false");
        public new JlConstant Copy() => this;
        public override string ToString() => _value;

        private class JlBoolean : JlConstant {
            internal JlBoolean(string value) : base(value){}
        }

        public static implicit operator JlConstant(bool b) => b ? True : False;
    }

    public class JlInteger : JlValue
    {
        public int Value { get; }
        public JlInteger(int value) { Value = value; }
        public new JlInteger Copy() { return this; }
        public override string ToString() => Value.ToString();

        public static JlInteger operator +(JlInteger j1, JlInteger j2) => new JlInteger(j1.Value + j2.Value);
        public static JlInteger operator -(JlInteger j1, JlInteger j2) => new JlInteger(j1.Value - j2.Value);
        public static JlInteger operator *(JlInteger j1, JlInteger j2) => new JlInteger(j1.Value * j2.Value);
        public static JlInteger operator /(JlInteger j1, JlInteger j2) => new JlInteger(j1.Value / j2.Value);

        public static JlConstant operator >(JlInteger j1, JlInteger j2) => j1.Value > j2.Value;
        public static JlConstant operator >=(JlInteger j1, JlInteger j2) => j1.Value >= j2.Value;
        public static JlConstant operator <(JlInteger j1, JlInteger j2) => j1.Value < j2.Value;
        public static JlConstant operator <=(JlInteger j1, JlInteger j2) => j1.Value <= j2.Value;

        public static implicit operator int(JlInteger ji) => ji.Value;

    }
    public class JlSymbol : JlValue
    {
        public string Name { get; }
        public JlSymbol(string value)  { Name = value; }
        public override string ToString() => Name;
        public new JlSymbol Copy() => this;

        public static implicit operator string(JlSymbol val) => val.Name;
        public static explicit operator JlSymbol(string s) => new JlSymbol(s);
    }

    public class JlString : JlValue
    {
        public string Value { get; }
        public JlString(string value)  { Value = value; }
        public new JlString Copy() => this;
        public override string ToString() => $"\"{Value}\"";

        public override string ToString(bool printReadable)
        {
            if (Value.Length > 0 && Value[0] == '\u029e')
                return $":{Value.Substring(1)}";
            if (printReadable)
                return "\"" + Value
                            .Replace("\\", "\\\\")
                            .Replace("\"", "\\\"")
                            .Replace("\n", "\\n") + "\"";
            
            return Value;
        }

        //public static implicit operator JlInteger(JlString s) => new JlInteger(Convert.ToInt32(s.Value));
     }

    public class JlList : JlValue
    {
        protected string Start = "(", End = ")";
        public List<JlValue> Value { get; }

        public JlList(IEnumerable<JlValue> list) {
            Value = list == null ? new List<JlValue>() : new List<JlValue>(list);
        }
        public JlList(params JlValue[] list ) : this((IEnumerable<JlValue>)list) { }
        public override bool ListQ() => true;
        public override string ToString() => ToString(true);
        public override string ToString(bool printReadably) => $"{Start}{Printer.Join(Value, " ", printReadably)}{End}";

        public override JlValue Copy() => new JlList(Value.Select(v => v.Copy()));

        public JlList ConjBang(params JlValue[] jvs)
        {
            Value.AddRange(jvs);
            return this;
        }

        public int Size => Value.Count;
        public JlValue this[int idx] => Value.Count > 0 ? Value[idx] : JlConstant.Nil;
        public JlList Rest()
        {
            if(Size > 0) return new JlList(Value.GetRange(1, Size -1));
            return new JlList();
        }

        public virtual JlList Slice(int start)
        {
            return new JlList( Value.GetRange( start, Value.Count-start ) );
        }
        public virtual JlList Slice(int start, int end)
        {
            return new JlList(Value.GetRange(start, end - start));
        }
        public static implicit operator JlList(List<JlValue> ji) => new JlList(ji.ToArray());


    }

    public class JlVector : JlList
    {
        public JlVector(IEnumerable<JlValue> val = null) : base(val) {
            Start = "["; End = "]";
        }

        public override JlValue Copy() => new JlVector(Value.Select(v => v.Copy()));
        public override bool ListQ() => false;
        public override JlList Slice(int start)
        {
            return new JlVector(Value.GetRange(start, Value.Count - start));
        }
    }

    public class JlHashMap : JlValue
    {
        public Dictionary<string, JlValue> Value { get; private set; }

        public JlHashMap(Dictionary<string, JlValue> value) {
            Value = value;
        }
        public JlHashMap(JlList list)
        {
            Value = list.Value.Where((v, i) => i % 2 == 0)
                .Zip(list.Value.Where((v, i) => i % 2 == 1),
                    (j1, j2) => new {Key=((JlString)j1).Value, Value=j2}).ToDictionary(kv => kv.Key,kv => kv.Value);
        }
        public override string ToString() => ToString(true);
        public override string ToString(bool printReadably) => $"{{{Printer.Join(Value, " ", printReadably)}}}";
        public new JlHashMap Copy()
        {
            var newSelf = (JlHashMap)this.MemberwiseClone();
            newSelf.Value = new Dictionary<string, JlValue>(Value);
            return newSelf;
        }
        public JlHashMap ConjBang(params JlValue[] jvs) {
            foreach (var value in jvs)
            {
                string key = value.ToString();
                Value.Add(key, value);
            }
            return this;
        }


        public JlHashMap AssocBang(JlList lst)
        {
            for (int i = 0; i < lst.Size; i += 2)
            {
                string key = ((JlString) lst[i]).Value;
                if (Value.ContainsKey(key))
                    Console.WriteLine("Current: {0}, New: {1}", Value[key], lst[i + 1]);
                else
                Value.Add(key, lst[i+1]);
            }
            return this;
        }

        public JlHashMap DissocBang(JlList lst)
        {
            for (int i = 0; i < lst.Size; i++)
                Value.Remove(((JlString)lst[i]).Value);
            return this;
        }
    }

    public class JlAtom : JlValue
    {
        public JlValue Value { get; set; }

        public JlAtom(JlValue value) {
            Value = value;
        }

        public override string ToString() => ToString(true);
        public override string ToString(bool printReadably) => $"(atom {Printer.PrintStr(Value, printReadably)})";
    }
    public class JlFunction : JlValue
    {
        private readonly Func<JlList, JlValue> _func;
        public JlValue Ast { get; }
        private Env Env { get; }
        private JlList FParams { get; }
        public bool IsMacro => _macro;
        public JlFunction(JlValue ast, Env env, JlList fparams,
                          Func<JlList, JlValue> func) : this(func) {
            Ast = ast;
            Env = env;
            FParams = fparams;
        }
        protected JlFunction() { }
        public JlFunction(Func<JlList, JlValue> func) { _func = func; }
        public virtual JlValue Apply(JlList args) => _func( args );

        public Env GenEnv(JlList args) => new Env( Env, FParams, args );
        public override string ToString() {
            if ( Ast != null )
                return $"<fn* {Printer.PrintStr( FParams, true )}>";
            if(_func != null)
                return $"<builtin_lambda {_func.GetType().Name}>";
            return $"<builtin_class {GetType().Name}>";
        }
        public static implicit operator JlFunction(Func<JlList, JlValue> func) => new JlFunction(func);
        public void SetMacro() { _macro = true; }

        private bool _macro;
    }
}
