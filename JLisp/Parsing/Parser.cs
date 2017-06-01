using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace JLisp.Parsing
{
    public class Parser
    {
        private readonly Tokinizer _tokinizer;
        private static Parser _parser;

        private Parser()
        {
            _tokinizer = Tokinizer.GetInstance();
        }

        public Expression Parse(string parseString)
        {
            DebugDisplay(parseString);
            var tokens = _tokinizer.Tokenize(parseString);
            var rpn = BuildAST(tokens.GetEnumerator());
            return rpn.First();

        }



        private void DebugDisplay(string parseString)
        {
            if (false)
            {
                var tokens = _tokinizer.Tokenize(parseString).ToList();
                Console.WriteLine("Tokenized List\n-------------------");
                tokens.ForEach(Console.WriteLine);
            }
            if (false)
            {
                var tokens = _tokinizer.Tokenize(parseString).ToList();
                var rpn = BuildAST(tokens.GetEnumerator()).ToList();
                Console.WriteLine("\nRPN List\n-------------------");
                rpn.ForEach(Console.WriteLine);
            }
        }

        private IEnumerable<Expression> BuildAST(IEnumerator<Token> en)
        {
            Queue<Expression> queue = new Queue<Expression>();
            while (en.MoveNext())
            {
                var token = en.Current;
                Expression ex = null;
                string id = token.Identity;
                switch (token.TokenType)
                {
                    case TokenType.Number:
                        ex = new NumericExpression(id);
                        break;
                    case TokenType.Operator:
                        ex = new OperatorExpression(id);
                        break;
                    case TokenType.Character:
                        switch (id[0])
                        {
                            case '(':
                                var ex2 = BuildAST(en).ToList();
                                if (!ex2.Any()) {
                                    var se = new SExpression();
                                    queue.Enqueue(se);
                                }
                                else if (ex2.Count == 1) {
                                    queue.Enqueue(ex2[0]);
                                }
                                else if (ex2[0] is OperatorExpression oe) {
                                    oe.ExpressionList.AddRange(ex2.Skip(1));
                                    var se = new SExpression(oe);
                                    queue.Enqueue(se);
                                }

                                continue;
                            case ')':
                                while (queue.Count > 0)
                                    yield return queue.Dequeue();
                                yield break;
                        }
                        break;
                    case TokenType.EOF:
                        while (queue.Count > 0)
                            yield return queue.Dequeue();
                        yield break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                yield return ex;
            }
        }

        public static Parser GetInstance()
        {
            return _parser ?? (_parser = new Parser());
        }
    }

}
