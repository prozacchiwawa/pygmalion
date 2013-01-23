/* ***** BEGIN LICENSE BLOCK *****
 * Version: MPL 1.1/GPL 2.0/LGPL 2.1
 *
 * The contents of this file are subject to the Mozilla Public License Version
 * 1.1 (the "License"); you may not use this file except in compliance with
 * the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 *
 * Software distributed under the License is distributed on an "AS IS" basis,
 * WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License
 * for the specific language governing rights and limitations under the
 * License.
 *
 * The Original Code is the Narcissus JavaScript engine.
 *
 * The Initial Developer of the Original Code is
 * Brendan Eich <brendan@mozilla.org>.
 * Portions created by the Initial Developer are Copyright (C) 2004
 * the Initial Developer. All Rights Reserved.
 *
 * Contributor(s):
 *
 * Alternatively, the contents of this file may be used under the terms of
 * either the GNU General Public License Version 2 or later (the "GPL"), or
 * the GNU Lesser General Public License Version 2.1 or later (the "LGPL"),
 * in which case the provisions of the GPL or the LGPL are applicable instead
 * of those above. If you wish to allow use of your version of this file only
 * under the terms of either the GPL or the LGPL, and not to allow others to
 * use your version of this file under the terms of the MPL, indicate your
 * decision by deleting the provisions above and replace them with the notice
 * and other provisions required by the GPL or the LGPL. If you do not delete
 * the provisions above, a recipient may use your version of this file under
 * the terms of any one of the MPL, the GPL or the LGPL.
 *
 * ***** END LICENSE BLOCK ***** */

