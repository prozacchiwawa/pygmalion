using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;

namespace pygmalion
{
    public enum CodeType { GLOBAL_CODE, EVAL_CODE, FUNCTION_CODE };

    class Node : List<Node>
    {
        public TokenType type;
        public Node valueNode;
        public object value;
        public Node condition, thenPart, elsePart;
        public int start, end, lineno, defaultIndex;
        public bool inForLoopInit, isLoop, postfix, inFunction;
        public bool readOnly;
        public string label;
        public Node discriminant;
        public Node body, iterator;
        public Node setup, update;
        public Node initializer;
        public Node finallyBlock;
        public Node block, expression, statement, tryBlock, guard;
        Tokenizer tokenizer;
        public StatementForm functionForm;
        public string name, varName;
        public Node caseLabel;
        public List<Node> cases = new List<Node>();
        public Node statements;
        public TokenType assignOp;
        public List<Node> catchClauses = new List<Node>();
        public bool ecmaStrictMode { get { return false; } }
        public Node varDecl, jobject;
        public List<Node> funDecls = new List<Node>();
        public List<Node> varDecls = new List<Node>();
        public List<Node> stmtStack = new List<Node>();
        public Node target, exception;
        public List<object> fparams = new List<object>();
        public int hookLevel, bracketLevel, curlyLevel, parenLevel;
        public Dictionary<string, JSSimpleProperty> fields = new Dictionary<string, JSSimpleProperty>();

        public Node(Tokenizer t, TokenType type, params Node[] arguments)
            : this(t, type, false, arguments) { }

        public Node(Tokenizer t, TokenType type, bool inFunction, params Node[] arguments)
        {
            Token token = t.token;
            this.inFunction = inFunction;
            if (token != null)
            {
                this.type = type != TokenType.NULL ? type : token.type;
                this.value = token.value;
                lineno = token.lineno;
                start = token.start;
                end = token.end;
            }
            else
            {
                this.type = type;
                lineno = t.lineno;
            }
            this.tokenizer = t;

            int i;
            for (i = 0; i < arguments.Length; i++)
                Add((Node)arguments[i]);
        }

        public Node(Tokenizer t) : this(t, TokenType.NULL) { }

        // Always use push to add operands to an expression, to update start and end.
        public Node push(Node kid)
        {
            int start = kid.start;
            int end = kid.end;
            if (start < this.start)
                this.start = start;
            if (this.end < end)
                this.end = end;
            Add(kid);
            return kid;
        }

        public static int indentLevel = 0;

        public string tokenstr(TokenType tt)
        {
            string t = jsdefs.tokens[tt];
            return new Regex("^\\W").IsMatch(t.ToString()) ? jsdefs.opTypeNames[t] : t.ToUpper();
        }

