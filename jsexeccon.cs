using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pygmalion
{
    public class ExecutionContext
    {
        internal CodeType type;
        internal Node target;
        public JSObjectBase thisOb;
        public object result;
        public object callee;
        public ExecutionContext caller;
        public JSObjectBase jobject;
        public ExecutionContext scope;
        public ExecutionContext parent;
        public ExecutionContext currentContext;

        public ExecutionContext(bool dummy) { }

        public ExecutionContext(ExecutionContext GLOBAL) : this()
        {
            scope.thisOb = GLOBAL.jobject;
            scope.jobject = GLOBAL.jobject;
        }

        public ExecutionContext()
        { 
            type = CodeType.EVAL_CODE;
            scope = new ExecutionContext(false);
            result = JSUndefined.Undefined;
        }

        public ExecutionContext(CodeType t) : this()
        {
            type = t;
        }
    }
}
