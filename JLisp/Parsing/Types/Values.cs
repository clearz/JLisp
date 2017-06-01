
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace JLisp.Parsing.Types
{

    public abstract class JlValue
    {
        public virtual string ToString(bool printReadably) => ToString();
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
        public JlInteger Subtract(JlInteger other) => new JlInteger(Value + other.Value);
        public JlInteger Multiply(JlInteger other) => new JlInteger(Value + other.Value);
        public JlInteger Divide(JlInteger other) => new JlInteger(Value + other.Value);
        public JlConstant LessThan(JlInteger other) => Value < other.Value ? JlConstant.True : JlConstant.False;
        public JlConstant LessThanEqual(JlInteger other) => Value <= other.Value ? JlConstant.True : JlConstant.False;
        public JlConstant GreaterThan(JlInteger other) => Value > other.Value ? JlConstant.True : JlConstant.False;
        public JlConstant GreaterThanEqual(JlInteger other) => Value >= other.Value ? JlConstant.True : JlConstant.False;
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
        public override string ToString() => $"\"{Value}\"";
    }

    public class JlList : JlValue
    {
        public string start = "(", end = ")";
        public List<JlValue> Value { get; }

        public JlList(IEnumerable<JlValue> list) {
            Value = list == null ? new List<JlValue>() : new List<JlValue>(list);
        }
        public JlList(params JlValue[] list ) {
            Value = new List<JlValue>(list);
        }
        public override string ToString() => ToString(true);
        public override string ToString(bool printReadably) => $"{start}{Printer.Join(Value, " ", printReadably)}{end}";

        public JlList Copy() => (JlList)this.MemberwiseClone();

        public JlList ConjBANG(params JlValue[] jvs)
        {
            Value.AddRange(jvs);
            return this;
        }
    }

    public class JlVector : JlList
    {
        public JlVector(IEnumerable<JlValue> val = null) : base(val) {
            start = "[";
            end = "]";
        }

        public new JlVector Copy() => (JlVector)this.MemberwiseClone();
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
}
