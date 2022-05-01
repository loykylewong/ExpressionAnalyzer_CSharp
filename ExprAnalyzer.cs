using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ExpressionAnalyzer
{
    using T = Double;   // Generic <T> does not support operators, or constraint such as "where T : IArithmatcOperators"
    public class MonDictionary<TKey, TValue> where TKey : notnull
    {
        #nullable enable
        private Dictionary<TKey, TValue> dict;
        public struct EventArg
        {
            public TKey? Key; public TValue? Value;
            public EventArg(TKey? k, TValue? v) { this.Key = k; this.Value = v; }
        }
        public delegate void EventHandler(object sender, EventArg e);
        public event EventHandler? KeyValuePairAdded;
        public event EventHandler? KeyValuePairRemoved;
        public event EventHandler? ContentsCleared;
        public event EventHandler? ValueChanged;
        public MonDictionary() { dict = new Dictionary<TKey, TValue>(); }
        public MonDictionary(int capacity) { dict = new Dictionary<TKey, TValue>(capacity); }
        public MonDictionary(Dictionary<TKey, TValue> dict) { this.dict = dict; }
        public void Add(TKey key, TValue value)
        {
            dict.Add(key, value);
            KeyValuePairAdded?.Invoke(this, new EventArg(key, value));
        }
        public bool Remove(TKey key)
        {
            TValue value = dict[key];
            bool ret = dict.Remove(key);
            KeyValuePairRemoved?.Invoke(this, new EventArg(key, value));
            return ret;
        }
        public void Clear()
        {
            dict.Clear();
            ContentsCleared?.Invoke(this, new EventArg(default, default));
        }
        public int Count { get { return dict.Count; } }
        public TValue this[TKey key]
        {
            get { return dict[key]; }
            set { TValue val = dict[key]; dict[key] = value; ValueChanged?.Invoke(this, new EventArg(key, val)); }
        }
        public bool TryGetValue(TKey key, out TValue? value) { return dict.TryGetValue(key, out value); }
        public bool ContainsKey(TKey key) { return dict.ContainsKey(key); }
        public bool ContainsValue(TValue value) { return dict.ContainsValue(value); }
        //public Dictionary<TKey, TValue>.KeyCollection Keys { get { return dict.Keys; } }
        //public Dictionary<TKey, TValue>.ValueCollection Values { get { return dict.Values; } }
        public Dictionary<TKey, TValue>.Enumerator GetEnumerator() { return dict.GetEnumerator(); }
        public override string? ToString() { return dict.ToString(); }
    }
    class ExprAnalyzer
    {
        #region construction
        public ExprAnalyzer()
        {
            this.expr = String.Empty;
            this.Variables = new MonDictionary<string, T>();
            regEvents();
        }
        public ExprAnalyzer(string expr)
        {
            this.expr = expr;
            this.Variables = new MonDictionary<string, T>();
            regEvents();
        }
        public ExprAnalyzer(string expr, MonDictionary<string, T> vars)
        {
            this.expr = expr;
            this.Variables = vars;
            regEvents();
        }
        public ExprAnalyzer(string expr, string vars)
        {
            this.expr = expr;
            this.Variables = new MonDictionary<string, T>();
            this.AddVariables(vars);
            regEvents();
        }
        private void regEvents()
        {
            this.Variables.ContentsCleared += (object sender, MonDictionary<string, T>.EventArg e) => this.analysed = false;
            this.Variables.ValueChanged += (object sender, MonDictionary<string, T>.EventArg e) => this.analysed = false;
            this.Variables.KeyValuePairRemoved += (object sender, MonDictionary<string, T>.EventArg e) => this.analysed = false;
        }
        #endregion
        #region fields
        public readonly MonDictionary<string, T> Variables;
        private string expr;
        // private variableStr[] varTable = new variableStr[0];
        private T value = 0;
        private bool analysed = false;
        private readonly Stack<string> oprStack = new Stack<string>();
        private readonly Queue<string> varQueue = new Queue<string>();
        private const string varStr = @"[a-zA-Z_]+\w*";
        private const string numStr = @"([\+\-]?\d+\.?\d*|\.\d+)([eE][\+\-]?\d+)?";
        private const string funStr = @"trunc|tanh|tan|sqrt|sinh|sin|sign|round|rem|pow|min|max|log10|log|ln|floor|exp|cosh|cos|ceil|atan2|atan|asin|acos|abs|sat|if";
        private const string oprStr = @"\(|\)|\+|\-|\*|/|%|&&|&|\|\||\||!|~|\^|\,|(<=)|(>=)|(==)|<|>|" + funStr;
        private Regex varRegex = new Regex(varStr);
        private Regex numRegex = new Regex(numStr);
        private Regex oprRegex = new Regex(oprStr);
        // private Regex validR = new Regex(@"(" + oprStr + @")|(" + numStr + @")|(" + varStr + @")");
        #endregion
        #region properties
        public T Value
        {
            get
            {
                if (this.analysed)
                {
                    return this.value;
                }
                else
                {
                    this.analyse();
                    this.analysed = true;
                    return this.value;
                }
            }
        }
        public string Expression
        {
            get
            {
                return this.expr;
            }
            set
            {
                this.expr = value;
                this.analysed = false;
            }
        }
        #endregion
        #region methods
        private void analyse()
        {
            this.varQueue.Clear();
            this.oprStack.Clear();
            if (this.expr.Length == 0)
            {
                this.value = 0.0;
                this.analysed = true;
                return;
            }
            string S;
            Match M;
            bool numBefore = false;
            int idx = 0;
            do
            {
                while (this.expr[idx] == ' ')
                    idx++;
                if ((M = this.numRegex.Match(this.expr, idx)).Success && M.Index == idx)
                {
                    S = M.Value;
                    if (numBefore)
                        dealOpr("+");
                    varQueue.Enqueue(S);
                    numBefore = true;
                }
                else if ((M = this.oprRegex.Match(this.expr, idx)).Success && M.Index == idx)
                {
                    S = M.Value;
                    if (S == ",")
                    {
                        dealOpr(")");
                        dealOpr("(");
                        numBefore = false;
                    }
                    else
                    {
                        dealOpr(S);
                        if (S == ")")
                            numBefore = true;
                        else
                            numBefore = false;
                    }
                }
                else if ((M = this.varRegex.Match(this.expr, idx)).Success && M.Index == idx)
                {
                    S = M.Value;
                    varQueue.Enqueue(this.variableValue(S));
                    numBefore = true;
                }
                else
                    throw (new Exception("ExprAnalyzer: Invalid Expression"));
                idx += M.Length;
            } while (idx < this.expr.Length);
            while (this.oprStack.Count > 0)
                this.varQueue.Enqueue(this.oprStack.Pop());
            this.calculate();
        }
        private string variableValue(string name)
        {
            try
            {
                return Variables[name].ToString();
            }
            catch (Exception e)
            {
                throw new Exception("ExprAnalyzer: Can't find variable:\"" + name + "\" in variable table.", e);
            }
        }
        private void dealOpr(string opr)
        {
            if (opr == "(")
                this.oprStack.Push(opr);
            else if (opr == ")")
            {
                try
                {
                    while (this.oprStack.Peek() != "(")
                    {
                        this.varQueue.Enqueue(this.oprStack.Pop());
                    }
                    this.oprStack.Pop();
                }
                catch (InvalidOperationException)
                {
                    throw (new Exception("ExprAnalyzer: brackets does not match."));
                }
            }
            else
            {
                while (this.oprStack.Count != 0 && this.oprPriority(this.oprStack.Peek()) >= this.oprPriority(opr))
                {
                    this.varQueue.Enqueue(this.oprStack.Pop());
                }
                this.oprStack.Push(opr);
            }
        }
        private int oprPriority(string opr) // larger is higher
        {
            switch (opr)
            {
                case "(":
                case ")":
                    return 0;
                case "~":
                case "!":
                    return 90;
                case "*":
                case "/":
                case "%":
                    return 80;
                case "+":
                case "-":
                    return 70;
                case ">":
                case ">=":
                case "<":
                case "<=":
                case "==":
                    return 60;
                case "&":
                    return 50;
                case "^":
                    return 45;
                case "|":
                    return 40;
                case "&&":
                    return 35;
                case "||":
                    return 30;
                default:
                    return 100;
            }
        }
        private void calculate()
        {
            Stack<T> Num = new Stack<T>();
            Match M;
            string S;
            while (this.varQueue.Count > 0)
            {
                T RightParam;
                S = this.varQueue.Dequeue();
                if ((M = this.numRegex.Match(S)).Success && M.Index == 0 && M.Length == S.Length)
                    Num.Push(T.Parse(S));
                else if ((M = this.oprRegex.Match(S)).Success && M.Index == 0 && M.Length == S.Length)
                {
                    try
                    {
                        RightParam = Num.Pop();
                        switch (S)
                        {
                            case "+":
                                Num.Push(Num.Pop() + RightParam);
                                break;
                            case "-":
                                Num.Push(Num.Pop() - RightParam);
                                break;
                            case "*":
                                Num.Push(Num.Pop() * RightParam);
                                break;
                            case "/":
                                Num.Push(Num.Pop() / RightParam);
                                break;
                            case "%":
                                Num.Push(Num.Pop() % RightParam);
                                break;
                            case "<":
                                Num.Push(Num.Pop() < RightParam ? 1.0 : 0.0);
                                break;
                            case "<=":
                                Num.Push(Num.Pop() <= RightParam ? 1.0 : 0.0);
                                break;
                            case ">":
                                Num.Push(Num.Pop() > RightParam ? 1.0 : 0.0);
                                break;
                            case ">=":
                                Num.Push(Num.Pop() >= RightParam ? 1.0 : 0.0);
                                break;
                            case "==":
                                Num.Push(Num.Pop() == RightParam ? 1.0 : 0.0);
                                break;
                            case "&":
                                Num.Push(unchecked((long)Num.Pop()) & unchecked((long)RightParam));
                                break;
                            case "|":
                                Num.Push(unchecked((long)Num.Pop()) | unchecked((long)RightParam));
                                break;
                            case "~":
                                Num.Push(~unchecked((long)RightParam));
                                break;
                            case "^":
                                Num.Push(unchecked((long)Num.Pop()) ^ unchecked((long)RightParam));
                                break;
                            case "!":
                                Num.Push(RightParam == 0 ? 1.0 : 0.0);
                                break;
                            case "&&":
                                {
                                    bool bl = Num.Pop() != 0.0;
                                    bool br = RightParam != 0.0;
                                    Num.Push((bl && br) ? 1.0 : 0.0);
                                }
                                break;
                            case "||":
                                {
                                    bool bl = Num.Pop() != 0.0;
                                    bool br = RightParam != 0.0;
                                    Num.Push((bl || br) ? 1.0 : 0.0);
                                }
                                break;
                            case "abs":
                                Num.Push(Math.Abs(RightParam));
                                break;
                            case "acos":
                                Num.Push(Math.Acos(RightParam));
                                break;
                            case "asin":
                                Num.Push(Math.Asin(RightParam));
                                break;
                            case "atan":
                                Num.Push(Math.Atan(RightParam));
                                break;
                            case "atan2":
                                Num.Push(Math.Atan2(Num.Pop(), RightParam));
                                break;
                            case "ceil":
                                Num.Push(Math.Ceiling(RightParam));
                                break;
                            case "cos":
                                Num.Push(Math.Cos(RightParam));
                                break;
                            case "cosh":
                                Num.Push(Math.Cosh(RightParam));
                                break;
                            case "exp":
                                Num.Push(Math.Exp(RightParam));
                                break;
                            case "floor":
                                Num.Push(Math.Floor(RightParam));
                                break;
                            case "rem":
                                Num.Push(Math.IEEERemainder(Num.Pop(), RightParam));
                                break;
                            case "ln":
                                Num.Push(Math.Log(RightParam));
                                break;
                            case "log":
                                Num.Push(Math.Log(Num.Pop(), RightParam));
                                break;
                            case "log10":
                                Num.Push(Math.Log10(RightParam));
                                break;
                            case "max":
                                Num.Push(Math.Max(Num.Pop(), RightParam));
                                break;
                            case "min":
                                Num.Push(Math.Min(Num.Pop(), RightParam));
                                break;
                            case "pow":
                                Num.Push(Math.Pow(Num.Pop(), RightParam));
                                break;
                            case "round":
                                Num.Push(Math.Round(RightParam));
                                break;
                            case "sign":
                                Num.Push(Math.Sign(RightParam));
                                break;
                            case "sin":
                                Num.Push(Math.Sin(RightParam));
                                break;
                            case "sinh":
                                Num.Push(Math.Sinh(RightParam));
                                break;
                            case "sqrt":
                                Num.Push(Math.Sqrt(RightParam));
                                break;
                            case "tan":
                                Num.Push(Math.Tan(RightParam));
                                break;
                            case "tanh":
                                Num.Push(Math.Tanh(RightParam));
                                break;
                            case "trunc":
                                Num.Push(Math.Truncate(RightParam));
                                break;
                            case "sat":
                                {
                                    T l = RightParam;
                                    T h = Num.Pop();
                                    T v = Num.Pop();
                                    if (h > l)
                                        v = v < l ? l : v > h ? h : v;
                                    else if (h < l)
                                        v = v < h ? h : v > l ? l : v;
                                    else
                                        v = l;
                                    Num.Push(v);
                                }
                                break;
                            case "if":
                                {
                                    T fv = RightParam;
                                    T tv = Num.Pop();
                                    T b = Num.Pop();
                                    if (b == 0)
                                        Num.Push(fv);
                                    else
                                        Num.Push(tv);
                                }
                                break;
                            default:
                                throw new InvalidOperationException("ExprAnalyzer: Unsupported function \"" + S + "\".");
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        throw (new Exception("ExprAnalyzer: Invalid Expression."));
                    }
                }
                else
                    throw (new Exception("for debug: program logic ERROR"));
            }
            this.value = Num.Pop();
            if (Num.Count != 0)
                throw (new Exception("ExprAnalyzer: Invalid Expression"));
        }
        public void AddVariables(string variableList)
        {
            Match M = Regex.Match(variableList, @"(?<Name>" + varStr + @")\s*=\s*(?<Value>" + numStr + @")");
            for (; M.Success; M = M.NextMatch())
            {
                T v = T.Parse(M.Result("${Value}"));
                this.Variables.Add(M.Result("${Name}"), v);
            }
        }
        public void Invalidate()
        {
            this.analysed = false;
        }
        #endregion
    }
}
