using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pygmalion
{
    internal class BoolObject : JSObject
    {
        bool mValue;
        public override string Class { get { return "Boolean"; } }
        public override string ToString() { return mValue ? "true" : "false"; }
        public object ValueOf() { return mValue; }
        [GlobalArg]
        public BoolObject(ExecutionContext GLOBAL, bool val) 
        { 
            mValue = val;
            BooleanFun StaticBooleanFun = (BooleanFun)jsexec.GlobalOrNull(GLOBAL, "StaticBooleanFun");
            this.DefProp(GLOBAL, "prototype", StaticBooleanFun != null ? StaticBooleanFun.GetItem(GLOBAL, "prototype").GetValue(GLOBAL) : null, false, false, false);
            DefProp(GLOBAL, "constructor", StaticBooleanFun, false, false, false, true);
        }
    }

    internal class BooleanFun : JSObject
    {
        public override string Class
        {
            get
            {
                return "function";
            }
        }

        public BooleanFun(ExecutionContext GLOBAL)
        {
            JSObject prototype = new BoolObject(GLOBAL, false);
            DefProp(GLOBAL, "prototype", prototype, false, false, false);
            prototype.DefProp(GLOBAL, "constructor", this);
            prototype.DefProp(GLOBAL, "length", (double)1, false, false, false);
            prototype.SetItem(GLOBAL, "toString", new JSNativeMethod(typeof(BoolObject), "ToString"));
            prototype.SetItem(GLOBAL, "valueOf", new JSNativeMethod(typeof(BoolObject), "ValueOf"));
            DefProp(GLOBAL, "length", (double)1, false, false, false);
        }

        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            int alen = (int)JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "length").GetValue(GLOBAL));
            if (alen == 0) return false;
            return JSObject.ToBool(GLOBAL, a.GetItem(GLOBAL, "0").GetValue(GLOBAL));
        }

        public override object Construct(ExecutionContext GLOBAL, JSObjectBase args, ExecutionContext x)
        {
            JSObject result = new BoolObject(GLOBAL, (int)JSObject.ToNumber(GLOBAL, args.GetItem(GLOBAL, "length").GetValue(GLOBAL)) > 0 && JSObject.ToBool(GLOBAL, args.GetItem(GLOBAL, "0").GetValue(GLOBAL)));
            result.DefProp(GLOBAL, "constructor", this);
            return result;
        }
    }
}
