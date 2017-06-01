using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace JLisp.Parsing
{
    public abstract class Expression
    {
        public abstract Expression Eval();
    }

    class NumericExpression : Expression
    {
        private readonly long _value;

        public NumericExpression(string value)
        {
            _value = Convert.ToInt64(value);
        }
        private NumericExpression(long value)
        {
            _value = value;
        }

        public static NumericExpression operator +(NumericExpression e1, NumericExpression e2) => new NumericExpression(e1._value + e2._value);
        public static NumericExpression operator -(NumericExpression e1, NumericExpression e2) => new NumericExpression(e1._value - e2._value);
        public static NumericExpression operator *(NumericExpression e1, NumericExpression e2) => new NumericExpression(e1._value * e2._value);
        public static NumericExpression operator /(NumericExpression e1, NumericExpression e2) => new NumericExpression(e1._value / e2._value);
        public static NumericExpression operator %(NumericExpression e1, NumericExpression e2) => new NumericExpression(e1._value % e2._value);
        public override Expression Eval() => this;
        public override string ToString() => _value.ToString();
        
    }

    class OperatorExpression : Expression, IComposite
    {
        protected readonly string _value;
        public List<Expression> ExpressionList { get; set; } = new List<Expression>();
        public OperatorExpression(string value)
        {
            _value = value;
        }

        public override Expression Eval()
        {
            if (!ExpressionList.Any()) return this;
            switch (_value)
            {
                case "+":
                    return ExpressionList.Skip(1).Aggregate((NumericExpression)ExpressionList.First().Eval(), (s, s1) => s + (NumericExpression)s1.Eval());
                case "-":
                    if(ExpressionList.Count == 1)
                        return ExpressionList.Aggregate(new NumericExpression("0"), (s, s1) => s - (NumericExpression)s1.Eval());
                    return ExpressionList.Skip(1).Aggregate((NumericExpression)ExpressionList.First().Eval(), (s, s1) => s -(NumericExpression) s1.Eval());
                case "*":
                    return ExpressionList.Skip(1).Aggregate((NumericExpression)ExpressionList.First().Eval(), (s, s1) => s * (NumericExpression)s1.Eval());
                case "/":
                    return ExpressionList.Skip(1).Aggregate((NumericExpression)ExpressionList.First().Eval(), (s, s1) => s / (NumericExpression)s1.Eval());
                case "%":
                    return ExpressionList.Skip(1).Aggregate((NumericExpression)ExpressionList.First().Eval(), (s, s1) => s % (NumericExpression)s1.Eval());
            }

            throw new ArgumentException();
        }

        public override string ToString() => $"{_value}";
    }

    internal class SExpression : Expression
    {
        protected readonly Expression _expresssion;

        public SExpression(Expression value = default(Expression))
        {
            _expresssion = value;
        }

        public override string ToString() => _expresssion?.ToString() ?? "()";

        public override Expression Eval()
        {
            return (_expresssion?.Eval() ?? this);
        }
    }

    internal interface IComposite // - (* 10 10) (+ 1 1 1)
    {
        List<Expression> ExpressionList { get; set; }
    }
}
