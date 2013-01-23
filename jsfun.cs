using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace pygmalion
{
    class FunctionObject : JSObject
    {
        public Node node;
        public ExecutionContext scope;
        public override string Class
        {
            get
            {
                return "Function";
            }
        }

        [Browsable(false)]
        public override string ToString()
        {
            return Node.ToString(false, node, "");
        }

        public FunctionObject(ExecutionContext GLOBAL, Node node, ExecutionContext scope)
        {
            this.node = node;
            if (this.node.type == TokenType.SCRIPT)
                this.node = this.node[0];
            this.scope = scope;
            this.SetItem(GLOBAL, "length", new JSSimpleProperty("length", (double)node.fparams.Count, true, true, true));
            JSObject prototype = new JSObject();
            this.SetItem(GLOBAL, "prototype", new JSSimpleProperty("prototype", prototype, true));
            prototype.SetItem(GLOBAL, "toString", new JSNativeMethod(typeof(JSObject), "ToString"));
            prototype.SetItem(GLOBAL, "valueOf", new JSNativeMethod(typeof(JSObject), "ToPrimitive"));
            this.SetItem(GLOBAL, "constructor", new JSSimpleProperty("constructor", this, false, false, true));
        }

        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            ExecutionContext x2 = new ExecutionContext(CodeType.FUNCTION_CODE);
            x2.thisOb = (JSObjectBase)(JSObject.ToBool(GLOBAL, t) ? t : GLOBAL.jobject);
            x2.caller = x;
            x2.callee = this;
            a.SetItem(GLOBAL, "callee", new JSSimpleProperty("callee", this, false, false, true));
            Node f = this.node;
            x2.scope = new ExecutionContext();
            x2.scope.parent = this.scope;
            x2.scope.jobject = new Activation(GLOBAL, f, a);

            GLOBAL.currentContext = x2;

            try
            {
                jsexec JSExec = (jsexec)GLOBAL.jobject.GetItem(GLOBAL, "JSExec").GetValue(GLOBAL);
                JSExec.execute(f.body == null ? f : f.body, x2);
            }
            catch (ReturnException)
            {
                return x2.result;
            }
            catch (ThrownException)
            {
                x.result = x2.result;
                throw;
            }
            finally
            {
                GLOBAL.currentContext = x;
            }

            return JSUndefined.Undefined;
        }

        public override object Construct(ExecutionContext GLOBAL, JSObjectBase a, ExecutionContext x)
        {
            JSObjectBase o = new JSObject();
            object p = this.GetItem(GLOBAL, "prototype").GetValue(GLOBAL);

            if (!(p == null || p is JSUndefined || p is double || p is string || p is bool))
                o.SetItem(GLOBAL, "prototype", new JSSimpleProperty("prototype", p, false, false, true));

            object v = Call(GLOBAL, o, a, x);
            if (v == null || v is JSUndefined || v is double || v is string || v is bool)
                return o;
            else
                return v;
        }
    }

    internal class FunctionFun : JSObject
    {
        public override string Class
        {
            get
            {
                return "function";
            }
        }
        public FunctionFun(ExecutionContext GLOBAL)
        {
            Node functionNode = new Node(new Tokenizer(GLOBAL, "", null, 0), TokenType.Function);
            JSObject prototype = new FunctionObject(GLOBAL, functionNode, GLOBAL.currentContext);
            DefProp(GLOBAL, "prototype", prototype, false, false, false);
            prototype.DefProp(GLOBAL, "constructor", this);
        }
        static Regex argRe = new Regex("^\\w+([ \t]*,[ \t]*(\\w+)+)*$");
        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            JSObject result;
            int i, alen = (int)JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "length").GetValue(GLOBAL));
            string fundef = "function anonymous(";
            string comma = "";
            for (i = 0; i < alen - 1; i++)
            {
                string arg = a.GetItem(GLOBAL, i.ToString()).GetValue(GLOBAL).ToString();
                if (!argRe.IsMatch(arg)) throw new SyntaxError("Malformed parameter in function", null, 0);
                fundef += comma + arg;
                comma = ",";
            }
            fundef += ") { " + (alen > 0 ? a.GetItem(GLOBAL, i.ToString()).GetValue(GLOBAL).ToString() : "") + " }";
            Node n = jsparse.parse(GLOBAL, fundef, null, 0);
            result = new FunctionObject(GLOBAL, n, x);
            DefProp(GLOBAL, "length", (double)n.fparams.Count, false, false, false);
            return result;
        }
        public override object Construct(ExecutionContext GLOBAL, JSObjectBase args, ExecutionContext x)
        {
            JSObject result = (JSObject)Call(GLOBAL, x, args, x);
            result.DefProp(GLOBAL, "constructor", this);
            return result;
        }
    }
}
