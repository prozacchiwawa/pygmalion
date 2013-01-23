using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pygmalion
{
    public class jsparse
    {
        public static bool TraceParse;
        internal static Node reduce(Tokenizer t, List<Node> operators, List<Node> operands)
        {
            Node n = operators[operators.Count - 1], left = null, right = null;
            operators.RemoveAt(operators.Count - 1);
            TokenType op = n.type;
            int arity = jsdefs.opArity[op];
            if (arity == -2)
            {
                // Flatten left-associative trees.
                left =
                    ((operands.Count >= 2 && operands[operands.Count - 2] != null) ?
                        null : operands[operands.Count - 2]);
                if (left != null && left.type == op)
                {
                    right = operands[operands.Count - 1];
                    operands.RemoveAt(operands.Count - 1);
                    left.push(right);
                    return left;
                }
                arity = 2;
            }

            // Always use push to add operands to n, to update start and end.
            List<Node> a = new List<Node>();
            a.AddRange(operands.GetRange(operands.Count - arity, arity));
            List<Node> newop = new List<Node>();
            newop.AddRange(operands.GetRange(0, operands.Count - arity));
            operands.Clear();
            operands.AddRange(newop);
            for (var i = 0; i < arity; i++)
                n.Add(a[i]);

            // Include closing bracket or postfix operator in [start,end).
            if (n.end < t.token.end)
                n.end = t.token.end;

            operands.Add(n);
            return n;
        }

        internal static Node Expression(Tokenizer t, Node x)
        {
            Node n = Expression(t, x, TokenType.NULL);
            if (TraceParse)
                System.Console.WriteLine("Expression [ " + n.ToString() + "]");
            return n;
        }
        internal static Node Expression(Tokenizer t, Node x, TokenType stop)
        {
            bool loop_if;
            Node n = null;
            TokenType tt;
            List<Node> operators = new List<Node>(), operands = new List<Node>();
            int bl = x.bracketLevel, cl = x.curlyLevel, pl = x.parenLevel,
                hl = x.hookLevel;

            loop_if = false;
            while ((tt = t.get()) != TokenType.END)
            {
                if (tt == stop &&
                    x.bracketLevel == bl && x.curlyLevel == cl && x.parenLevel == pl &&
                    x.hookLevel == hl)
                {
                    // Stop only if tt matches the optional stop parameter, and that
                    // token is not quoted by some kind of bracket.
                    break;
                }
                switch (tt)
                {
                    case TokenType.SEMICOLON:
                        // NB: cannot be empty, Statement handled that.
                        loop_if = true;
                        break;

                    case TokenType.ASSIGN:
                    case TokenType.HOOK:
                    case TokenType.COLON:
                        if (t.scanOperand)
                        {
                            loop_if = true;
                            break;
                        }
                        // Use >, not >=, for right-associative ASSIGN and HOOK/COLON.
                        while
                            (operators.Count > 0 && 
                             (jsdefs.opPrecedence[operators[operators.Count - 1].type] > jsdefs.opPrecedence[tt]) ||
                             (tt == TokenType.COLON && operators[operators.Count - 1].type == TokenType.ASSIGN))
                        {
                            reduce(t, operators, operands);
                        }
                        if (tt == TokenType.COLON)
                        {
                            n = operators[operators.Count - 1];
                            if (n.type != TokenType.HOOK)
                                throw t.newSyntaxError("Invalid label");
                            --x.hookLevel;
                        }
                        else
                        {
                            operators.Add(new Node(t));
                            if (tt == TokenType.ASSIGN)
                                operands[operands.Count - 1].assignOp = t.token.assignOp;
                            else
                                ++x.hookLevel;      // tt == HOOK
                        }
                        t.scanOperand = true;
                        break;

                    case TokenType.In:
                        // An in operator should not be parsed if we're parsing the head of
                        // a for (...) loop, unless it is in the then part of a conditional
                        // expression, or parenthesized somehow.
                        if (x.inForLoopInit && x.hookLevel == 0 &&
                            x.bracketLevel == 0 && x.curlyLevel == 0 &&
                            x.parenLevel == 0)
                        {
                            loop_if = true;
                            break;
                        }
                        // FALL THROUGH
                        if (t.scanOperand)
                        {
                            loop_if = true;
                            break;
                        }
                        while (jsdefs.opPrecedence[operators[operators.Count - 1].type] >= jsdefs.opPrecedence[tt])
                            reduce(t, operators, operands);

                        if (tt == TokenType.DOT)
                        {
                            t.mustMatch(TokenType.IDENTIFIER);
                            operands.Add(new Node(t, TokenType.DOT, operands[operands.Count - 1], new Node(t)));
                            operands.RemoveAt(operands.Count - 1);
                        }
                        else
                        {
                            operators.Add(new Node(t));
                            t.scanOperand = true;
                        }
                        break;
                    case TokenType.COMMA:
                    // Treat comma as left-associative so reduce can fold left-heavy
                    // COMMA trees into a single array.
                    // FALL THROUGH
                    case TokenType.OR:
                    case TokenType.AND:
                    case TokenType.BITWISE_OR:
                    case TokenType.BITWISE_XOR:
                    case TokenType.BITWISE_AND:
                    case TokenType.EQ:
                    case TokenType.NE:
                    case TokenType.STRICT_EQ:
                    case TokenType.STRICT_NE:
                    case TokenType.LT:
                    case TokenType.LE:
                    case TokenType.GE:
                    case TokenType.GT:
                    case TokenType.Instanceof:
                    case TokenType.LSH:
                    case TokenType.RSH:
                    case TokenType.URSH:
                    case TokenType.PLUS:
                    case TokenType.MINUS:
                    case TokenType.MUL:
                    case TokenType.DIV:
                    case TokenType.MOD:
                    case TokenType.DOT:
                        if (t.scanOperand)
                        {
                            loop_if = true;
                            break;
                        }
                        while (operators.Count > 0 && jsdefs.opPrecedence[operators[operators.Count - 1].type] >= jsdefs.opPrecedence[tt])
                            reduce(t, operators, operands);

                        if (tt == TokenType.DOT)
                        {
                            t.mustMatch(TokenType.IDENTIFIER);
                            operands[operands.Count - 1] = new Node(t, TokenType.DOT, operands[operands.Count - 1], new Node(t));
                        }
                        else
                        {
                            operators.Add(new Node(t));
                            t.scanOperand = true;
                        }
                        break;

                    case TokenType.Delete:
                    case TokenType.Void:
                    case TokenType.Typeof:
                    case TokenType.NOT:
                    case TokenType.BITWISE_NOT:
                    case TokenType.UNARY_PLUS:
                    case TokenType.UNARY_MINUS:
                    case TokenType.New:
                        if (!t.scanOperand)
                        {
                            loop_if = true;
                            break;
                        }
                        operators.Add(new Node(t));
                        break;

                    case TokenType.INCREMENT:
                    case TokenType.DECREMENT:
                        if (t.scanOperand)
                        {
                            operators.Add(new Node(t));  // prefix increment or decrement
                        }
                        else
                        {
                            // Don't cross a line boundary for postfix {in,de}crement.
                            if (t.tokens[(t.tokenIndex + t.lookahead - 1) & 3].lineno !=
                                t.lineno)
                            {
                                loop_if = true;
                                break;
                            }

                            // Use >, not >=, so postfix has higher precedence than prefix.
                            while (operators.Count > 0 && jsdefs.opPrecedence[operators[operators.Count - 1].type] > jsdefs.opPrecedence[tt])
                                reduce(t, operators, operands);
                            n = new Node(t, tt, operands[operands.Count - 1]);
                            operands.RemoveAt(operands.Count - 1);
                            n.postfix = true;
                            operands.Add(n);
                        }
                        break;

                    case TokenType.Function:
                        if (!t.scanOperand)
                        {
                            loop_if = true;
                            break;
                        }
                        operands.Add(FunctionDefinition(t, x, false, StatementForm.EXPRESSED_FORM));
                        t.scanOperand = false;
                        break;

                    case TokenType.Null:
                    case TokenType.This:
                    case TokenType.True:
                    case TokenType.False:
                    case TokenType.IDENTIFIER:
                    case TokenType.NUMBER:
                    case TokenType.STRING:
                    case TokenType.REGEXP:
                        if (!t.scanOperand)
                        {
                            loop_if = true;
                            break;
                        }
                        operands.Add(new Node(t));
                        t.scanOperand = false;
                        break;

                    case TokenType.LEFT_BRACKET:
                        if (t.scanOperand)
                        {
                            // Array initialiser.  Parse using recursive descent, as the
                            // sub-grammar here is not an operator grammar.
                            n = new Node(t, TokenType.ARRAY_INIT);
                            while ((tt = t.peek()) != TokenType.RIGHT_BRACKET)
                            {
                                if (tt == TokenType.COMMA)
                                {
                                    t.get();
                                    n.push(null);
                                    continue;
                                }
                                n.push(Expression(t, x, TokenType.COMMA));
                                if (!t.match(TokenType.COMMA))
                                    break;
                            }
                            t.mustMatch(TokenType.RIGHT_BRACKET);
                            operands.Add(n);
                            t.scanOperand = false;
                        }
                        else
                        {
                            // Property indexing operator.
                            operators.Add(new Node(t, TokenType.INDEX));
                            t.scanOperand = true;
                            ++x.bracketLevel;
                        }
                        break;

                    case TokenType.RIGHT_BRACKET:
                        if (t.scanOperand || x.bracketLevel == bl)
                        {
                            loop_if = true;
                            break;
                        }
                        while (reduce(t, operators, operands).type != TokenType.INDEX)
                            continue;
                        --x.bracketLevel;
                        break;

                    case TokenType.LEFT_CURLY:
                        if (!t.scanOperand)
                        {
                            loop_if = true;
                            break;
                        }
                        // Object initialiser.  As for array initialisers (see above),
                        // parse using recursive descent.
                        ++x.curlyLevel;
                        n = new Node(t, TokenType.OBJECT_INIT);
                        bool object_init_if;
                        Node idnode = null;
                        object_init_if = false;
                        if (!t.match(TokenType.RIGHT_CURLY))
                        {
                            do
                            {
                                tt = t.get();
                                if ((t.token.value.ToString() == "get" || t.token.value.ToString() == "set") &&
                                    t.peek() == TokenType.IDENTIFIER)
                                {
                                    if (x.ecmaStrictMode)
                                        throw t.newSyntaxError("Illegal property accessor");
                                    n.push(FunctionDefinition(t, x, true, StatementForm.EXPRESSED_FORM));
                                }
                                else
                                {
                                    switch (tt)
                                    {
                                        case TokenType.IDENTIFIER:
                                        case TokenType.NUMBER:
                                        case TokenType.STRING:
                                            idnode = new Node(t);
                                            break;
                                        case TokenType.RIGHT_CURLY:
                                            if (x.ecmaStrictMode)
                                                throw t.newSyntaxError("Illegal trailing ,");
                                            object_init_if = true;
                                            break;
                                        default:
                                            throw t.newSyntaxError("Invalid property name");
                                    }
                                    t.mustMatch(TokenType.COLON);
                                    n.push(new Node(t, TokenType.PROPERTY_INIT, idnode,
                                                    Expression(t, x, TokenType.COMMA)));
                                }
                                if (object_init_if) break;
                            } while (t.match(TokenType.COMMA));
                            t.mustMatch(TokenType.RIGHT_CURLY);
                        }
                        operands.Add(n);
                        t.scanOperand = false;
                        --x.curlyLevel;
                        break;

                    case TokenType.RIGHT_CURLY:
                        if (!t.scanOperand && x.curlyLevel != cl)
                            throw new Exception("PANIC: right curly botch");
                        loop_if = true;
                        break;

                    case TokenType.LEFT_PAREN:
                        if (t.scanOperand)
                        {
                            operators.Add(new Node(t, TokenType.GROUP));
                        }
                        else
                        {
                            while (jsdefs.opPrecedence[operators.Count > 0 ? operators[operators.Count - 1].type : TokenType.CALL] > jsdefs.opPrecedence[TokenType.New])
                                reduce(t, operators, operands);

                            // Handle () now, to regularize the n-ary case for n > 0.
                            // We must set scanOperand in case there are arguments and
                            // the first one is a regexp or unary+/-.
                            if (operators.Count > 0)
                                n = operators[operators.Count - 1];
                            else
                                n = new Node(t, TokenType.NULL);

                            t.scanOperand = true;
                            if (t.match(TokenType.RIGHT_PAREN))
                            {
                                if (n.type == TokenType.New)
                                {
                                    operators.RemoveAt(operators.Count - 1);
                                    n.push(operands[operands.Count - 1]);
                                    operands.RemoveAt(operands.Count - 1);
                                }
                                else
                                {
                                    n = new Node(t, TokenType.CALL, operands[operands.Count - 1],
                                                 new Node(t, TokenType.LIST));
                                    operands.RemoveAt(operands.Count - 1);
                                }
                                operands.Add(n);
                                t.scanOperand = false;
                                break;
                            }
                            if (n.type == TokenType.New)
                                n.type = TokenType.NEW_WITH_ARGS;
                            else
                                operators.Add(new Node(t, TokenType.CALL));
                        }
                        ++x.parenLevel;
                        break;

                    case TokenType.RIGHT_PAREN:
                        if (t.scanOperand || x.parenLevel == pl)
                        {
                            loop_if = true;
                            break;
                        }
                        while ((tt = reduce(t, operators, operands).type) != TokenType.GROUP && tt != TokenType.CALL &&
                               tt != TokenType.NEW_WITH_ARGS)
                        {
                            continue;
                        }
                        if (tt != TokenType.GROUP)
                        {
                            n = operands[operands.Count - 1];
                            if (n[1].type != TokenType.COMMA)
                                n[1] = new Node(t, TokenType.LIST, n[1]);
                            else
                                n[1].type = TokenType.LIST;
                        }
                        --x.parenLevel;
                        break;

                    // Automatic semicolon insertion means we may scan across a newline
                    // and into the beginning of another statement.  If so, break out of
                    // the while loop and let the t.scanOperand logic handle errors.
                    default:
                        loop_if = true;
                        break;
                }
                if (loop_if) break;
            }

            if (TraceParse)
            {
                System.Console.WriteLine
                    ("loop exit at {0}..., node {1}",
                    t.input.Substring(0, Math.Min(t.input.Length, 20)), n != null ? n.ToString() : "(null)");
            }

            if (x.hookLevel != hl)
                throw t.newSyntaxError("Missing : after ?");
            if (x.parenLevel != pl)
                throw t.newSyntaxError("Missing ) in parenthetical");
            if (x.bracketLevel != bl)
                throw t.newSyntaxError("Missing ] in index expression");
            if (t.scanOperand)
                throw t.newSyntaxError("Missing operand");

            // Resume default mode, scanning for operands, not operators.
            t.scanOperand = true;
            t.unget();
            while (operators.Count != 0)
                reduce(t, operators, operands);
            return operands[operands.Count - 1];
        }
        internal static Node parse(ExecutionContext GLOBAL, string s, string f, int l)
        {
            Tokenizer t = new Tokenizer(GLOBAL, s, f, l);
            Node x = new Node(t, (TokenType)(int)CodeType.GLOBAL_CODE);
            Node n = Script(t, x);
            if (!t.done)
                throw t.newSyntaxError("Syntax error");
            return n;
        }
        internal static Node Script(Tokenizer t, Node x)
        {
            Node n = Statements(t, x);
            //System.Console.WriteLine("SCRIPT [" + n.ToString() + "]");
            n.type = TokenType.SCRIPT;
            n.funDecls = x.funDecls;
            n.varDecls = x.varDecls;
            return n;
        }

        internal static Node Statements(Tokenizer t, Node x)
        {
            Node n = new Node(t, TokenType.BLOCK);
            x.stmtStack.Add(n);
            while (!t.done && t.peek() != TokenType.RIGHT_CURLY)
                n.push(Statement(t, x));
            x.stmtStack.RemoveAt(x.stmtStack.Count - 1);
            return n;
        }

        internal static Node Statement(Tokenizer t, Node x)
        {
            int i;
            string label;
            Node n, n2;
            List<Node> ss = new List<Node>();
            TokenType tt = t.get();

            // Cases for statements ending in a right curly return early, avoiding the
            // common semicolon insertion magic after this switch.
            switch (tt)
            {
                case TokenType.Function:
                    return FunctionDefinition
                        (t, x, true,
                           (x.stmtStack.Count > 1) ? StatementForm.STATEMENT_FORM : StatementForm.DECLARED_FORM);

                case TokenType.LEFT_CURLY:
                    n = Statements(t, x);
                    t.mustMatch(TokenType.RIGHT_CURLY);
                    return n;

                case TokenType.If:
                    n = new Node(t, TokenType.NULL);
                    n.condition = ParenExpression(t, x);
                    x.stmtStack.Add(n);
                    n.thenPart = Statement(t, x);
                    n.elsePart = t.match(TokenType.Else) ? Statement(t, x) : null;
                    x.stmtStack.RemoveAt(x.stmtStack.Count - 1);
                    return n;

                case TokenType.Switch:
                    n = new Node(t, TokenType.NULL);
                    t.mustMatch(TokenType.LEFT_PAREN);
                    n.discriminant = Expression(t, x);
                    t.mustMatch(TokenType.RIGHT_PAREN);
                    n.defaultIndex = -1;
                    x.stmtStack.Add(n);
                    t.mustMatch(TokenType.LEFT_CURLY);
                    while ((tt = t.get()) != TokenType.RIGHT_CURLY)
                    {
                        switch (tt)
                        {
                            case TokenType.Default:
                                if (n.defaultIndex >= 0)
                                    throw t.newSyntaxError("More than one switch default");
                                // FALL THROUGH
                                n2 = new Node(t);
                                if (tt == TokenType.Default)
                                    n.defaultIndex = n.cases.Count;
                                else
                                    n2.caseLabel = Expression(t, x, TokenType.COLON);
                                break;
                            case TokenType.Case:
                                n2 = new Node(t);
                                if (tt == TokenType.Default)
                                    n.defaultIndex = n.cases.Count;
                                else
                                    n2.caseLabel = Expression(t, x, TokenType.COLON);
                                break;
                            default:
                                throw t.newSyntaxError("Invalid switch case");
                        }
                        t.mustMatch(TokenType.COLON);
                        n2.statements = new Node(t, TokenType.BLOCK);
                        while ((tt = t.peek()) != TokenType.Case && tt != TokenType.Default && tt != TokenType.RIGHT_CURLY)
                            n2.statements.push(Statement(t, x));
                        n.cases.Add(n2);
                    }
                    x.stmtStack.RemoveAt(x.stmtStack.Count - 1);
                    return n;

                case TokenType.For:
                    n = new Node(t);
                    n.isLoop = true;
                    n2 = null;
                    t.mustMatch(TokenType.LEFT_PAREN);
                    if ((tt = t.peek()) != TokenType.SEMICOLON)
                    {
                        x.inForLoopInit = true;
                        if (tt == TokenType.Var || tt == TokenType.Const)
                        {
                            t.get();
                            n2 = Variables(t, x);
                        }
                        else
                        {
                            n2 = Expression(t, x);
                        }
                        x.inForLoopInit = false;
                    }
                    if (n2 != null && t.match(TokenType.In))
                    {
                        n.type = TokenType.FOR_IN;
                        if (n2.type == TokenType.Var)
                        {
                            if (n2.Count != 1)
                            {
                                throw new SyntaxError("Invalid for..in left-hand side",
                                                      t.filename, n2.lineno);
                            }

                            // NB: n2[0].type == IDENTIFIER and n2[0].value == n2[0].name.
                            n.iterator = n2[0];
                            n.varDecl = n2;
                        }
                        else
                        {
                            n.iterator = n2;
                            n.varDecl = null;
                        }
                        n.jobject = Expression(t, x);
                    }
                    else
                    {
                        n.setup = n2;
                        t.mustMatch(TokenType.SEMICOLON);
                        n.condition = (t.peek() == TokenType.SEMICOLON) ? null : Expression(t, x);
                        t.mustMatch(TokenType.SEMICOLON);
                        n.update = (t.peek() == TokenType.RIGHT_PAREN) ? null : Expression(t, x);
                    }
                    t.mustMatch(TokenType.RIGHT_PAREN);
                    n.body = nest(t, x, n, Statement);
                    return n;

                case TokenType.While:
                    n = new Node(t);
                    n.isLoop = true;
                    n.condition = ParenExpression(t, x);
                    n.body = nest(t, x, n, Statement);
                    return n;

                case TokenType.Do:
                    n = new Node(t);
                    n.isLoop = true;
                    n.body = nest(t, x, n, Statement, TokenType.While);
                    n.condition = ParenExpression(t, x);
                    if (!x.ecmaStrictMode)
                    {
                        // <script language="JavaScript"> (without version hints) may need
                        // automatic semicolon insertion without a newline after do-while.
                        // See http://bugzilla.mozilla.org/show_bug.cgi?id=238945.
                        t.match(TokenType.SEMICOLON);
                        return n;
                    }
                    break;

                case TokenType.Break:
                case TokenType.Continue:
                    n = new Node(t);
                    if (t.peekOnSameLine() == TokenType.IDENTIFIER)
                    {
                        t.get();
                        n.label = t.token.value.ToString();
                    }
                    ss = x.stmtStack;
                    i = ss.Count;
                    label = n.label;
                    if (label != null && label != "")
                    {
                        do
                        {
                            if (--i < 0)
                                throw t.newSyntaxError("Label not found");
                        } while (ss[i].label != label);
                    }
                    else
                    {
                        do
                        {
                            if (--i < 0)
                            {
                                throw t.newSyntaxError("Invalid " + ((tt == TokenType.Break)
                                                                     ? "break"
                                                                     : "continue"));
                            }
                        } while (!ss[i].isLoop && (tt != TokenType.Break || ss[i].type != TokenType.Switch));
                    }
                    n.target = ss[i];
                    break;

                case TokenType.Try:
                    n = new Node(t);
                    n.tryBlock = Block(t, x);
                    while (t.match(TokenType.Catch))
                    {
                        n2 = new Node(t);
                        t.mustMatch(TokenType.LEFT_PAREN);
                        n2.varName = t.mustMatch(TokenType.IDENTIFIER).value.ToString();
                        if (t.match(TokenType.If))
                        {
                            if (x.ecmaStrictMode)
                                throw t.newSyntaxError("Illegal catch guard");
                            if (n.catchClauses.Count != 0 && n.catchClauses[n.catchClauses.Count - 1].guard == null)
                                throw t.newSyntaxError("Guarded catch after unguarded");
                            n2.guard = Expression(t, x);
                        }
                        else
                        {
                            n2.guard = null;
                        }
                        t.mustMatch(TokenType.RIGHT_PAREN);
                        n2.block = Block(t, x);
                        n.catchClauses.Add(n2);
                    }
                    if (t.match(TokenType.Finally))
                        n.finallyBlock = Block(t, x);
                    if (n.catchClauses.Count == 0 && n.finallyBlock != null)
                        throw t.newSyntaxError("Invalid try statement");
                    return n;

                case TokenType.Catch:
                case TokenType.Finally:
                    throw t.newSyntaxError(jsdefs.tokens[tt] + " without preceding try");

                case TokenType.Throw:
                    n = new Node(t);
                    n.exception = Expression(t, x);
                    break;

                case TokenType.Return:
                    if (!x.inFunction)
                        throw t.newSyntaxError("Invalid return");
                    n = new Node(t);
                    tt = t.peekOnSameLine();
                    if (tt != TokenType.END && tt != TokenType.NEWLINE && tt != TokenType.SEMICOLON && tt != TokenType.RIGHT_CURLY)
                        n.valueNode = Expression(t, x);
                    break;

                case TokenType.With:
                    n = new Node(t);
                    n.jobject = ParenExpression(t, x);
                    n.body = nest(t, x, n, Statement);
                    return n;

                case TokenType.Var:
                case TokenType.Const:
                    n = Variables(t, x);
                    break;

                case TokenType.Debugger:
                    n = new Node(t);
                    break;

                case TokenType.NEWLINE:
                case TokenType.SEMICOLON:
                    n = new Node(t, TokenType.SEMICOLON);
                    n.expression = null;
                    return n;

                default:
                    if (tt == TokenType.IDENTIFIER)
                    {
                        t.scanOperand = false;
                        tt = t.peek();
                        t.scanOperand = true;
                        if (tt == TokenType.COLON)
                        {
                            label = t.token.value.ToString();
                            ss = x.stmtStack;
                            for (i = ss.Count - 1; i >= 0; --i)
                            {
                                if (ss[i].label == label)
                                    throw t.newSyntaxError("Duplicate label");
                            }
                            t.get();
                            n = new Node(t, TokenType.LABEL);
                            n.label = label;
                            n.statement = nest(t, x, n, Statement);
                            return n;
                        }
                    }

                    n = new Node(t, TokenType.SEMICOLON);
                    t.unget();
                    n.expression = Expression(t, x);
                    n.end = n.expression.end;
                    break;
            }

            if (t.lineno == t.token.lineno)
            {
                tt = t.peekOnSameLine();
                if (tt != TokenType.END && tt != TokenType.NEWLINE && tt != TokenType.SEMICOLON && tt != TokenType.RIGHT_CURLY)
                    throw t.newSyntaxError("Missing ; before statement");
            }
            t.match(TokenType.SEMICOLON);
            if (TraceParse)
                System.Console.WriteLine("Parse => " + (n == null ? "(null)" : n.ToString()));
            return n;
        }

        internal static Node ParenExpression(Tokenizer t, Node x)
        {
            t.mustMatch(TokenType.LEFT_PAREN);
            Node n = Expression(t, x);
            t.mustMatch(TokenType.RIGHT_PAREN);
            return n;
        }
        internal static Node FunctionDefinition(Tokenizer t, Node x, bool requireName, StatementForm functionForm)
        {
            Node f = new Node(t, TokenType.NULL);
            if (f.type != TokenType.Function)
                f.type = (f.value.ToString() == "get") ? TokenType.GETTER : TokenType.SETTER;
            if (t.match(TokenType.IDENTIFIER))
                f.name = t.token.value.ToString();
            else if (requireName)
                throw t.newSyntaxError("Missing function identifier");

            t.mustMatch(TokenType.LEFT_PAREN);
            TokenType tt;
            while ((tt = t.get()) != TokenType.RIGHT_PAREN)
            {
                if (tt != TokenType.IDENTIFIER)
                    throw t.newSyntaxError("Missing formal parameter");
                f.fparams.Add(t.token.value);
                if (t.peek() != TokenType.RIGHT_PAREN)
                    t.mustMatch(TokenType.COMMA);
            }

            t.mustMatch(TokenType.LEFT_CURLY);
            Node x2 = new Node(t, TokenType.NULL, true);
            f.body = Script(t, x2);
            t.mustMatch(TokenType.RIGHT_CURLY);
            f.end = t.token.end;

            f.functionForm = functionForm;
            if (functionForm == StatementForm.DECLARED_FORM)
                x.funDecls.Add(f);
            return f;
        }

        // Statement stack and nested statement handler.
        internal delegate Node NestFun(Tokenizer t, Node x);
        internal static Node nest(Tokenizer t, Node x, Node node, NestFun func, TokenType end)
        {
            x.stmtStack.Add(node);
            var n = func(t, x);
            x.stmtStack.RemoveAt(x.stmtStack.Count - 1);
            if (end != TokenType.NULL && end != TokenType.END)
                t.mustMatch(end);
            return n;
        }
        internal static Node nest(Tokenizer t, Node x, Node node, NestFun func)
        {
            return nest(t, x, node, func, TokenType.NULL);
        }
        internal static Node Variables(Tokenizer t, Node x)
        {
            var n = new Node(t);
            do
            {
                t.mustMatch(TokenType.IDENTIFIER);
                var n2 = new Node(t);
                n2.name = n2.value.ToString();
                if (t.match(TokenType.ASSIGN))
                {
                    if (t.token.assignOp != TokenType.NULL)
                        throw t.newSyntaxError("Invalid variable initialization");
                    n2.initializer = Expression(t, x, TokenType.COMMA);
                }
                n2.readOnly = (n.type == TokenType.Const);
                n.push(n2);
                x.varDecls.Add(n2);
            } while (t.match(TokenType.COMMA));
            return n;
        }
        static Node Block(Tokenizer t, Node x)
        {
            t.mustMatch(TokenType.LEFT_CURLY);
            var n = Statements(t, x);
            t.mustMatch(TokenType.RIGHT_CURLY);
            return n;
        }
    }
}
