/* ***** BEGIN LICENSE BLOCK *****
 * vim: set ts=4 sw=4 et tw=80:
 *
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
 * Execution of parse trees.
 *
 * Standard classes except for eval, Function, Array, and String are borrowed
 * from the host JS environment.  Function is metacircular.  Array and String
 * are reflected via wrapping the corresponding native constructor and adding
 * an extra level of prototype-based delegation.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace pygmalion
{
    public class BreakException : Exception
    {
        public BreakException() { }
    }

    public class ContinueException : Exception
    {
        public ContinueException() { }
    }

    public class ThrownException : Exception
    {
        public readonly object Value;
        public ThrownException(object value) { Value = value; }
    }

    public class ReturnException : Exception
    {
    }

    public class FalseReturn : Exception
    {
    }

    public class ScriptException : Exception
    {
        Node n;
        public override string Message
        {
            get
            {
                return string.Format("{0}\nWhile Evaluating {1} at {2}:{3}", base.Message, Node.ToString(false, n, ""), n.filename, n.lineno);
            }
        }
        internal ScriptException(string message, Node n, Exception inner) : base(message, inner)
        {
            this.n = n;
        }
    }

    public class jsexec
    {
        public bool TraceExec = false;
        ObjectFun StaticObjectFun;
        StringFun StaticStringFun;
        BooleanFun StaticBooleanFun;
        ArrayFun StaticArrayFun;
        NumberFun StaticNumberFun;
        FunctionFun StaticFunctionFun;
        NumberObject StaticNumberObject;
        JSObject DatePrototype;
        public readonly ExecutionContext GLOBAL = new ExecutionContext();

        void CollectArguments(JSObjectBase v, Node n, ref int k, ExecutionContext x)
        {
            int i;
            object u;

            if (n.type == TokenType.LIST || n.type == TokenType.COMMA)
            {
                for (i = 0; i < n.Count; i++)
                    CollectArguments(v, n[i], ref k, x);
            }

            if (!(n.type == TokenType.LIST || n.type == TokenType.COMMA))
            {
                u = Reference.GetValue(GLOBAL, execute(n, x));
                v.SetItem(GLOBAL, k.ToString(), new JSSimpleProperty(k.ToString(), u));
                k++;
            }
        }

        internal object apply(bool callMe, JSObjectBase f, object t, JSObjectBase args, ExecutionContext x)
        {
            // Curse ECMA again!
            if (t == JSUndefined.Undefined || t == null)
                t = x;

            if (!(t is JSObjectBase))
                t = JSObject.ToObject(GLOBAL, t);

            if (args == JSUndefined.Undefined || args == null)
            {
                args = new JSObject();
                args.SetItem(GLOBAL, "length", new JSSimpleProperty("length", 0.0, false, false, true));
            }
            else if (args is JSArray)
            {
                var vv = new JSObject();
                int ii, jj;
                for (ii = 0, jj = (int)JSObject.ToNumber(GLOBAL, args.GetItem(GLOBAL, "length").GetValue(GLOBAL)); ii < jj; ii++)
                    vv.SetItem(GLOBAL, ii.ToString(), new JSSimpleProperty(ii.ToString(), args.GetItem(GLOBAL, ii.ToString()).GetValue(GLOBAL), false, false, true));
                vv.SetItem(GLOBAL, "length", new JSSimpleProperty("length", (double)ii, false, false, true));
                args = vv;
            }

            if (callMe)
                return f.Call(GLOBAL, t, args, x);
            else
                return f.Construct(GLOBAL, args, x);
        }

        internal object Add(object res1, object res2)
        {
            object v;
            if (res1 is string || res2 is string)
            {
                string s1 = res1 != null ? JSObject.ToPrimitive(GLOBAL, res1) : "";
                string s2 = res2 != null ? JSObject.ToPrimitive(GLOBAL, res2) : "";
                v = s1 + s2;
            }
            else if (res1 is double || res2 is double || res1 is bool || res2 is bool)
            {
                v = (double)(JSObject.ToNumber(GLOBAL, res1) + JSObject.ToNumber(GLOBAL, res2));
            }
            else
            {
                v = JSObject.ToPrimitive(GLOBAL, res1) + JSObject.ToPrimitive(GLOBAL, res2);
            }
            return v;
        }

        internal object execute(Node n, ExecutionContext x) {
            int i, j;
            List<Node> aNode;
            ExecutionContext sEcon;
            JSObjectBase jaa;
            JSObjectBase f, tVal;
            object r, s, u = null, v = null;
            Node tNode = null, rNode = null, uNode = null;
            bool matchDefault = false;
            bool switchLoop;

            if (TraceExec)
                System.Console.Write("Execute[" + n.ToString() + " => ");

            try
            {
                switch (n.type)
                {
                    case TokenType.Function:
                        if (n.functionForm != StatementForm.DECLARED_FORM)
                        {
                            if (n.name == null || n.name == "" || n.functionForm == StatementForm.STATEMENT_FORM)
                            {
                                v = new FunctionObject(GLOBAL, n, x.scope);
                                if (n.functionForm == StatementForm.STATEMENT_FORM)
                                    x.scope.jobject.SetItem(GLOBAL, n.name, new JSSimpleProperty(n.name, v));
                            }
                            else
                            {
                                tVal = new JSObject();
                                ExecutionContext tmp = x.scope;
                                x.scope = new ExecutionContext();
                                x.scope.jobject = tVal;
                                x.scope.parent = tmp;
                                try
                                {
                                    v = new FunctionObject(GLOBAL, n, x.scope);
                                    tVal.SetItem(GLOBAL, n.name, new JSSimpleProperty(n.name, v, true, true));
                                }
                                finally
                                {
                                    x.scope = x.scope.parent;
                                }
                            }
                        }
                        break;

                    case TokenType.SCRIPT:
                        tVal = x.scope.jobject;
                        aNode = n.funDecls;
                        for (i = 0, j = aNode.Count; i < j; i++)
                        {
                            s = aNode[i].name;
                            f = new FunctionObject(GLOBAL, aNode[i], x.scope);
                            tVal.SetItem(GLOBAL, s.ToString(), new JSSimpleProperty(s.ToString(), f, x.type != CodeType.EVAL_CODE));
                        }
                        aNode = n.varDecls;
                        for (i = 0, j = aNode.Count; i < j; i++)
                        {
                            uNode = aNode[i];
                            s = uNode.name;
                            if (uNode.readOnly && tVal.HasOwnProperty(s.ToString()))
                            {
                                throw new TypeError
                                    ("Redeclaration of const " + s, uNode.filename, uNode.lineno);
                            }
                            if (uNode.readOnly || !tVal.HasOwnProperty(s.ToString()))
                            {
                                tVal.SetItem(GLOBAL, s.ToString(), new JSSimpleProperty(s.ToString(), JSUndefined.Undefined, x.type != CodeType.EVAL_CODE, uNode.readOnly));
                            }
                        }
                        // FALL THROUGH
                        for (i = 0, j = n.Count; i < j; i++)
                            execute(n[i], x);
                        break;

                    case TokenType.BLOCK:
                        for (i = 0, j = n.Count; i < j; i++)
                            execute(n[i], x);
                        break;

                    case TokenType.If:
                        if (JSObject.ToBool(GLOBAL, execute(n.condition, x)))
                            execute(n.thenPart, x);
                        else if (n.elsePart != null)
                            execute(n.elsePart, x);
                        break;

                    case TokenType.Switch:
                        s = execute(n.discriminant, x);
                        aNode = n.cases;
                        Node tt;
                        switchLoop = false;
                        for (i = 0, j = aNode.Count; ; i++)
                        {
                            if (i == j)
                            {
                                if (n.defaultIndex >= 0)
                                {
                                    i = n.defaultIndex - 1; // no case matched, do default
                                    matchDefault = true;
                                    continue;
                                }
                                break;                      // no default, exit switch_loop
                            }
                            tt = aNode[i];                       // next case (might be default!)
                            if (tt.type == TokenType.Case)
                            {
                                u = Reference.GetValue(GLOBAL, execute(tt.caseLabel, x));
                            }
                            else
                            {
                                if (!matchDefault)          // not defaulting, skip for now
                                    continue;
                                u = s;                      // force match to do default
                            }
                            if (object.Equals(u, s))
                            {
                                for (; ; )
                                {                  // this loop exits switch_loop
                                    if (tt.statements != null)
                                    {
                                        try
                                        {
                                            execute(tt.statements, x);
                                        }
                                        catch (BreakException)
                                        {
                                            if (x.target == n)
                                            {
                                                switchLoop = true;
                                                break;
                                            }
                                            else
                                            {
                                                throw;
                                            }
                                        }
                                    }
                                    if (++i == j)
                                    {
                                        switchLoop = true;
                                        break;
                                    }
                                    tNode = aNode[i];
                                }
                                // NOT REACHED
                            }
                            if (switchLoop) break;
                        }
                        break;

                    case TokenType.For:
                        if (n.setup != null)
                            Reference.GetValue(GLOBAL, execute(n.setup, x));
                        // FALL THROUGH
                        while (n.condition == null ||
                               JSObject.ToBool(GLOBAL, Reference.GetValue(GLOBAL, execute(n.condition, x))))
                        {
                            try
                            {
                                execute(n.body, x);
                            }
                            catch (BreakException)
                            {
                                if (x.target == n)
                                    break;
                                throw;
                            }
                            catch (ContinueException)
                            {
                                if (x.target == n)
                                {
                                    if (n.update != null)
                                        Reference.GetValue(GLOBAL, execute(n.update, x));
                                    if (n.condition != null && !JSObject.ToBool(GLOBAL, Reference.GetValue(GLOBAL, execute(n.condition, x))))
                                        break;
                                    else
                                        continue;
                                }
                                throw;
                            }
                            if (n.update != null)
                                Reference.GetValue(GLOBAL, execute(n.update, x));
                        }
                        break;

                    case TokenType.While:
                        while (n.condition != null ||
                               JSObject.ToBool(GLOBAL, Reference.GetValue(GLOBAL, execute(n.condition, x))))
                        {
                            try
                            {
                                execute(n.body, x);
                            }
                            catch (BreakException)
                            {
                                if (x.target == n)
                                    break;
                                throw;
                            }
                            catch (ContinueException)
                            {
                                if (x.target == n)
                                    continue;
                                throw;
                            }
                            if (n.update != null)
                                Reference.GetValue(GLOBAL, execute(n.update, x));
                        }
                        break;

                    case TokenType.FOR_IN:
                        uNode = n.varDecl;
                        if (uNode != null)
                            execute(uNode, x);
                        rNode = n.iterator;
                        v = Reference.GetValue(GLOBAL, execute(n.jobject, x));

                        // ECMA deviation to track extant browser JS implementation behavior.
                        tVal = JSObject.ToObject(GLOBAL, v);
                        i = 0;
                        jaa = new JSArray(GLOBAL);
                        foreach (string ii in tVal.Properties)
                        {
                            string istr = (i++).ToString();
                            jaa.SetItem(GLOBAL, istr, new JSSimpleProperty(istr, ii));
                        }
                        for (j = i, i = 0; i < j; i++)
                        {
                            Reference.PutValue(GLOBAL, execute(rNode, x), jaa.GetItem(GLOBAL, i.ToString()).GetValue(GLOBAL));
                            try
                            {
                                execute(n.body, x);
                            }
                            catch (BreakException)
                            {
                                if (x.target != n)
                                    break;
                                throw;
                            }
                            catch (ContinueException)
                            {
                                if (x.target != n)
                                    continue;
                                throw;
                            }
                        }
                        break;

                    case TokenType.Do:
                        do
                        {
                            try
                            {
                                execute(n.body, x);
                            }
                            catch (BreakException)
                            {
                                if (x.target != n)
                                    break;
                                throw;
                            }
                            catch (ContinueException)
                            {
                                if (x.target != n)
                                    continue;
                                throw;
                            }
                        } while (JSObject.ToBool(GLOBAL, Reference.GetValue(GLOBAL, execute(n.condition, x))));
                        break;

                    case TokenType.Break:
                        x.target = n.target;
                        throw new BreakException();

                    case TokenType.Continue:
                        x.target = ((Node)n).target;
                        throw new ContinueException();

                    case TokenType.Try:
                        try
                        {
                            execute(n.tryBlock, x);
                        }
                        catch (Exception exn)
                        {
                            ThrownException ex = exn as ThrownException;
                            object e;
                            if (ex != null) e = ex.Value; else e = new JSInstanceWrapper(GLOBAL, exn);
                            j = n.catchClauses.Count;
                            x.result = JSUndefined.Undefined;
                            for (i = 0; ; i++)
                            {
                                if (i == j)
                                {
                                    throw;
                                }
                                tNode = n.catchClauses[i];
                                ExecutionContext xscope = new ExecutionContext();
                                xscope.jobject = new JSObject();
                                xscope.jobject.SetItem(GLOBAL, tNode.varName, new JSSimpleProperty(tNode.varName, e, false, false, true));
                                xscope.parent = x.scope;
                                x.scope = xscope;
                                try
                                {
                                    if (tNode.guard != null && !JSObject.ToBool(GLOBAL, Reference.GetValue(GLOBAL, execute(tNode.guard, x))))
                                        continue;
                                    execute(tNode.block, x);
                                    break;
                                }
                                finally
                                {
                                    x.scope = x.scope.parent;
                                }
                            }
                        }
                        finally
                        {
                            if (n.finallyBlock != null)
                                execute(n.finallyBlock, x);
                        }
                        break;

                    case TokenType.Throw:
                        x.result = Reference.GetValue(GLOBAL, execute(n.exception, x));
                        throw new ThrownException((double)(int)TokenType.Throw);

                    case TokenType.Return:
                        x.result = Reference.GetValue(GLOBAL, execute(n.valueNode, x));
                        throw new ReturnException();

                    case TokenType.With:
                        {
                            r = execute(n.jobject, x);
                            tVal = JSObject.ToObject(GLOBAL, Reference.GetValue(GLOBAL, r));
                            ExecutionContext tmp = x.scope;
                            x.scope = new ExecutionContext();
                            x.scope.jobject = tVal;
                            x.scope.parent = tmp;
                            try
                            {
                                execute(n.body, x);
                            }
                            finally
                            {
                                x.scope = x.scope.parent;
                            }
                        }
                        break;

                    case TokenType.Var:
                    case TokenType.Const:
                        for (i = 0, j = n.Count; i < j; i++)
                        {
                            uNode = n[i].initializer;
                            if (uNode == null)
                                continue;
                            string identName = n[i].name;
                            for (sEcon = x.scope; sEcon != null; sEcon = sEcon.parent)
                            {
                                if (sEcon.jobject.HasOwnProperty(identName))
                                    break;
                            }
                            u = Reference.GetValue(GLOBAL, execute(uNode, x));
                            if (n.type == TokenType.Const)
                                sEcon.jobject.SetItem(GLOBAL, identName, new JSSimpleProperty(identName, u, true, true, false));
                            else
                                sEcon.jobject.SetItem(GLOBAL, identName, new JSSimpleProperty(identName, u));
                        }
                        break;

                    case TokenType.Debugger:
                        throw new Exception("NYI: debugger");

                    case TokenType.SEMICOLON:
                        if (n.expression != null)
                            x.result = Reference.GetValue(GLOBAL, execute(n.expression, x));
                        break;

                    case TokenType.LABEL:
                        try
                        {
                            execute(n.statement, x);
                        }
                        catch (BreakException)
                        {
                            if (x.target != n)
                                throw;
                        }
                        break;

                    case TokenType.COMMA:
                        for (i = 0, j = n.Count; i < j; i++)
                            v = Reference.GetValue(GLOBAL, execute(n[i], x));
                        break;

                    case TokenType.ASSIGN:
                        r = execute(n[0], x);
                        TokenType tok = jsdefs.tokenWords[n.value.ToString()];
                        v = Reference.GetValue(GLOBAL, execute(n[1], x));
                        if (tok != TokenType.ASSIGN)
                        {
                            u = Reference.GetValue(GLOBAL, r);
                            if (tok != TokenType.PLUS)
                            {
                                double ld = JSObject.ToNumber(GLOBAL, u), bd = JSObject.ToNumber(GLOBAL, v);
                                long laa = (long)ld, bb = (long)bd;
                                int bint = (int)bb;
                                switch (tok)
                                {
                                    case TokenType.BITWISE_OR: ld = laa | bb; break;
                                    case TokenType.BITWISE_XOR: ld = laa ^ bb; break;
                                    case TokenType.BITWISE_AND: ld = laa & bb; break;
                                    case TokenType.LSH: ld = laa << bint; break;
                                    case TokenType.RSH: ld = laa >> bint; break;
                                    case TokenType.URSH: ld = ((uint)laa) >> bint; break;
                                    case TokenType.MINUS: ld = ld - bd; break;
                                    case TokenType.MUL: ld = ld * bd; break;
                                    case TokenType.DIV: ld = ld / bd; break;
                                    case TokenType.MOD: ld = laa % bb; break;
                                }
                                v = ld;
                            }
                            else
                            {
                                v = Add(u, v);
                            }
                        }
                        Reference.PutValue(GLOBAL, r, v);
                        break;

                    case TokenType.HOOK:
                        {
                            object res1 = Reference.GetValue(GLOBAL, execute(n[0], x));
                            v = JSObject.ToBool(GLOBAL, res1) ? Reference.GetValue(GLOBAL, execute(n[1], x))
                                                       : Reference.GetValue(GLOBAL, execute(n[2], x));
                        }
                        break;

                    case TokenType.OR:
                        {
                            object res1 = Reference.GetValue(GLOBAL, execute(n[0], x));
                            v = JSObject.ToBool(GLOBAL, res1) ? res1 : Reference.GetValue(GLOBAL, execute(n[1], x));
                        }
                        break;

                    case TokenType.AND:
                        {
                            object res1 = Reference.GetValue(GLOBAL, execute(n[0], x));
                            v = JSObject.ToBool(GLOBAL, res1) ? Reference.GetValue(GLOBAL, execute(n[1], x)) : res1;
                        }
                        break;

                    case TokenType.BITWISE_OR:
                        v = (double)((int)JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute(n[0], x))) | (int)JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute(n[1], x))));
                        break;

                    case TokenType.BITWISE_XOR:
                        v = (double)((int)JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute(n[0], x))) ^ (int)JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute(n[1], x))));
                        break;

                    case TokenType.BITWISE_AND:
                        v = (double)((int)JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute(n[0], x))) & (int)JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute(n[1], x))));
                        break;

                    case TokenType.EQ:
                        v = Reference.GetValue(GLOBAL, execute(n[0], x));
                        u = Reference.GetValue(GLOBAL, execute(n[1], x));
                        if ((v == null || v == JSUndefined.Undefined) && (u == null || u == JSUndefined.Undefined))
                            v = true;
                        else
                            v = JSObject.CompareTo(GLOBAL, v, u) == 0;
                        break;

                    case TokenType.NE:
                        if (((u == null || u is JSUndefined) && (v != null && !(v is JSUndefined))) ||
                            ((v == null || v is JSUndefined) && (u != null && !(u is JSUndefined))))
                            return true;

                        v = JSObject.CompareTo(GLOBAL, Reference.GetValue(GLOBAL, execute(n[0], x)), Reference.GetValue(GLOBAL, execute(n[1], x))) != 0;
                        break;

                    case TokenType.STRICT_EQ:
                        v = Object.ReferenceEquals(Reference.GetValue(GLOBAL, execute(n[0], x)), Reference.GetValue(GLOBAL, execute(n[1], x)));
                        break;

                    case TokenType.STRICT_NE:
                        v = !Object.ReferenceEquals(Reference.GetValue(GLOBAL, execute(n[0], x)), Reference.GetValue(GLOBAL, execute(n[1], x)));
                        break;

                    case TokenType.LT:
                        v = JSObject.CompareTo(GLOBAL, Reference.GetValue(GLOBAL, execute(n[0], x)), Reference.GetValue(GLOBAL, execute(n[1], x))) == -1;
                        break;

                    case TokenType.LE:
                        v = JSObject.CompareTo(GLOBAL, Reference.GetValue(GLOBAL, execute(n[0], x)), Reference.GetValue(GLOBAL, execute(n[1], x))) != 1;
                        break;

                    case TokenType.GE:
                        v = JSObject.CompareTo(GLOBAL, Reference.GetValue(GLOBAL, execute(n[0], x)), Reference.GetValue(GLOBAL, execute(n[1], x))) != -1;
                        break;

                    case TokenType.GT:
                        v = JSObject.CompareTo(GLOBAL, Reference.GetValue(GLOBAL, execute(n[0], x)), Reference.GetValue(GLOBAL, execute(n[1], x))) == 1;
                        break;

                    case TokenType.In:
                        v = execute(n[1], x);
                        tVal = Reference.GetValue(GLOBAL, v) as JSObjectBase;
                        if (tVal == null) throw new TypeError("invalid 'in' operand " + v.ToString());
                        v = tVal.HasProperty(GLOBAL, Reference.GetValue(GLOBAL, execute(n[0], x)).ToString());
                        break;

                    case TokenType.Instanceof:
                        u = Reference.GetValue(GLOBAL, execute(n[0], x));
                        tVal = Reference.GetValue(GLOBAL, execute(n[1], x)) as JSObjectBase;
                        if (tVal == null) throw new TypeError("Not an object in instanceof");
                        v = tVal.HasInstance(GLOBAL, u);
                        break;

                    case TokenType.LSH:
                        v = (double)((int)JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute(n[0], x))) << (int)JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute(n[1], x))));
                        break;

                    case TokenType.RSH:
                        v = (double)((int)JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute(n[0], x))) >> (int)JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute(n[1], x))));
                        break;

                    case TokenType.URSH:
                        v = (double)((int)JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute(n[0], x))) >> (int)JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute(n[1], x))));
                        break;

                    case TokenType.PLUS:
                        v = Add(Reference.GetValue(GLOBAL, execute(n[0], x)), Reference.GetValue(GLOBAL, execute(n[1], x)));
                        break;

                    case TokenType.MINUS:
                        v = JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute(n[0], x))) - JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute(n[1], x)));
                        break;

                    case TokenType.MUL:
                        v = JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute(n[0], x))) * JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute(n[1], x)));
                        break;

                    case TokenType.DIV:
                        v = JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute(n[0], x))) / JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute(n[1], x)));
                        break;

                    case TokenType.MOD:
                        v = JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute(n[0], x))) % (long)JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute(n[1], x)));
                        break;

                    case TokenType.Delete:
                        {
                            u = execute((Node)n[0], x);
                            Reference refer = u as Reference;
                            if (refer != null)
                            {
                                v = refer.GetBase().Delete(refer.GetPropertyName());
                            }
                        }
                        break;

                    case TokenType.Void:
                        Reference.GetValue(GLOBAL, execute((Node)n[0], x));
                        v = JSUndefined.Undefined;
                        break;

                    case TokenType.Typeof:
                        u = execute((Node)n[0], x);
                        if (u is Reference)
                        {
                            Reference refer = (Reference)u;
                            if (refer.GetBase() == null)
                            {
                                v = JSUndefined.Undefined;
                                break;
                            }
                            u = Reference.GetValue(GLOBAL, refer);
                        }
                        v = JSObject.Typeof(u).ToLower();
                        break;

                    case TokenType.NOT:
                        v = !JSObject.ToBool(GLOBAL, Reference.GetValue(GLOBAL, execute((Node)n[0], x)));
                        break;

                    case TokenType.BITWISE_NOT:
                        v = (double)~(long)JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute((Node)n[0], x)));
                        break;

                    case TokenType.UNARY_PLUS:
                        v = JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute((Node)n[0], x)));
                        break;

                    case TokenType.UNARY_MINUS:
                        v = -JSObject.ToNumber(GLOBAL, Reference.GetValue(GLOBAL, execute((Node)n[0], x)));
                        break;

                    case TokenType.INCREMENT:
                    case TokenType.DECREMENT:
                        {
                            object t = execute(n[0], x);
                            u = Reference.GetValue(GLOBAL, t);
                            if (n.postfix)
                                v = u;
                            Reference.PutValue(GLOBAL, t, n.type == TokenType.INCREMENT ? JSObject.ToNumber(GLOBAL, u) + 1 : JSObject.ToNumber(GLOBAL, u) - 1);
                            if (!n.postfix)
                                v = u;
                        }
                        break;

                    case TokenType.DOT:
                        {
                            r = execute((Node)n[0], x);
                            object t = Reference.GetValue(GLOBAL, r);
                            u = n[1].value;
                            v = new Reference(JSObject.ToObject(GLOBAL, t), u.ToString());
                        }
                        break;

                    case TokenType.INDEX:
                        {
                            r = execute(n[0], x);
                            object t = Reference.GetValue(GLOBAL, r);
                            u = Reference.GetValue(GLOBAL, execute(n[1], x));
                            v = new Reference(JSObject.ToObject(GLOBAL, t), u.ToString());
                        }
                        break;

                    case TokenType.LIST:
                        // Curse ECMA for specifying that arguments is not an Array object!
                        tVal = new JSObject();
                        int k = 0;
                        CollectArguments(tVal, n, ref k, x);
                        tVal.SetItem(GLOBAL, "length", new JSSimpleProperty("length", (double)k));
                        v = tVal;
                        break;

                    case TokenType.CALL:
                        {
                            r = execute(n[0], x);
                            JSObjectBase args = (JSObjectBase)execute(n[1], x);
                            JSObjectBase fun = (JSObjectBase)Reference.GetValue(GLOBAL, r);
                            tVal = (r is Reference) ? ((Reference)r).GetBase() : null;
                            if (tVal is Activation)
                                tVal = null;
                            try
                            {
                                v = apply(true, fun, tVal, args, x);
                            }
                            catch (TypeError te)
                            {
                                if (r is Reference)
                                    throw new TypeError(((Reference)r).GetPropertyName() + ": " + te.Message);
                                else
                                    throw;
                            }
                        }
                        break;

                    case TokenType.New:
                    case TokenType.NEW_WITH_ARGS:
                        {
                            r = execute(n[0], x);
                            JSObjectBase fun = (JSObjectBase)Reference.GetValue(GLOBAL, r);
                            if (n.type == TokenType.New)
                            {
                                jaa = new JSArray(GLOBAL);
                            }
                            else
                            {
                                jaa = (JSObjectBase)execute(n[1], x);
                            }
                            v = fun.Construct(GLOBAL, jaa, x);
                        }
                        break;

                    case TokenType.ARRAY_INIT:
                        JSArray jaa1 = new JSArray(GLOBAL);
                        for (i = 0, j = n.Count; i < j; i++)
                        {
                            if (n[i] != null)
                                jaa1[i] = new JSSimpleProperty(i.ToString(), Reference.GetValue(GLOBAL, execute(n[i], x)));
                        }
                        jaa1.length = j;
                        v = jaa1;
                        break;

                    case TokenType.OBJECT_INIT:
                        tVal = new JSObject();
                        for (i = 0, j = n.Count; i < j; i++)
                        {
                            Node tx = n[i];
                            if (tx.type == TokenType.PROPERTY_INIT)
                            {
                                tVal.SetItem(GLOBAL, tx[0].value.ToString(),
                                    new JSSimpleProperty
                                        (tx[0].value.ToString(),
                                         Reference.GetValue(GLOBAL, execute(tx[1], x))));
                            }
                            else
                            {
                                f = new FunctionObject(GLOBAL, tx, x.scope);
                                string tname = tx.name;
                                JSSimpleProperty prop;
                                if (!tx.fields.TryGetValue(tname, out prop))
                                    tx.fields[tname] = new JSSimpleProperty(tname, v);
                                if (tx.type == TokenType.GETTER)
                                    prop.DefineGetter(new Thunk(tVal, f, x));
                                else
                                    prop.DefineSetter(new Thunk(tVal, f, x));
                                tVal.SetItem(GLOBAL, tname, prop);
                            }
                        }
                        v = tVal;
                        break;

                    case TokenType.Null:
                        v = null;
                        break;

                    case TokenType.This:
                        v = x.thisOb;
                        break;

                    case TokenType.True:
                        v = true;
                        break;

                    case TokenType.False:
                        v = false;
                        break;

                    case TokenType.IDENTIFIER:
                        {
                            for (sEcon = x.scope; sEcon != null; sEcon = sEcon.parent)
                            {
                                if (sEcon.jobject != null && sEcon.jobject.HasProperty(GLOBAL, n.value.ToString()))
                                    break;
                            }
                            v = new Reference(sEcon != null ? sEcon.jobject : GLOBAL.jobject, n.value.ToString());
                        }
                        break;

                    case TokenType.NUMBER:
                    case TokenType.STRING:
                    case TokenType.REGEXP:
                        v = n.value;
                        break;

                    case TokenType.GROUP:
                        v = execute(n[0], x);
                        break;

                    default:
                        throw new TypeError("PANIC: unknown operation " + n.type.ToString() + ": " + n.ToString());
                }
            }
            catch (FalseReturn) { v = false; }
            catch (ContinueException) { throw; }
            catch (BreakException) { throw; }
            catch (ThrownException) { throw; }
            catch (ReturnException) { throw; }
            catch (Exception e)
            {
                throw new ScriptException(e.Message, n, e);
            }
            if (TraceExec)
                System.Console.WriteLine(v == null ? "(null)" : v.ToString() + " ]");

            return v;
        }

        public object eval(string s, string f, int l)
        {
            ExecutionContext x = GLOBAL.currentContext;
            ExecutionContext x2 = new ExecutionContext(GLOBAL);
            x2.thisOb = x.thisOb;
            GLOBAL.currentContext = x2;
            try
            {
                execute(jsparse.parse(GLOBAL, s, f, l), x2);
            }
            catch (ThrownException)
            {
                if (JSObject.ToBool(GLOBAL, x))
                {
                    x.result = x2.result;
                    throw;
                }
                throw new ThrownException(x2.result);
            }
            finally
            {
                GLOBAL.currentContext = x;
            }
            return x2.result;
        }

        public static object evaluate(string s, string f, int l)
        {
            jsexec je = new jsexec();
            object result = je.eval(s, f, l);
            return result;
        }

        public jsexec()
        {
            GLOBAL.jobject = new JSObject();
            GLOBAL.jobject = GLOBAL.thisOb = new JSObject();

            StaticObjectFun = new ObjectFun(GLOBAL);
            GLOBAL.jobject.SetItem(GLOBAL, "StaticObjectFun", new JSSimpleProperty("StaticObjectFun", StaticObjectFun));
            StaticStringFun = new StringFun(GLOBAL);
            GLOBAL.jobject.SetItem(GLOBAL, "StaticStringFun", new JSSimpleProperty("StaticStringFun", StaticStringFun));
            StaticBooleanFun = new BooleanFun(GLOBAL);
            GLOBAL.jobject.SetItem(GLOBAL, "StaticBooleanFun", new JSSimpleProperty("StaticBooleanFun", StaticBooleanFun));
            StaticArrayFun = new ArrayFun(GLOBAL);
            GLOBAL.jobject.SetItem(GLOBAL, "StaticArrayFun", new JSSimpleProperty("StaticArrayFun", StaticArrayFun));
            StaticNumberFun = new NumberFun(GLOBAL);
            GLOBAL.jobject.SetItem(GLOBAL, "StaticNumberFun", new JSSimpleProperty("StaticNumberFun", StaticNumberFun));
            StaticFunctionFun = new FunctionFun(GLOBAL);
            GLOBAL.jobject.SetItem(GLOBAL, "StaticFunctionFun", new JSSimpleProperty("StaticFunctionFun", StaticFunctionFun));
            StaticNumberObject = new NumberObject(GLOBAL, 0.0);
            DatePrototype = new JSDate(GLOBAL);
            GLOBAL.jobject.SetItem(GLOBAL, "DatePrototype", new JSSimpleProperty("DatePrototype", DatePrototype));
            JSDate.SetupPrototype(GLOBAL, DatePrototype);

            GLOBAL.jobject.SetItem(GLOBAL, "toString", new JSNativeMethod(typeof(JSObject), "StaticToString"));

            /* Core types */
            JSObject thisOb = (JSObject)GLOBAL.thisOb;
            thisOb.DefProp(GLOBAL, "Object", StaticObjectFun);
            thisOb.DefProp(GLOBAL, "Function", StaticFunctionFun);
            thisOb.DefProp(GLOBAL, "Boolean", StaticBooleanFun);
            thisOb.DefProp(GLOBAL, "Number", StaticNumberFun);
            thisOb.DefProp(GLOBAL, "String", StaticStringFun);
            thisOb.DefProp(GLOBAL, "Array", StaticArrayFun);

            /* Types that work like classes */
            thisOb.DefProp(GLOBAL, "Math", JSClassWrapper.RegisterClass(GLOBAL, typeof(pygmalion.JSMath)));
            thisOb.DefProp(GLOBAL, "Date", JSClassWrapper.RegisterClass(GLOBAL, typeof(pygmalion.JSDate)));

            /* Standard library */
            thisOb.DefProp(GLOBAL, "decodeURI", new DecodeURIFun());
            thisOb.DefProp(GLOBAL, "decodeURIComponent", new DecodeURIComponentFun());
            thisOb.DefProp(GLOBAL, "encodeURI", new EncodeURIFun());
            thisOb.DefProp(GLOBAL, "encodeURIComponent", new EncodeURIComponentFun());
            thisOb.DefProp(GLOBAL, "escape", new EscapeFun());
            thisOb.DefProp(GLOBAL, "eval", new EvalFun());
            thisOb.DefProp(GLOBAL, "gc", new GcFun());
            thisOb.DefProp(GLOBAL, "Infinity", double.PositiveInfinity, false, false, false);
            thisOb.DefProp(GLOBAL, "isFinite", new isFiniteFun());
            thisOb.SetItem(GLOBAL, "isNaN", new JSNativeMethod(typeof(Double), "IsNaN"));
            thisOb.DefProp(GLOBAL, "parseFloat", new ParseFloat(), false, false, false);
            thisOb.DefProp(GLOBAL, "parseInt", new ParseInt(), false, false, false);
            thisOb.DefProp(GLOBAL, "NaN", double.NaN, false, false, false);
            thisOb.DefProp(GLOBAL, "unescape", new UnescapeFun());
            thisOb.DefProp(GLOBAL, "version", new VersionFun());

            GLOBAL.jobject.SetItem(GLOBAL, "JSExec", new JSSimpleProperty("JSExec", this));
            GLOBAL.thisOb = GLOBAL.jobject;
            GLOBAL.currentContext = new ExecutionContext(GLOBAL);
            GLOBAL.scope = GLOBAL.currentContext;
        }

        public static object GlobalOrNull(ExecutionContext GLOBAL, string ident)
        {
            JSProperty prop = GLOBAL.jobject.GetItem(GLOBAL, ident);
            if (prop != null) return prop.GetValue(GLOBAL); else return null;
        }
    }

    internal class EvalFun : JSObject
    {
        public override string Class
        {
            get
            {
                return "function";
            }
        }

        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            if (!a.HasProperty(GLOBAL, "length")) return JSUndefined.Undefined;
            int alen = (int)JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "length").GetValue(GLOBAL));
            if (alen < 1) return JSUndefined.Undefined;

            object s = a.GetItem(GLOBAL, "0").GetValue(GLOBAL);
            if (!(s is string)) return s;
            string str = (string)s;

            ExecutionContext x2 = new ExecutionContext(CodeType.EVAL_CODE);
            x2.thisOb = x.thisOb;
            x2.caller = x.caller;
            x2.callee = x.callee;
            x2.scope = x.scope;
            GLOBAL.currentContext = x2;
            try {
                jsexec JSExec = (jsexec)GLOBAL.jobject.GetItem(GLOBAL, "JSExec").GetValue(GLOBAL);
                JSExec.execute(jsparse.parse(GLOBAL, s.ToString(), null, 0), x2);
            } catch (ThrownException) {
                x.result = x2.result;
                throw;
            } finally {
                GLOBAL.currentContext = x;
            }
            return x2.result;
        }
    }
}
