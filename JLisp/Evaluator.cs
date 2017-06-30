using System.Collections.Generic;
using System.IO;
using System.Linq;
using JLisp.Parsing;
using JLisp.Parsing.Functions;
using JLisp.Parsing.Types;
using static JLisp.Parsing.Types.JlConstant;

namespace JLisp
{
    public class Evaluator
    {
        private static Env ENV_ROOT { get; }
        static bool IsPair(JlValue x) => x is JlList && ((JlList)x).Size > 0;

        static JlValue QuasiQuote(JlValue ast)
        {
            if (!IsPair(ast)) return new JlList(new JlSymbol("quote"), ast);

            JlValue a0 = ((JlList)ast)[0];
            if (a0 is JlSymbol s && s.Name == "unquote") return ((JlList)ast)[1];
            if (IsPair(a0) && ((JlList)a0)[0] is JlSymbol s1 && s1.Name == "splice-unquote")
                return new JlList(new JlSymbol("concat"), ((JlList)a0)[1], QuasiQuote(((JlList)ast).Rest()));
            return new JlList(new JlSymbol("cons"), QuasiQuote(a0), QuasiQuote(((JlList)ast).Rest()));
        }

        static bool IsMacroCall(JlValue ast, Env env)
        {
            if (ast is JlList lst)
            {
                JlValue a0 = lst[0];
                if (a0 is JlSymbol s && env.Find(s.Name) != null)
                {
                    JlValue mac = env.Get(s.Name);
                    return mac is JlFunction f && f.IsMacro;
                }
            }
            return false;
        }

        static JlValue MicroExpand(JlValue ast, Env env)
        {
            while (IsMacroCall(ast, env))
            {
                JlSymbol a0 = (JlSymbol)((JlList)ast)[0];
                JlFunction mac = (JlFunction)env.Get(a0.Name);
                ast = mac.Apply(((JlList)ast).Rest());
            }
            return ast;
        }

        static JlValue EvalAst(JlValue ast, Env env)
        {
            if (ast is JlSymbol sym) return env.Get(sym.Name);
            if (ast is JlList oldLst)
            {
                var newLst = ast.ListQ() ? new JlList() : new JlVector();
                foreach (var jv in oldLst.Value)
                    newLst.ConjBANG(InnerEvaluate(jv, env));
                return newLst;
            }
            if (ast is JlHashMap map)
            {
                var newDict = new Dictionary<string, JlValue>();
                foreach (var jv in map.Value)
                    newDict.Add(jv.Key, InnerEvaluate(jv.Value, env));

                return new JlHashMap(newDict);
            }
            return ast;
        }
        

        static JlValue InnerEvaluate(JlValue origAst, Env env)
        {
            JlValue a0, a1, a2;
            JlList el;
            while (true)
            {
                if (!origAst.ListQ()) return EvalAst(origAst, env);
                JlValue expanded = MicroExpand(origAst, env);
                if (!expanded.ListQ()) return expanded;

                var ast = (JlList)expanded;
                if (ast.Size == 0) return ast;
                a0 = ast[0];
                string a0sym = a0 is JlSymbol sym ? sym.Name : "__<*fn*>__";

                switch (a0sym)
                {
                    case "def!":
                        a1 = ast[1];
                        a2 = ast[2];
                        var res = InnerEvaluate(a2, env);
                        env.Set(((JlSymbol)a1).Name, res);
                        return res;
                    case "let*":
                        a1 = ast[1];
                        a2 = ast[2];
                        var letEnv = new Env(env);
                        for (int i = 0; i < ((JlList)a1).Size; i += 2)
                        {
                            var key = (JlSymbol)((JlList)a1)[i];
                            var val = ((JlList)a1)[i + 1];
                            letEnv.Set(key.Name, InnerEvaluate(val, letEnv));
                        }
                        return InnerEvaluate(a2, letEnv);
                    case "quote":
                        return ast[1];
                    case "quasiquote":
                        return InnerEvaluate(QuasiQuote(ast[1]), env);
                    case "defmacro!":
                        a1 = ast[1];
                        a2 = ast[2];
                        res = InnerEvaluate(a2, env);
                        ((JlFunction)res).SetMacro();
                        env.Set(((JlSymbol)a1).Name, res);
                        return res;
                    case "microexpand":
                        a1 = ast[1];
                        return MicroExpand(a1, env);
                    case "do":
                        EvalAst(ast.Rest(), env);
                        origAst = ast[ast.Size - 1];
                        break;
                    case "if":
                        a1 = ast[1];
                        var cond = InnerEvaluate(a1, env);
                        if (cond == Nil || cond == False)
                        {
                            if (ast.Size > 3)
                                origAst = ast[3];
                            else return Nil;
                        }
                        else origAst = ast[2];
                        break;
                    case "fn*":
                        var a1f = (JlList)ast[1];
                        var a2f = ast[2];
                        return new JlFunction(a2f, env, a1f,
                            args => InnerEvaluate(a2f, new Env(env, a1f, args)));
                    default:
                        el = (JlList)EvalAst(ast, env);
                        if (el[0] is JlFunction f)
                        {
                            var fnast = f.Ast;
                            if (fnast != null)
                            {
                                origAst = fnast;
                                env = f.GenEnv(el.Rest());
                            }
                            else
                                return f.Apply(el.Rest());
                        }
                        else throw new ParseError($"Expecting a Function Got '{el}'");
                        break;
                }
            }
        }

        public static Env Set(string key, JlValue value) => ENV_ROOT.Set(key, value);

        public static JlValue Eval(string str)
        {
            return InnerEvaluate(Reader.ReadStr(str), ENV_ROOT);
        }

        private static void _ref(Env env, string name, JlValue val) { env.Set(name, val); }

        static Evaluator()
        {
            ENV_ROOT = new Env(null);
            Init();
        }
        private static void Init() { 

            foreach (var entry in Core.Ns)
                _ref(ENV_ROOT, entry.Key, entry.Value);

            _ref(ENV_ROOT, "read-string", new JlFunction(
                a => Reader.ReadStr((((JlString)a[0]).Value))));
            _ref(ENV_ROOT, "eval", new JlFunction(
                a => InnerEvaluate(a[0], ENV_ROOT)));
            _ref(ENV_ROOT, "slurp", new JlFunction(
                a => new JlString(File.ReadAllText(((JlString)a[0]).Value))));
            _ref(ENV_ROOT, "fmt", new JlFunction(
                a => new JlString(string.Format(((JlString)a[0]).Value, a.Value.Skip(1).ToArray()))));

            Eval("(def! not (fn* [a] (if a false true)))");
            Eval("(def! load-file (fn* [f] (eval (read-string (str \"(do \" (slurp f) \")\")))))");
            Eval("(defmacro! cond (fn* (& xs) (if (> (count xs) 0) (list 'if (first xs) (if (> (count xs) 1) (nth xs 1) (throw \"odd number of forms to cond\")) (cons 'cond (rest (rest xs)))))))");
            Eval("(defmacro! or (fn* (& xs) (if (empty? xs) nil (if (= 1 (count xs)) (first xs) `(let* (or_FIXME ~(first xs)) (if or_FIXME or_FIXME (or ~@(rest xs))))))))");

        }


    }
}
