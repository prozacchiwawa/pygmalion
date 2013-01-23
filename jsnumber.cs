using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pygmalion
{
    public class NumberObject : JSObject
    {
        double mValue;
        public override string ToString()
        {
            return mValue.ToString();
        }
        public NumberObject(ExecutionContext GLOBAL, double val) 
        {
            mValue = val;
            NumberFun StaticNumberFun = (NumberFun)jsexec.GlobalOrNull(GLOBAL, "StaticNumberFun");
            DefProp(GLOBAL, "prototype", StaticNumberFun != null ? StaticNumberFun.GetItem(GLOBAL, "prototype").GetValue(GLOBAL) : null, false, false, false);
            DefProp(GLOBAL, "constructor", StaticNumberFun != null ? StaticNumberFun : null, false, false, false);
        }
    }
    
    public class NumberFun : JSObject
    {
        public override string Class
        {
            get
            {
                return "function";
            }
        }
        public NumberFun(ExecutionContext GLOBAL)
        {
            DefProp(GLOBAL, "MAX_VALUE", double.MaxValue, true, false, false, true);
            DefProp(GLOBAL, "MIN_VALUE", double.Epsilon, true, false, false, true);
            DefProp(GLOBAL, "NaN", double.NaN, true, false, false, true);
            DefProp(GLOBAL, "NEGATIVE_INFINITY", double.NegativeInfinity, true, false, false, true);
            DefProp(GLOBAL, "POSITIVE_INFINITY", double.PositiveInfinity, true, false, false, true);
            DefProp(GLOBAL, "length", (double)1, false, false, false, false);
            JSObject prototype = new NumberObject(GLOBAL, 0.0);
            prototype.SetItem(GLOBAL, "toString", new JSNativeMethod(typeof(JSObject), "ToString"));
            prototype.SetItem(GLOBAL, "valueOf", new JSNativeMethod(typeof(JSObject), "ToPrimitive"));
            DefProp(GLOBAL, "prototype", prototype, false, false, false);
            prototype.DefProp(GLOBAL, "constructor", this);
        }
        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            object result;
            if (JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "length").GetValue(GLOBAL)) > 0)
                result = JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "0").GetValue(GLOBAL));
            else result = 0.0;
            return result;
        }
        public override object Construct(ExecutionContext GLOBAL, JSObjectBase args, ExecutionContext x)
        {
            JSObject result;
            if (JSObject.ToNumber(GLOBAL, args.GetItem(GLOBAL, "length").GetValue(GLOBAL)) > 0)
                result = new NumberObject(GLOBAL, JSObject.ToNumber(GLOBAL, args.GetItem(GLOBAL, "0").GetValue(GLOBAL)));
            else result = new NumberObject(GLOBAL, 0.0);
            result.DefProp(GLOBAL, "prototype", this.GetItem(GLOBAL, "prototype").GetValue(GLOBAL));
            result.DefProp(GLOBAL, "constructor", this);
            return result;
        }
    }
}
