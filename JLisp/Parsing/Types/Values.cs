
using System;
using System.Collections.Generic;
using System.Linq;

namespace JLisp.Parsing.Types
{

    public abstract class JlValue
    {
        public virtual string ToString(bool printReadably) => ToString();
        public virtual bool ListQ() => false;

        public static bool _EqualQ(JlValue a, JlValue b)
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
            return false;
        }

        public abstract JlValue Copy();
        public static bool operator ==(JlValue a, JlValue b) => _EqualQ(a, b);
        public static bool operator !=(JlValue a, JlValue b) => !_EqualQ(a, b);

        public static implicit operator string(JlValue val) => val.ToString();
    }

    public class JlConstant : JlValue
    {
        private readonly string _value;
        private JlConstant(string value) { _value = value; }
        public static JlConstant Nil { get; } = new JlConstant("nil");
        public static JlConstant True { get; } = new JlBoolean("true");
        public static JlConstant False { get; } = new JlBoolean("false");
        public override JlValue Copy() => this;
        public override string ToString() => _value;

        private class JlBoolean : JlConstant {
            internal JlBoolean(string value) : base(value){}
        }
    }

    public class JlInteger : JlValue
    {
        public int Value { get; }
        public JlInteger(int value) { Value = value; }
        public override JlValue Copy() { return new JlInteger(Value); }
        public override string ToString() => Value.ToString();
        public JlInteger Add(JlInteger other) => new JlInteger(Value + other.Value);
        public JlInteger Subtract(JlInteger other) => new JlInteger(Value - other.Value);
        public JlInteger Multiply(JlInteger other) => new JlInteger(Value * other.Value);
        public JlInteger Divide(JlInteger other) => new JlInteger(Value / other.Value);
        public JlConstant LessThan(JlInteger other) => Value < other.Value ? JlConstant.True : JlConstant.False;
        public JlConstant LessThanEqual(JlInteger other) => Value <= other.Value ? JlConstant.True : JlConstant.False;
        public JlConstant GreaterThan(JlInteger other) => Value > other.Value ? JlConstant.True : JlConstant.False;
        public JlConstant GreaterThanEqual(JlInteger other) => Value >= other.Value ? JlConstant.True : JlConstant.False;

        public static JlInteger operator +(JlInteger j1, JlInteger j2) => j1?.Add(j2);
        public static JlInteger operator -(JlInteger j1, JlInteger j2) => j1?.Subtract(j2);
        public static JlInteger operator *(JlInteger j1, JlInteger j2) => j1?.Multiply(j2);
        public static JlInteger operator /(JlInteger j1, JlInteger j2) => j1?.Divide(j2);

        public static JlConstant operator >(JlInteger j1, JlInteger j2) => j1?.GreaterThan(j2);
        public static JlConstant operator >=(JlInteger j1, JlInteger j2) => j1?.GreaterThanEqual(j2);
        public static JlConstant operator <(JlInteger j1, JlInteger j2) => j1?.LessThan(j2);
        public static JlConstant operator <=(JlInteger j1, JlInteger j2) => j1?.LessThanEqual(j2);

        public static implicit operator JlList(JlInteger ji) => new JlList( ji ); 

    }
    public class JlSymbol : JlValue
    {
        public string Name { get; }
        public JlSymbol(string value)  { Name = value; }
        public override string ToString() => Name;
        public override JlValue Copy() => this;
    }

    public class JlString : JlValue
    {
        public string Value { get; }
        public JlString(string value)  { Value = value; }
        public override JlValue Copy() => new JlString(Value);
        public override string ToString() => Value;

        public override string ToString(bool printReadable) {
            if ( printReadable ) {
                return $"\"{Value.Replace( "\\", "\\\\" ).Replace( "\n", "\\n" )}\"";
            }
            return Value;
        }

        public static implicit operator JlInteger(JlString s) => new JlInteger(Convert.ToInt32( s.Value ));
    }

    public class JlList : JlValue
    {
        public string _start = "(", _end = ")";
        public List<JlValue> Value { get; }

        public JlList(IEnumerable<JlValue> list) {
            Value = list == null ? new List<JlValue>() : new List<JlValue>(list);
        }
        public JlList(params JlValue[] list ) {
            Value = new List<JlValue>(list);
        }
        public override bool ListQ() => true;
        public override string ToString() => ToString(true);
        public override string ToString(bool printReadably) => $"{_start}{Printer.Join(Value, " ", printReadably)}{_end}";

        public override JlValue Copy() => new JlList(Value.Select(v => v.Copy()));

        public JlList ConjBANG(params JlValue[] jvs)
        {
            Value.AddRange(jvs);
            return this;
        }

        public int Size => Value.Count;
        public JlValue Nth(int idx) => Value[idx];
        public JlValue this[int idx] => Value[idx];
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
        public static implicit operator JlList(JlInteger ji) => new JlList(ji);


    }

    public class JlVector : JlList
    {
        public JlVector(IEnumerable<JlValue> val = null) : base(val) {
            _start = "["; _end = "]";
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
        public Dictionary<string, JlValue> Value { get; }

        public JlHashMap(Dictionary<string, JlValue> value) {
            Value = value;
        }
        public JlHashMap(JlList list)
        {
            Value = list.Value.Where((v, i) => i % 2 == 0)
                .Zip(list.Value.Where((v, i) => i % 2 == 1),
                    (j1, j2) => new {Key=j1.ToString(), Value=j2}).ToDictionary(kv => kv.Key,kv => kv.Value);
        }
        public override string ToString() => ToString(true);
        public override string ToString(bool printReadably) => $"{{{Printer.Join(Value, " ", printReadably)}}}";
        public override JlValue Copy() => new JlHashMap(Value.ToDictionary(v => v.Key, v => v.Value.Copy()));
        public JlHashMap ConjBANG(params JlValue[] jvs) {
            foreach (var value in jvs)
                Value.Add(value.ToString(), value);
            return this;
        }
    }

    public class JlFunction : JlValue
    {
        private readonly Func<JlList, JlValue> _func;
        public JlValue Ast { get; }
        public Env Env { get; }
        public JlList FParams { get; }
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

        public override JlValue Copy() => new JlFunction(Ast.Copy(), Env, (JlList)FParams.Copy(), _func){_macro = _macro};
        public Env GenEnv(JlList args) => new Env( Env, FParams, args );
        public override string ToString() {
            if ( Ast != null )
                return $"<fn* {Printer.PrintStr( FParams, true )}>";
            if(_func != null)
                return $"<builtin_lambda {_func}>";
            return $"<builtin_class {GetType().Name}>";
        }
        public void SetMacro() { _macro = true; }
        private bool _macro;
    }
}
