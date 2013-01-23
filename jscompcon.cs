using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pygmalion
{
    class Thunk : JSObject
    {
        ExecutionContext x;
        JSObjectBase f;
        JSObjectBase thisOb;

        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            ExecutionContext newScope = x;
            newScope.parent = GLOBAL.currentContext;
            newScope.thisOb = thisOb;
            GLOBAL.currentContext = newScope;
            return f.Call(GLOBAL, t, a, newScope);
        }

        public Thunk(JSObjectBase thisOb, JSObjectBase f, ExecutionContext x)
        {
            this.thisOb = thisOb;
            this.f = f;
            this.x = x;
        }
    }
}
