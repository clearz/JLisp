
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace JLisp.Parsing.Types
{

    public abstract class JlValue
    {
        public virtual string ToString(bool printReadably) => ToString();
        public override string ToString() => "<unknown>";
        public virtual bool ListQ() => false;

        public static bool _EqualQ(JlValue a, JlValue b) {
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null)) return false;
            if (ReferenceEquals(a, b)) return true;
            Type ota = a.GetType(), otb = b.GetType();
            if ( !((ota == otb) || (a is JlList && b is JlList)) ) return false;
            else {
                if ( a is JlInteger ji )
                    return ji.Value == ((JlInteger)b).Value;
                if ( a is JlSymbol js )
                    return js.Name == ((JlSymbol)b).Name;
                if ( a is JlString jt )
                    return jt.Value == ((JlString)b).Value;
                if ( a is JlList lista ) {
                    var listb = (JlList)b;
                    if ( lista.Size() != listb.Size() ) return false;
                    for ( int i = 0; i < lista.Size(); i++ ) {
                        if ( !_EqualQ( lista[i], listb[i] ) )
                            return false;
                    }
                    return true;
                }
            }
            return false;
        }

        public static bool operator ==(JlValue a, JlValue b) => _EqualQ(a, b);
        public static bool operator !=(JlValue a, JlValue b) => !_EqualQ(a, b);
    }

    public class JlConstant : JlValue
    {
        private readonly string _value;
        private JlConstant(string value) { _value = value; }
        public static JlConstant Nil { get; } = new JlConstant("nil");
        public static JlConstant True { get; } = new JlConstant("true");
        public static JlConstant False { get; } = new JlConstant("false");
        public override string ToString() => _value.ToString();
    }

    public class JlInteger : JlValue
    {
        public int Value { get; }
        public JlInteger(int value) { Value = value; }
        public JlInteger Copy() { return this; }
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

    }
    public class JlSymbol : JlValue
    {
        public string Name { get; }
        public JlSymbol(string value)  { Name = value; }
        public override string ToString() => Name;
        public JlSymbol Copy() => this;
    }

    public class JlString : JlValue
    {
        public string Value { get; }
        public JlString(string value)  { Value = value; }
        public JlString Copy() => this;
        public override string ToString() => Value;

        public override string ToString(bool printReadable) {
            if ( printReadable ) {
                return $"\"{Value.Replace( "\\", "\\\\" ).Replace( "\n", "\\n" )}\"";
            }
            return Value;
        }
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

        public JlList Copy() => (JlList)this.MemberwiseClone();

        public JlList ConjBANG(params JlValue[] jvs)
        {
            Value.AddRange(jvs);
            return this;
        }

        public int Size() => Value.Count;
        public JlValue Nth(int idx) => Value[idx];
        public JlValue this[int idx] => Value[idx];
        public JlList Rest()
        {
            if(Size() > 0) return new JlList(Value.GetRange(1, Size() -1));
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


    }

    public class JlVector : JlList
    {
        public JlVector(IEnumerable<JlValue> val = null) : base(val) {
            _start = "[";
            _end = "]";
        }

        public new JlVector Copy() => (JlVector)this.MemberwiseClone();
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
        public JlHashMap(JlList list) {
            Value = list.Value.ToDictionary(jv => jv.ToString());
        }
        public override string ToString() => ToString(true);
        public override string ToString(bool printReadably) => $"{{{Printer.Join(Value, " ", printReadably)}}}";
        public JlHashMap Copy() => (JlHashMap)this.MemberwiseClone();
        public JlHashMap ConjBANG(params JlValue[] jvs) {
            foreach (var value in jvs)
                Value.Add(value.ToString(), value);
            return this;
        }
    }

    public class JlFunction : JlValue
    {
        private readonly Func<JlList, JlValue> _func = null;
        public JlFunction(Func<JlList, JlValue> func) { _func = func; }
        public JlValue Apply(JlList args) { return _func( args ); }

        public override string ToString() => this.GetType().Name;
    }
}
