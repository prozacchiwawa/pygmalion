using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pygmalion
{
    class Token
    {
        public TokenType type, assignOp = TokenType.NULL;
        public object value;
        public int lineno, start, end;
        public Token() { this.type = TokenType.NULL; }
        public Token(TokenType type) { this.type = type; }
    }
}
