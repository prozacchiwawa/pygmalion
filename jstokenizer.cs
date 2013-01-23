using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace pygmalion
{
    class Tokenizer : JSObject
    {
        int cursor;
        public string source;
        public Dictionary<int, Token> tokens = new Dictionary<int, Token>();
        public int tokenIndex, lookahead;
        public bool scanNewlines, scanOperand;
        public string filename;
        public int lineno;
        public ExecutionContext GLOBAL;
        // Build a regexp that recognizes operators and punctuators (except newline).
        public static Regex opRegExp;
        // A regexp to match floating point literals (but not integer literals).
        public static Regex fpRegExp = new Regex("^\\d+\\.\\d*(?:[eE][-+]?\\d+)?|^\\d+(?:\\.\\d*)?[eE][-+]?\\d+|^\\.\\d+(?:[eE][-+]?\\d+)?");
        // A regexp to match regexp literals.
        public static Regex reRegExp = new Regex("^\\/((?:\\.|\\[(?:\\.|[^\\]])*\\]|[^\\/])+)\\/([gimy]*)");
        // A regexp to match eol
        public static Regex elRegExp = new Regex("\n");
        // A regexp for hex numbers
        public static Regex hxRegExp = new Regex("^0[xX][\\da-fA-F]+|^0[0-7]*|^\\d+");
        // A regexp for regex escapes
        public static Regex ecRegExp = new Regex("[?|^&(){}\\[\\]+\\-*\\/\\.]");
        // A regexp for scan newlines mode
        public static Regex snRegExp = new Regex("^[ \t]+");
        // A regexp for no newlines mode
        public static Regex nnRegExp = new Regex("^\\s+");
        // A comments regex
        public static Regex cmRegExp = new Regex("^\\/(?:\\*(?:.|\n)*?\\*\\/|\\/.*)");
        // An identifier regex
        public static Regex idRegExp = new Regex("^[$_\\w]+");
        // A quotes regex
        public static Regex qtRegExp = new Regex("^\"|^'");
        // A char matching regex
        public static Regex dtRegExp = new Regex(".*");

        internal static void InitTokenizer()
        {
            string opRegExpSrc = "^";
            foreach (string i in jsdefs.opTypeNames.Keys)
            {
                if (i == "\n")
                    continue;
                if (opRegExpSrc != "^")
                    opRegExpSrc += "|^";
                opRegExpSrc += ecRegExp.Replace(i, "\\$&");
            }
            opRegExp = new Regex(opRegExpSrc);
        }

        public Tokenizer(ExecutionContext GLOBAL, string s, string f, int l)
        {
            this.GLOBAL = GLOBAL;
            this.cursor = 0;
            this.source = s;
            this.tokenIndex = 0;
            this.lookahead = 0;
            this.scanNewlines = false;
            this.scanOperand = true;
            this.filename = f != null ? f : "";
            this.lineno = f != null ? l : 1;
        }

        public string input
        {
            get
            {
                return this.source.Substring(this.cursor);
            }
        }

        public bool done
        {
            get
            {
                return this.peek() == TokenType.END;
            }
        }

        public Token token
        {
            get
            {
                Token result;
                if (!this.tokens.TryGetValue(this.tokenIndex, out result))
                    return null;
                else
                    return result;
            }
        }

        [GlobalArg]
        public bool match(TokenType tt)
        {
            return this.get() == tt || JSObject.ToBool(GLOBAL, this.unget());
        }

        public Token mustMatch(TokenType tt)
        {
            if (!this.match(tt))
                throw this.newSyntaxError("Missing " + jsdefs.tokens[tt].ToLower());
            return this.token;
        }

        public TokenType peek()
        {
            TokenType tt;
            Token next;
            if (this.lookahead != 0)
            {
                next = this.tokens[(this.tokenIndex + this.lookahead) & 3];
                if (this.scanNewlines && next.lineno != this.lineno)
                    tt = TokenType.NEWLINE;
                else
                    tt = next.type;
            }
            else
            {
                tt = this.get();
                this.unget();
            }
            return tt;
        }

        public TokenType peekOnSameLine()
        {
            this.scanNewlines = true;
            TokenType tt = this.peek();
            this.scanNewlines = false;
            return tt;
        }

        public TokenType get()
        {
            Token token;
            while (this.lookahead != 0)
            {
                --this.lookahead;
                this.tokenIndex = (this.tokenIndex + 1) & 3;
                token = this.tokens[this.tokenIndex];
                if (token.type != TokenType.NEWLINE || this.scanNewlines)
                    return token.type;
            }

            string capInput;
            Match capMatch;

            for (;;)
            {
                capInput = this.input;
                capMatch = (this.scanNewlines ? snRegExp : nnRegExp).Match(capInput);
                if (capMatch.Success)
                {
                    string spaces = capMatch.Value;
                    this.cursor += spaces.Length;
                    Match nlMatch = elRegExp.Match(spaces);
                    if (nlMatch.Value != "")
                        this.lineno += nlMatch.Length;
                    capInput = this.input;
                }

                if (!(capMatch = cmRegExp.Match(capInput)).Success)
                    break;
                string comment = capMatch.Value;
                this.cursor += comment.Length;
                string newlines = elRegExp.Match(comment).Value;
                if (newlines != "")
                    this.lineno += newlines.Length;
            }

            this.tokenIndex = (this.tokenIndex + 1) & 3;
            if (!this.tokens.TryGetValue(this.tokenIndex, out token))
                token = null;

            if (token == null)
            {
                token = new Token();
                this.tokens[this.tokenIndex] = token;
            }

            if (capInput == "")
                return token.type = TokenType.END;

            if ((capMatch = fpRegExp.Match(capInput)).Success)
            {
                token.type = TokenType.NUMBER;
                token.value = JSObject.ToNumber(GLOBAL, capMatch.Groups[0].Value);
            }
            else if ((capMatch = hxRegExp.Match(capInput)).Success)
            {
                token.type = TokenType.NUMBER;
                token.value = JSObject.ToNumber(GLOBAL, capMatch.Groups[0].Value);
            }
            else if ((capMatch = idRegExp.Match(capInput)).Success) // FIXME no ES3 unicode
            {       
                string id = capMatch.Groups[0].Value;
                if (!jsdefs.keywords.TryGetValue(id, out token.type))
                    token.type = TokenType.IDENTIFIER;
                token.value = id;
            }
            else if ((capMatch = qtRegExp.Match(capInput)).Success) //"){
            {
                char matchCh = capMatch.Groups[0].Value[0];
                int matched;
                token.type = TokenType.STRING;
                token.value = JSObject.StringLiteral(capInput, out matched);
                capMatch = new Regex("^" + matchCh + ".{" + matched.ToString() + "}" + matchCh).Match(capInput);
            }
            else if (this.scanOperand && (capMatch = reRegExp.Match(capInput)).Success)
            {
                token.type = TokenType.REGEXP;
                token.value = new Regex(capMatch.Groups[1].Value/*, capMatch.Groups[2].Value*/); //, capMatch.Groups[2].Value);
            }
            else if ((capMatch = opRegExp.Match(capInput)).Success)
            {
                string op = capMatch.Groups[0].Value;
                if (jsdefs.assignOps.ContainsKey(op) && capInput[op.Length] == '=')
                {
                    token.type = TokenType.ASSIGN;
                    token.assignOp = jsdefs.assignOps[op];
                    capMatch = dtRegExp.Match(op + "=");
                }
                else
                {
                    token.type = jsdefs.tokenWords[op];
                    if (this.scanOperand &&
                       (token.type == TokenType.PLUS))
                        token.type = TokenType.UNARY_PLUS;
                    if (this.scanOperand &&
                        (token.type == TokenType.MINUS))
                        token.type = TokenType.UNARY_MINUS;
                    token.assignOp = TokenType.NULL;
                }
                token.value = op;
            }
            else if (this.scanNewlines && (capMatch = (elRegExp.Match(capInput))).Success)
            {
                token.type = TokenType.NEWLINE;
            }
            else
            {
                throw this.newSyntaxError("Illegal token");
            }

            token.start = this.cursor;
            this.cursor += capMatch.Groups[0].Value.Length;
            token.end = this.cursor;
            token.lineno = this.lineno;
            return token.type;
        }

        public object unget()
        {
            if (++this.lookahead == 4) throw new Exception("PANIC: too much lookahead!");
            this.tokenIndex = (this.tokenIndex - 1) & 3;
            return JSUndefined.Undefined;
        }

        public Exception newSyntaxError(string m)
        {
            SyntaxError e = new SyntaxError(m, this.filename, this.lineno);
            e.source = this.source;
            e.cursor = this.cursor;
            return e;
        }
    }

    public class SyntaxError : Exception
    {
        string mFilename;
        public string Filename { get { return mFilename; } }
        int mLine;
        public int Line { get { return mLine; } }
        public string source;
        public int cursor;
        public SyntaxError(string m, string f, int l)
            : base(m)
        {
            mFilename = f;
            mLine = l;
        }
    }
}