/*
 * Narcissus - JS implemented in JS.
 *
 * Well-known constants and lookup tables.  Many consts are generated from the
 * tokens table via eval to minimize redundancy, so consumers must be compiled
 * separately to take advantage of the simple switch-case constant propagation
 * done by SpiderMonkey.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace pygmalion
{
    public enum TokenType
    {
        NULL = -1,
        // End of source.
        END,

        // Operators and punctuators.  Some pair-wise order matters, e.g. (+, -)
        // and (UNARY_PLUS, UNARY_MINUS).
        NEWLINE, SEMICOLON,
        COMMA,
        ASSIGN,
        HOOK, COLON, CONDITIONAL,
        OR,
        AND,
        BITWISE_OR,
        BITWISE_XOR,
        BITWISE_AND,
        EQ, NE, STRICT_EQ, STRICT_NE,
        LT, LE, GE, GT,
        LSH, RSH, URSH,
        PLUS, MINUS,
        MUL, DIV, MOD,
        NOT, BITWISE_NOT, UNARY_PLUS, UNARY_MINUS,
        INCREMENT, DECREMENT,
        DOT,
        LEFT_BRACKET, RIGHT_BRACKET,
        LEFT_CURLY, RIGHT_CURLY,
        LEFT_PAREN, RIGHT_PAREN,

        // Nonterminal tree node type codes.
        SCRIPT, BLOCK, LABEL, FOR_IN, CALL, NEW_WITH_ARGS, INDEX,
        ARRAY_INIT, OBJECT_INIT, PROPERTY_INIT, GETTER, SETTER,
        GROUP, LIST,

        // Terminals.
        IDENTIFIER, NUMBER, STRING, REGEXP,

        // Keywords.
        Break,
        Case, Catch, Const, Continue,
        Debugger, Default, Delete, Do,
        Else, Enum,
        False, Finally, For, Function,
        If, In, Instanceof,
        New, Null,
        Return,
        Switch,
        This, Throw, True, Try, Typeof,
        Var, Void,
        While, With
    }
    public class jsdefs
    {
        static string[] tokensList = new string[] {
            // End of source.
            "END",

            // Operators and punctuators.  Some pair-wise order matters, e.g. (+, -)
            // and (UNARY_PLUS, UNARY_MINUS).
            "\n", ";",
            ",",
            "=",
            "?", ":", "CONDITIONAL",
            "||",
            "&&",
            "|",
            "^",
            "&",
            "==", "!=", "===", "!==",
            "<", "<=", ">=", ">",
            "<<", ">>", ">>>",
            "+", "-",
            "*", "/", "%",
            "!", "~", "UNARY_PLUS", "UNARY_MINUS",
            "++", "--",
            ".",
            "[", "]",
            "{", "}",
            "(", ")",

            // Nonterminal tree node type codes.
            "SCRIPT", "BLOCK", "LABEL", "FOR_IN", "CALL", "NEW_WITH_ARGS", "INDEX",
            "ARRAY_INIT", "OBJECT_INIT", "PROPERTY_INIT", "GETTER", "SETTER",
            "GROUP", "LIST",

            // Terminals.
            "IDENTIFIER", "NUMBER", "STRING", "REGEXP",

            // Keywords.
            "break",
            "case", "catch", "const", "continue",
            "debugger", "default", "delete", "do",
            "else", "enum",
            "false", "finally", "for", "function",
            "if", "in", "instanceof",
            "new", "null",
            "return",
            "switch",
            "this", "throw", "true", "try", "typeof",
            "var", "void",
            "while", "with",
        };
        internal static Dictionary<TokenType, int> opArity = new Dictionary<TokenType, int>();
        static KeyValuePair<TokenType, int>[] opArityList = new KeyValuePair<TokenType, int>[] {
            new KeyValuePair<TokenType, int>(TokenType.COMMA, -2),
            new KeyValuePair<TokenType, int>(TokenType.ASSIGN, 2),
            new KeyValuePair<TokenType, int>(TokenType.HOOK, 3),
            new KeyValuePair<TokenType, int>(TokenType.OR, 2),
            new KeyValuePair<TokenType, int>(TokenType.AND, 2),
            new KeyValuePair<TokenType, int>(TokenType.BITWISE_OR, 2),
            new KeyValuePair<TokenType, int>(TokenType.BITWISE_XOR, 2),
            new KeyValuePair<TokenType, int>(TokenType.BITWISE_AND, 2),
            new KeyValuePair<TokenType, int>(TokenType.EQ, 2), 
            new KeyValuePair<TokenType, int>(TokenType.NE, 2), 
            new KeyValuePair<TokenType, int>(TokenType.STRICT_EQ, 2), 
            new KeyValuePair<TokenType, int>(TokenType.STRICT_NE, 2),
            new KeyValuePair<TokenType, int>(TokenType.LT, 2), 
            new KeyValuePair<TokenType, int>(TokenType.LE, 2), 
            new KeyValuePair<TokenType, int>(TokenType.GE, 2), 
            new KeyValuePair<TokenType, int>(TokenType.GT, 2), 
            new KeyValuePair<TokenType, int>(TokenType.In, 2), 
            new KeyValuePair<TokenType, int>(TokenType.Instanceof, 2),
            new KeyValuePair<TokenType, int>(TokenType.LSH, 2), 
            new KeyValuePair<TokenType, int>(TokenType.RSH, 2), 
            new KeyValuePair<TokenType, int>(TokenType.URSH, 2),
            new KeyValuePair<TokenType, int>(TokenType.PLUS, 2), 
            new KeyValuePair<TokenType, int>(TokenType.MINUS, 2),
            new KeyValuePair<TokenType, int>(TokenType.MUL, 2), 
            new KeyValuePair<TokenType, int>(TokenType.DIV, 2), 
            new KeyValuePair<TokenType, int>(TokenType.MOD, 2),
            new KeyValuePair<TokenType, int>(TokenType.Delete, 1), 
            new KeyValuePair<TokenType, int>(TokenType.Void, 1), 
            new KeyValuePair<TokenType, int>(TokenType.Typeof, 1),  // PRE_INCREMENT: 1, PRE_DECREMENT: 1,
            new KeyValuePair<TokenType, int>(TokenType.NOT, 1), 
            new KeyValuePair<TokenType, int>(TokenType.BITWISE_NOT, 1), 
            new KeyValuePair<TokenType, int>(TokenType.UNARY_PLUS, 1), 
            new KeyValuePair<TokenType, int>(TokenType.UNARY_MINUS, 1),
            new KeyValuePair<TokenType, int>(TokenType.INCREMENT, 1), 
            new KeyValuePair<TokenType, int>(TokenType.DECREMENT, 1),     // postfix
            new KeyValuePair<TokenType, int>(TokenType.New, 1), 
            new KeyValuePair<TokenType, int>(TokenType.NEW_WITH_ARGS, 2), 
            new KeyValuePair<TokenType, int>(TokenType.DOT, 2), 
            new KeyValuePair<TokenType, int>(TokenType.INDEX, 2), 
            new KeyValuePair<TokenType, int>(TokenType.CALL, 2),
            new KeyValuePair<TokenType, int>(TokenType.ARRAY_INIT, 1), 
            new KeyValuePair<TokenType, int>(TokenType.OBJECT_INIT, 1), 
            new KeyValuePair<TokenType, int>(TokenType.GROUP, 1)
        };

        internal static Dictionary<TokenType, int> opPrecedence = new Dictionary<TokenType, int>();
        static KeyValuePair<TokenType, int>[] opPrecedenceList = new KeyValuePair<TokenType, int>[] 
        {
            new KeyValuePair<TokenType, int>(TokenType.CALL, 0),
            new KeyValuePair<TokenType, int>(TokenType.NEW_WITH_ARGS, 0),
            new KeyValuePair<TokenType, int>(TokenType.INDEX, 0),
            new KeyValuePair<TokenType, int>(TokenType.GROUP, 0),
            new KeyValuePair<TokenType, int>(TokenType.SEMICOLON, 0),
            new KeyValuePair<TokenType, int>(TokenType.COMMA, 1),
            new KeyValuePair<TokenType, int>(TokenType.ASSIGN, 2), 
            new KeyValuePair<TokenType, int>(TokenType.HOOK, 2), 
            new KeyValuePair<TokenType, int>(TokenType.COLON, 2),
            // The above all have to have the same precedence, see bug 330975.
            new KeyValuePair<TokenType, int>(TokenType.OR, 4),
            new KeyValuePair<TokenType, int>(TokenType.AND, 5),
            new KeyValuePair<TokenType, int>(TokenType.BITWISE_OR, 6),
            new KeyValuePair<TokenType, int>(TokenType.BITWISE_XOR, 7),
            new KeyValuePair<TokenType, int>(TokenType.BITWISE_AND, 8),
            new KeyValuePair<TokenType, int>(TokenType.EQ, 9), 
            new KeyValuePair<TokenType, int>(TokenType.NE, 9), 
            new KeyValuePair<TokenType, int>(TokenType.STRICT_EQ, 9), 
            new KeyValuePair<TokenType, int>(TokenType.STRICT_NE, 9),
            new KeyValuePair<TokenType, int>(TokenType.LT, 10), 
            new KeyValuePair<TokenType, int>(TokenType.LE, 10), 
            new KeyValuePair<TokenType, int>(TokenType.GE, 10), 
            new KeyValuePair<TokenType, int>(TokenType.GT, 10), 
            new KeyValuePair<TokenType, int>(TokenType.In, 10), 
            new KeyValuePair<TokenType, int>(TokenType.Instanceof, 10),
            new KeyValuePair<TokenType, int>(TokenType.LSH, 11), 
            new KeyValuePair<TokenType, int>(TokenType.RSH, 11), 
            new KeyValuePair<TokenType, int>(TokenType.URSH, 11),
            new KeyValuePair<TokenType, int>(TokenType.PLUS, 12), 
            new KeyValuePair<TokenType, int>(TokenType.MINUS, 12),
            new KeyValuePair<TokenType, int>(TokenType.MUL, 13), 
            new KeyValuePair<TokenType, int>(TokenType.DIV, 13), 
            new KeyValuePair<TokenType, int>(TokenType.MOD, 13),
            new KeyValuePair<TokenType, int>(TokenType.Delete, 14), 
            new KeyValuePair<TokenType, int>(TokenType.Void, 14), 
            new KeyValuePair<TokenType, int>(TokenType.Typeof, 14), // PRE_INCREMENT: 14, PRE_DECREMENT: 14,
            new KeyValuePair<TokenType, int>(TokenType.NOT, 14), 
            new KeyValuePair<TokenType, int>(TokenType.BITWISE_NOT, 14), 
            new KeyValuePair<TokenType, int>(TokenType.UNARY_PLUS, 14), 
            new KeyValuePair<TokenType, int>(TokenType.UNARY_MINUS, 14),
            new KeyValuePair<TokenType, int>(TokenType.INCREMENT, 15), 
            new KeyValuePair<TokenType, int>(TokenType.DECREMENT, 15),     // postfix
            new KeyValuePair<TokenType, int>(TokenType.New, 16),
            new KeyValuePair<TokenType, int>(TokenType.DOT, 17),
        };

        // Operator and punctuator mapping from token to tree node type name.
        // NB: superstring tokens (e.g., ++) must come before their substring token
        // counterparts (+ in the example), so that the opRegExp regular expression
        // synthesized from this list makes the longest possible match.
        static KeyValuePair<string, string>[] opTypeNamesArray = new KeyValuePair<string, string>[] {
            new KeyValuePair<string, string>("\n",   "NEWLINE"),
            new KeyValuePair<string, string>(";",    "SEMICOLON"),
            new KeyValuePair<string, string>(",",    "COMMA"),
            new KeyValuePair<string, string>("?",    "HOOK"),
            new KeyValuePair<string, string>(":",    "COLON"),
            new KeyValuePair<string, string>("||",   "OR"),
            new KeyValuePair<string, string>("&&",   "AND"),
            new KeyValuePair<string, string>("|",    "BITWISE_OR"),
            new KeyValuePair<string, string>("^",    "BITWISE_XOR"),
            new KeyValuePair<string, string>("&",    "BITWISE_AND"),
            new KeyValuePair<string, string>("===",  "STRICT_EQ"),
            new KeyValuePair<string, string>("==",   "EQ"),
            new KeyValuePair<string, string>("=",    "ASSIGN"),
            new KeyValuePair<string, string>("!==",  "STRICT_NE"),
            new KeyValuePair<string, string>("!=",   "NE"),
            new KeyValuePair<string, string>("<<",   "LSH"),
            new KeyValuePair<string, string>("<=",   "LE"),
            new KeyValuePair<string, string>("<",    "LT"),
            new KeyValuePair<string, string>(">>>",  "URSH"),
            new KeyValuePair<string, string>(">>",   "RSH"),
            new KeyValuePair<string, string>(">=",   "GE"),
            new KeyValuePair<string, string>(">",    "GT"),
            new KeyValuePair<string, string>("++",   "INCREMENT"),
            new KeyValuePair<string, string>("--",   "DECREMENT"),
            new KeyValuePair<string, string>("+",    "PLUS"),
            new KeyValuePair<string, string>("-",    "MINUS"),
            new KeyValuePair<string, string>("*",    "MUL"),
            new KeyValuePair<string, string>("/",    "DIV"),
            new KeyValuePair<string, string>("%",    "MOD"),
            new KeyValuePair<string, string>("!",    "NOT"),
            new KeyValuePair<string, string>("~",    "BITWISE_NOT"),
            new KeyValuePair<string, string>(".",    "DOT"),
            new KeyValuePair<string, string>("[",    "LEFT_BRACKET"),
            new KeyValuePair<string, string>("]",    "RIGHT_BRACKET"),
            new KeyValuePair<string, string>("{",    "LEFT_CURLY"),
            new KeyValuePair<string, string>("}",    "RIGHT_CURLY"),
            new KeyValuePair<string, string>("(",    "LEFT_PAREN"),
            new KeyValuePair<string, string>(")",    "RIGHT_PAREN")
        };

        // Map assignment operators to their indexes in the tokens array.
        internal static string[] assignOpsList = new string[] { "|", "^", "&", "<<", ">>", ">>>", "+", "-", "*", "/", "%" };
        internal static Dictionary<string, TokenType> assignOps = new Dictionary<string, TokenType>();
        internal static Dictionary<TokenType, string> tokens = new Dictionary<TokenType, string>();
        internal static Dictionary<string, TokenType> tokenWords = new Dictionary<string, TokenType>();
        internal static Dictionary<string, string> opTypeNames = new Dictionary<string, string>();
        internal static Dictionary<string, TokenType> keywords = new Dictionary<string, TokenType>();

        static jsdefs()
        {
            int i;
            foreach (KeyValuePair<string, string> kv in opTypeNamesArray)
            {
                opTypeNames[kv.Key] = kv.Value;
            }
            for (i = 0; i < tokensList.Length; i++)
            {
                string t = tokensList[i];
                tokens[(TokenType)i] = t;
                if (new Regex("^[a-z]").IsMatch(t))
                {
                    keywords[t] = (TokenType)i;
                }
                tokenWords[t] = (TokenType)i;
            }
            foreach (string s in assignOpsList)
            {
                assignOps[s] = tokenWords[s];
            }
            foreach (KeyValuePair<TokenType, int> tt in opArityList)
            {
                opArity[tt.Key] = tt.Value;
            }
            foreach (KeyValuePair<TokenType, int> tt in opPrecedenceList)
            {
                opPrecedence[tt.Key] = tt.Value;
            }

            Tokenizer.InitTokenizer();
        }
    }
}
