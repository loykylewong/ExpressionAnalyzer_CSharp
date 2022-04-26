using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ExpressAnalyzer
{
    class ExprAnalyzer
    {
        #region structs & enums
        public struct Variable
        {
            public string Name;
            public double Value;
            public Variable(string name, double value)
            {
                this.Name = name;
                this.Value = value;
            }
        }
        private struct variableStr
        {
            public string Name;
            public string Value;
            public variableStr(string name, string value)
            {
                this.Name = name;
                this.Value = value;
            }
        }
        #endregion
        #region construction
        public ExprAnalyzer()
        {
        }
        public ExprAnalyzer(string expressString, Variable[] variableTable)
        {
            this.express = expressString;
            this.SetVariableTable(variableTable);
        }
        public ExprAnalyzer(string expressString, string variableTable)
        {
            this.express = expressString;
            this.SetVariableTable(variableTable);
        }
        #endregion
        #region fields
        private string express = "";
        private variableStr[] varTable = new variableStr[0];
        private double value = 0;
        private bool analysed = false;
        private Stack<string> oprStack = new Stack<string>();
        private Queue<string> varQueue = new Queue<string>();
        private const string varStr = @"[a-zA-Z_]+\w*";
        private const string numStr = @"([\+\-]?\d+\.?\d*|\.\d+)([eE][\+\-]?\d+)?";
        private const string funStr = @"trunc|tanh|tan|sqrt|sinh|sin|sign|round|rem|pow|min|max|log10|log|ln|floor|exp|cosh|cos|ceiling|atan2|atan|asin|acos|abs";
        private const string oprStr = @"\(|\)|\+|\-|\*|/|%|&|\||~|\^|\,|" + funStr;
        private Regex varRegex = new Regex(varStr);
        private Regex numRegex = new Regex(numStr);
        private Regex oprRegex = new Regex(oprStr);
        private Regex validR = new Regex(@"(" + oprStr + @")|(" + numStr + @")|(" + varStr + @")");
        #endregion
        #region properties
        public double Value
        {
            get
            {
                if(this.analysed)
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
        public string Express
        {
            get
            {
                return this.express;
            }
            set
            {
                this.express = value;
                this.analysed = false;
            }
        }
        #endregion
        #region methods
        private void analyse()
        {
            this.varQueue.Clear();
            this.oprStack.Clear();
            if(this.express.Length == 0)
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
                while (this.express[idx] == ' ')
                    idx++;
                if ((M = this.numRegex.Match(this.express, idx)).Success && M.Index == idx)
                {
                    S = M.Value;
                    if (numBefore)
                        dealOpr("+");
                    varQueue.Enqueue(S);
                    numBefore = true;
                }
                else if ((M = this.oprRegex.Match(this.express, idx)).Success && M.Index == idx)
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
                else if ((M = this.varRegex.Match(this.express, idx)).Success && M.Index == idx)
                {
                    S = M.Value;
                    varQueue.Enqueue(this.variableValue(S));
                    numBefore = true;
                }
                else
                    throw (new Exception("ExprAnalyzer: Invalid Expression"));
                idx += M.Length;
            } while (idx < this.express.Length);
            while (this.oprStack.Count > 0)
                this.varQueue.Enqueue(this.oprStack.Pop());
            this.calculate();
        }
        private string variableValue(string name)
        {
            foreach(variableStr v in this.varTable)
            {
                if(v.Name == name)
                    return v.Value;
            }
            throw (new Exception("ExprAnalyzer: Can't find variable:\"" + name + "\" in variable table."));
        }
        private void dealOpr(string opr)
        {
            if(opr == "(")
                this.oprStack.Push(opr);
            else if(opr == ")")
            {
                try
                {
                    while (this.oprStack.Peek() != "(")
                    {
                        this.varQueue.Enqueue(this.oprStack.Pop());
                    }
                    this.oprStack.Pop();
                }
                catch(InvalidOperationException)
                {
                    throw (new Exception("ExprAnalyzer: brackets does not match."));
                }
            }
            else
            {
                while(this.oprStack.Count != 0 && this.oprPriority(this.oprStack.Peek()) >= this.oprPriority(opr))
                {
                    this.varQueue.Enqueue(this.oprStack.Pop());
                }
                this.oprStack.Push(opr);
            }
        }
        private int oprPriority(string opr)
        {
            switch(opr)
            {
                case "(":
                case ")":
                    return 0;
                case "+":
                case "-":
                    return 20;
                case "*":
                case "/":
                case "%":
                    return 40;
                case "&":
                case "|":
                case "~":
                case "^":
                    return 60;
                default:
                    return 80;
            }
        }
        private void calculate()
        {
            Stack<double> Num = new Stack<double>();
            Match M;
            string S;
            while(this.varQueue.Count > 0)
            {
                double RightParam;
                S = this.varQueue.Dequeue();
                if ((M = this.numRegex.Match(S)).Success && M.Index == 0 && M.Length == S.Length)
                    Num.Push(double.Parse(S));
                else if((M = this.oprRegex.Match(S)).Success && M.Index == 0 && M.Length == S.Length)
                {
                    try
                    {
                        RightParam = Num.Pop();
                        switch(S)
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
                            case "ceiling":
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
                        }
                    }
                    catch(InvalidOperationException)
                    {
                        throw (new Exception("ExprAnalyzer: Invalid Expression."));
                    }
                }
                else
                    throw (new Exception("for debug: program logic ERROR"));
            }
            this.value = Num.Pop();
            if(Num.Count != 0)
                throw (new Exception("ExprAnalyzer: Invalid Expression"));
        }
        public void SetVariableTable(string variableTable)
        {
            List<variableStr> VT=new List<variableStr>();
            Match M = Regex.Match(variableTable, @"(?<Name>" + varStr + @")\s*=\s*(?<Value>" + numStr + @")");
            for(; M.Success; M = M.NextMatch())
                VT.Add(new variableStr(M.Result("${Name}"), M.Result("${Value}")));
            this.varTable = new variableStr[VT.Count];
            VT.CopyTo(this.varTable);
            this.analysed = false;
        }
        public void SetVariableTable(Variable[] variableTable)
        {
            this.varTable = new variableStr[variableTable.Length];
            int i = 0;
            foreach(Variable v in variableTable)
            {
                this.varTable[i++] = new variableStr(v.Name, v.Value.ToString());
            }
            this.analysed = false;
        }
        public bool ChangeVarible(string name, double value)
        {
            for(int i = 0; i < this.varTable.Length; i++)
            {
                if(this.varTable[i].Name == name)
                {
                    this.varTable[i].Value = value.ToString();
                    this.analysed = false;
                    return true;
                }
            }
            return false;
        }
        public void ChangeVarible(int index, double value)
        {
            this.varTable[index].Value = value.ToString();
            this.analysed = false;
        }
        #endregion
    }
}