        public static string ToString(bool semiIfNeeded, Node n, string indent)
        {
            string idt = "    ";
            string result = "";
            int i;

            if (n == null) return "";

            string semi;
            if (n.type == TokenType.If ||
                n.type == TokenType.Do ||
                n.type == TokenType.For ||
                n.type == TokenType.Try ||
                n.type == TokenType.While ||
                n.type == TokenType.Switch ||
                n.type == TokenType.SCRIPT)
                semi = "";
            else
                semi = semiIfNeeded ? ";" : "";

            switch (n.type)
            {
                case TokenType.Function:
                    return indent + "function () {\n" + ToString(true, n.body, indent + idt) + "}";
                case TokenType.SCRIPT:
                    for (i = 0; i < n.Count; i++)
                    {
                        result += ToString(true, n[i], indent) + "\n";
                    }
                    return result;
                case TokenType.STRING:
                    return "'" + n.value.ToString().Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t") + "'";
                case TokenType.LIST:
                case TokenType.COMMA:
                    result = "";
                    string comma = "";
                    foreach (Node child in n)
                    {
                        result += comma + ToString(false, child, "");
                        comma = ",";
                    }
                    return result;
                case TokenType.IDENTIFIER:
                    return n.value.ToString();
                case TokenType.This:
                    return "this";
                case TokenType.False:
                    return "false";
                case TokenType.True:
                    return "true";
                case TokenType.SEMICOLON:
                    return indent + ToString(false, n.expression, "") + (semiIfNeeded ? ";" : "");
                case TokenType.BLOCK:
                    result = (indent.Length >= 4 ? indent.Substring(4) : "") + "{\n";
                    for (i = 0; i < n.Count; i++)
                    {
                        result += indent + ToString(true, n[i], indent + idt) + semi + "\n";
                    }
                    result += (indent.Length >= 4 ? indent.Substring(4) : "") + "}";
                    return result;
                case TokenType.If:
                    return indent + "if (" + ToString(false, n.condition, "") + ")\n" +
                        ToString(true, n.thenPart, indent + idt) +
                        (n.elsePart != null ? indent + "\n" + indent + "else\n" +
                        ToString(true, n.elsePart, indent + idt) : "");
                case TokenType.CALL:
                    return ToString(false, n[0], "") + "(" + ToString(false, n[1], "") + ")";
                case TokenType.NUMBER:
                    return n.value.ToString();
                case TokenType.DOT:
                    return ToString(false, n[0], "") + jsdefs.tokens[n.type] + ToString(false, n[1], "");
                case TokenType.ASSIGN:
                    bool isAssignment = (string)n.value == "=";
                    return ToString(false, n[0], "") + " " + (isAssignment ? "= " : n.value.ToString() + "= ") + ToString(false, n[1], "");
                case TokenType.AND:
                case TokenType.EQ:
                case TokenType.GE:
                case TokenType.GT:
                case TokenType.LE:
                case TokenType.LT:
                case TokenType.MINUS:
                case TokenType.MOD:
                case TokenType.MUL:
                case TokenType.NE:
                case TokenType.PLUS:
                case TokenType.STRICT_EQ:
                case TokenType.STRICT_NE:
                    return ToString(false, n[0], "") + " " + jsdefs.tokens[n.type] + " " + ToString(false, n[1], "");
                case TokenType.Return:
                    return indent + "return " + ToString(false, n.valueNode, "");
                case TokenType.NEW_WITH_ARGS:
                    return ToString(false, n[0], "") + "(" + ToString(false, n[1], "") + ")";
                case TokenType.ARRAY_INIT:
                    result = "";
                    comma = "";
                    foreach (Node child in n)
                    {
                        result += comma + ToString(false, child, "");
                        comma = ",";
                    }
                    return "[" + result + "]";
                case TokenType.INDEX:
                    return ToString(false, n[0], "") + "[" + ToString(false, n[1], "") + "]";
                case TokenType.Var:
                    result = "";
                    comma = "";
                    foreach (Node child in n)
                    {
                        result += comma + ToString(false, child, "") + (child.initializer != null ? (" = " + ToString(false, child.initializer, "")) : "");
                        comma = ",";
                    }
                    return "var " + result;
                case TokenType.For:
                    result = "";
                    semi = "";
                    return "for (" + ToString(false, n.setup, "") + "; " + ToString(false, n.condition, "") + "; " + ToString(false, n.update, "") + ")\n" + ToString(false, n.body, indent);
                case TokenType.INCREMENT:
                case TokenType.DECREMENT:
                    result = n.type == TokenType.INCREMENT ? "++" : "--";
                    return (n.postfix ? "" : result) + ToString(false, n[0], "") + (n.postfix ? result : "");
                case TokenType.New:
                    return "new " + ToString(false, n[0], "") + "()";
                default:
                    return indent + "[unknown node type: " + n.type.ToString() + "]";
            }
        }

        string ToString(string INDENTATION, List<Node> usedNodes)
        {
            if (usedNodes.Contains(this))
                return INDENTATION + "{ ... }";
            else
                usedNodes.Add(this);
            string s = INDENTATION + "{\n";
            s += INDENTATION + "type: " + tokenstr(this.type) + ",\n";
            s += INDENTATION + "value: " + (value != null ? value.ToString() : "(null)") + ",\n";
            foreach (FieldInfo fi in GetType().GetFields())
                if (fi.FieldType == typeof(Node))
                {
                    Node fv = (Node)fi.GetValue(this);
                    if (fv != null)
                    {
                        s += INDENTATION + fi.Name + ":\n" + fv.ToString(INDENTATION + "    ", usedNodes) + ",\n";
                    }
                }
            int i;
            for (i = 0; i < Count; i++)
            {
                s += INDENTATION + "[" + i + "]:\n" + this[i].ToString(INDENTATION + "    ", usedNodes) + ",\n";
            }
            s += INDENTATION + "}";
            return s;
        }

        public string Breakout { get { return this.ToString(); } }

        public override string ToString()
        {
            List<Node> usedNodes = new List<Node>();
            return ToString("", usedNodes);
        }

        public string getSource()
        {
            return tokenizer.source.Substring(this.start, this.end);
        }

        public string filename
        {
            get { return tokenizer.filename; }
        }
    }
}
