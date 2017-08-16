using System;
using System.Collections.Generic;
using System.Linq;
using JLisp.Parsing;
using JLisp.Parsing.Functions;
using JLisp.Parsing.Types;
using static JLisp.Parsing.Types.JlConstant;

namespace JLisp
{
    public class Evaluator
    {
        public static Env EnvRoot { get; private set; }
        static bool IsPair(JlValue x) => x is JlList && ((JlList)x).Count > 0;

        static JlValue QuasiQuote(JlValue ast)
        {
            if (!IsPair(ast)) return new JlList(new JlSymbol("quote"), ast);

            JlValue a0 = ((JlList)ast)[0];
            if (a0 is JlSymbol s && s.Name == "unquote") return ((JlList)ast)[1];
            if (IsPair(a0) && ((JlList)a0)[0] is JlSymbol s1 && s1.Name == "splice-unquote")
                return new JlList(new JlSymbol("concat"), ((JlList)a0)[1], QuasiQuote(((JlList)ast).GetTail()));
            return new JlList(new JlSymbol("cons"), QuasiQuote(a0), QuasiQuote(((JlList)ast).GetTail()));
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

        static JlValue MacroExpand(JlValue ast, Env env)
        {
            while (IsMacroCall(ast, env))
            {
                JlSymbol a0 = (JlSymbol)((JlList)ast)[0];
                JlFunction mac = (JlFunction)env.Get(a0.Name);
                ast = mac.Invoke(((JlList)ast).GetTail());
            }
            return ast;
        }

        static JlValue EvalAst(JlValue ast, Env env)
        {
            if (ast is JlSymbol sym) return env.Get(sym.Name);
            if (ast is JlList oldLst)
            {
                var newLst = ast.IsList ? new JlList() : new JlVector();
                foreach (var jv in oldLst.Value)
                    newLst.AddRange(InnerEvaluate(jv, env));
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
                if (!origAst.IsList) return EvalAst(origAst, env);
                JlValue expanded = MacroExpand(origAst, env);
                if (!expanded.IsList) return EvalAst(expanded, env);

                var ast = (JlList)expanded;
                if (ast.Count == 0) return ast;
                a0 = ast[0];
                string a0Sym = a0 is JlSymbol sym ? sym.Name : "__<*fn*>__";

                switch (a0Sym)
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
                        for (int i = 0; i < ((JlList)a1).Count; i += 2)
                        {
                            var key = (JlSymbol)((JlList)a1)[i];
                            var val = ((JlList)a1)[i + 1];
                            letEnv.Set(key, InnerEvaluate(val, letEnv));
                        }
                        origAst = a2;
                        env = letEnv;
                        break;
                    case "quote":
                        return ast[1];
                    case "quasiquote":
                        origAst = QuasiQuote(ast[1]);
                        break;
                    case "defmacro!":
                        a1 = ast[1];
                        a2 = ast[2];
                        res = InnerEvaluate(a2, env);
                        ((JlFunction)res).IsMacro = true;
                        env.Set(((JlSymbol)a1), res);
                        return res;
                    case "macroexpand":
                        a1 = ast[1];
                        return MacroExpand(a1, env);
                    case "try*":
                        try
                        {
                            return InnerEvaluate(ast[1], env);
                        }
                        catch (Exception e)
                        {
                            if (ast.Count > 2)
                            {
                                JlValue exc;
                                a2 = ast[2];
                                JlValue a20 = ((JlList) a2)[0];
                                if (((JlSymbol) a20).Name == "catch*")
                                {
                                    if (e is JlException ex)
                                        exc = ex.Value;
                                    else
                                        exc = e.StackTrace;
                                    return InnerEvaluate(((JlList) a2)[2],
                                        new Env(env, ((JlList) a2).Slice(1, 2),
                                            new JlList(exc)));
                                }
                            }
                            throw e;
                        }
                    case "do":
                        EvalAst(ast.GetTail(), env);
                        origAst = ast[ast.Count - 1];
                        break;
                    case "if":
                        a1 = ast[1];
                        var cond = InnerEvaluate(a1, env);
                        if (cond == Nil || cond == False)
                        {
                            if (ast.Count > 3)
                                origAst = ast[3];
                            else return Nil;
                        }
                        else origAst = ast[2];
                        break;
                    case "fn*":
                        var a1F = (JlList)ast[1];
                        var a2F = ast[2];
                        return new JlFunction(a2F, env, a1F,
                            args => InnerEvaluate(a2F, new Env(env, a1F, args)));
                    default:
                        el = (JlList)EvalAst(ast, env);
                        if (el[0] is JlFunction f)
                        {
                            var fnast = f.Ast;
                            if (fnast != null)
                            {
                                origAst = fnast;
                                env = f.CreateChildEnv(el.GetTail());
                            }
                            else
                                return f.Invoke(el.GetTail());
                        }
                        else throw new ParseError($"Unknown Type Got '{el}'");
                        break;
                }
            }
        }

        public static JlValue Eval(string str)
        {
            return InnerEvaluate(Reader.ReadStr(str), EnvRoot);
        }

        static Evaluator()
        {
            Init();
        }
        private static void Init() {

            EnvRoot = new Env(null);
            foreach (var entry in Core.Ns)
                EnvRoot.Set(entry.Key, entry.Value);
            EnvRoot.Set("eval", new JlFunction(a => InnerEvaluate(a[0], EnvRoot)));
            EnvRoot.Set("fmt", new JlFunction(
                a => string.Format(((JlString)a[0]).Value, a.Value.Skip(1).ToArray())));
            EnvRoot.Set("typeof", new JlFunction(a => a[0].GetType().Name));


            Eval("(def! *host-language* \"C#\")");
            Eval("(def! not (fn* (a) (if a false true)))");
            Eval("(def! load-file (fn* (f) (eval (read-string (str \"(do \" (slurp f) \")\")))))");
            Eval("(defmacro! cond (fn* (& xs) (if (> (count xs) 0) (list 'if (first xs) (if (> (count xs) 1) (nth xs 1) (throw \"odd number of forms to cond\")) (cons 'cond (rest (rest xs)))))))");
            Eval("(def! *gensym-counter* (atom 0))");
            Eval("(def! gensym (fn* [] (symbol (str \"G__\" (swap! *gensym-counter* (fn* [x] (+ 1 x)))))))");
            Eval("(defmacro! or (fn* (& xs) (if (empty? xs) nil (if (= 1 (count xs)) (first xs) (let* (condvar (gensym)) `(let* (~condvar ~(first xs)) (if ~condvar ~condvar (or ~@(rest xs)))))))))");

        }


        public static void Reset()
        {
            Init();
        }
    }
}
