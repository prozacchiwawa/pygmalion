using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;

namespace pygmalion
{
    public class ReferenceError : Exception
    {
        public ReferenceError(string msg) : base(msg) { }
    }

    public interface JSProperty
    {
        string Name { get; }
        bool ReadOnly { get; }
        bool DontEnum { get; }
        bool DontDelete { get; }
        bool Internal { get; }
        object GetValue(ExecutionContext GLOBAL);
        void SetValue(ExecutionContext GLOBAL, object value);
    }

    public class JSSimpleProperty : JSProperty
    {
        string mName;
        public string Name { get { return mName; } }
        bool mReadOnly;
        public bool ReadOnly { get { return mReadOnly; } }
        bool mDontEnum;
        public bool DontEnum { get { return mDontEnum; } }
        bool mDontDelete;
        public bool DontDelete { get { return mDontDelete; } }
        bool mInternal;
        public bool Internal { get { return mInternal; } }
        object mValue, mGetter, mSetter;
        public object GetValue(ExecutionContext GLOBAL)
        {
            JSObjectBase getFun = mGetter as JSObjectBase;
            if (getFun != null) return getFun.Call(GLOBAL, mValue, new JSArray(GLOBAL), GLOBAL.currentContext);
            else if (mSetter != null) return JSUndefined.Undefined;
            else return mValue;
        }

        public void SetValue(ExecutionContext GLOBAL, object value)
        {
            JSObjectBase setFun = mSetter as JSObjectBase;
            Trace.Assert(!(value is Reference));
            if (mGetter != null && setFun == null) return;
            else if (setFun != null)
                setFun.Call(GLOBAL, mValue, new JSArray(GLOBAL, new object[] { value }), GLOBAL.currentContext);
            else if (!ReadOnly) mValue = value;
        }

        public JSSimpleProperty(string name, object defaultValue)
            : this(name, defaultValue, true) { }
        public JSSimpleProperty(string name, object defaultValue, bool enumerable)
            : this(name, defaultValue, enumerable, true) { }
        public JSSimpleProperty(string name, object defaultValue, bool enumerable, bool removable)
            : this(name, defaultValue, enumerable, removable, true) { }
        public JSSimpleProperty(string name, object defaultValue, bool enumerable, bool removable, bool writable)
            : this(name, defaultValue, enumerable, removable, writable, false) { }
        public JSSimpleProperty(string name, object defaultValue, bool enumerable, bool removable, bool writable, bool internl)
        {
            mName = name;
            mValue = defaultValue;
            mDontEnum = !enumerable;
            mDontDelete = !removable;
            mReadOnly = !writable;
            mInternal = internl;
        }
        public void DefineGetter(object func)
        {
            mGetter = func;
        }
        public void DefineSetter(object func)
        {
            mSetter = func;
        }
    }

    internal class JSNativeProperty : JSProperty
    {
        object mThisRef;
        PropertyInfo mPropertyInfo;
        public string Name
        {
            get
            {
                return mPropertyInfo.Name;
            }
        }
        bool mReadOnly;
        public bool ReadOnly { get { return mReadOnly || !mPropertyInfo.CanWrite; } }
        bool mDontEnum;
        public bool DontEnum { get { return mDontEnum; } }
        public bool DontDelete { get { return true; } }
        public bool Internal { get { return true; } }
        public object GetValue(ExecutionContext GLOBAL)
        {
            return JSObject.ToJS(GLOBAL, mPropertyInfo.GetValue(mThisRef, new object[] { }));
        }
        public void SetValue(ExecutionContext GLOBAL, object value)
        {
            object toset;
            if (!JSObject.ConvertToType(GLOBAL, value, mPropertyInfo.PropertyType, out toset))
                throw new TypeError("Can't use object " + value.ToString() + " for .net field " + mThisRef.GetType().Name + "." + mPropertyInfo.Name);
            mPropertyInfo.SetValue(mThisRef, toset, new object[] { });
        }
        public JSNativeProperty(object thisRef, PropertyInfo propInfo)
        {
            mThisRef = thisRef;
            mPropertyInfo = propInfo;
            object[] attributes = mPropertyInfo.GetCustomAttributes(true);
            mDontEnum = false;
            mReadOnly = false;
            foreach (object attribute in attributes)
            {
                if (attribute is BrowsableAttribute)
                    mDontEnum = !((BrowsableAttribute)attribute).Browsable;
                if (attribute is ReadOnlyAttribute)
                    mReadOnly = !((ReadOnlyAttribute)attribute).IsReadOnly || !mPropertyInfo.CanWrite;
            }
        }
    }

    internal class JSNativeEvent : JSProperty, IDisposable
    {
        object mThisRef;
        EventInfo mEventInfo;
        JSArray mHookedUp;
        Delegate mCallDelegate;
        ExecutionContext GLOBAL;

        public string Name
        {
            get
            {
                return mEventInfo.Name;
            }
        }
        public bool ReadOnly { get { return true; } }
        public bool DontEnum { get { return true; } }
        public bool DontDelete { get { return true; } }
        public bool Internal { get { return true; } }
        public object GetValue(ExecutionContext GLOBAL)
        {
            return mHookedUp;
        }
        public void SetValue(ExecutionContext GLOBAL, object value) { }
        public void TriggerEvent(object sender, EventArgs args)
        {
            object senderOb = JSObject.ToJS(GLOBAL, sender), argsOb = JSObject.ToJS(GLOBAL, args);
            JSArray argArray = new JSArray(GLOBAL, new object[] { senderOb, argsOb });
            foreach (object ob in mHookedUp)
            {
                JSObjectBase obBase = ob as JSObjectBase;
                if (obBase != null)
                    obBase.Call(GLOBAL, ob, argArray, GLOBAL.currentContext);
            }
        }
        public void Dispose()
        {
            mEventInfo.RemoveEventHandler(mThisRef, mCallDelegate);
        }
        public delegate void TriggerEventDelegate(object sender, EventArgs args);
        public JSNativeEvent(ExecutionContext GLOBAL, object thisRef, EventInfo eventInfo)
        {
            this.GLOBAL = GLOBAL;
            mHookedUp = new JSArray(GLOBAL);
            mThisRef = thisRef;
            mEventInfo = eventInfo;
            mCallDelegate = Delegate.CreateDelegate(typeof(TriggerEventDelegate), GetType().GetMethod("TriggerEvent"), false);
            if (mCallDelegate != null)
                eventInfo.AddEventHandler(mThisRef, mCallDelegate);
        }
    }

    internal class JSNativeField : JSProperty
    {
        object mThisRef;
        FieldInfo mFieldInfo;
        public string Name
        {
            get
            {
                return mFieldInfo.Name;
            }
        }
        bool mReadOnly;
        public bool ReadOnly { get { return mReadOnly || mFieldInfo.IsInitOnly; } }
        bool mDontEnum;
        public bool DontEnum { get { return mDontEnum; } }
        public bool DontDelete { get { return true; } }
        public bool Internal { get { return true; } }
        public object GetValue(ExecutionContext GLOBAL)
        {
            return JSObject.ToJS(GLOBAL, mFieldInfo.GetValue(mThisRef));
        }
        public void SetValue(ExecutionContext GLOBAL, object value)
        {
            object obval;
            if (JSObject.ConvertToType(GLOBAL, value, mFieldInfo.GetType(), out obval))
                mFieldInfo.SetValue(mThisRef, obval);
            else
                throw new TypeError(mFieldInfo.Name + " field of " + mFieldInfo.DeclaringType.Name + " can't convert from javascript object " + JSObject.Typeof(value) + " to type " + mFieldInfo.GetType().Name);
        }
        public JSNativeField(object thisRef, FieldInfo fieldInfo)
        {
            mThisRef = thisRef;
            mFieldInfo = fieldInfo;
            object[] attributes = mFieldInfo.GetCustomAttributes(true);
            mDontEnum = false;
            mReadOnly = false;
            foreach (object attribute in attributes)
            {
                if (attribute is BrowsableAttribute)
                    mDontEnum = !((BrowsableAttribute)attribute).Browsable;
                if (attribute is ReadOnlyAttribute)
                    mReadOnly = !((ReadOnlyAttribute)attribute).IsReadOnly;
            }
        }
    }

    internal class JSNativeMethod : JSProperty
    {
        public class JSMethodCall : JSObject
        {
            JSNativeMethod mMethodProp;
            bool isThisArg(MethodInfo mi)
            {
                object[] attributes = mi.GetCustomAttributes(typeof(ThisArgAttribute), true);
                return attributes.Length > 0;
            }
            public override string Class { get { return "function"; } }
            public override string ToString() { return "function () { [native code] }"; }
            public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
            {
                JSObjectBase thisArgs = a;
                if (mMethodProp.mThisArg)
                {
                    int i, alen = (int)JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "length").GetValue(GLOBAL));
                    JSObject newArgs = new JSObject();
                    newArgs.DefProp(GLOBAL, "length", (double)(alen + 1));
                    for (i = alen; i > 0; i--)
                        newArgs.SetItem(GLOBAL, i.ToString(), a.GetItem(GLOBAL, (i - 1).ToString()));
                    newArgs.DefProp(GLOBAL, "0", t);
                    thisArgs = newArgs;
                }
                foreach (MethodInfo mi in mMethodProp.mMethodInfo)
                {
                    object[] nargs = SatisfyArgumentList(GLOBAL, isThisArg(mi) ? thisArgs : a, mi.GetParameters());
                    if (nargs != null)
                        return ToJS(GLOBAL, mi.Invoke(mi.IsStatic ? null : ToNative(t), nargs));
                }
                throw new TypeError("Couldn't match arguments for " + mMethodProp.mOverallName);
            }
            public JSMethodCall(JSNativeMethod method) { mMethodProp = method; }

            public static object ToNative(object t)
            {
                if (t is JSInstanceWrapper)
                    return ((JSInstanceWrapper)t).This;
                else if (t is JSClassWrapper)
                    return ((JSClassWrapper)t).ThisType;
                else return t;
            }
        }

        class MethodSigComparer : IComparer<MethodInfo>
        {
            public int Compare(MethodInfo a, MethodInfo b)
            {
                int i;
                ParameterInfo[] aParams = a.GetParameters();
                ParameterInfo[] bParams = b.GetParameters();

                /* Longer signatures match first */
                if (aParams.Length > bParams.Length)
                    return -1;
                else if (aParams.Length < bParams.Length)
                    return 1;

                /* array sorts first, double, decimal and float, int etc, others */
                for (i = 0; i < aParams.Length; i++)
                {
                    if (aParams[i].ParameterType.IsArray && !bParams[i].ParameterType.IsArray)
                        return -1;
                    else if (!aParams[i].ParameterType.IsArray && bParams[i].ParameterType.IsArray)
                        return 1;
                    if (JSObject.IsParamFloating(aParams[i].ParameterType) && !JSObject.IsParamFloating(bParams[i].ParameterType))
                        return -1;
                    else if (!JSObject.IsParamFloating(aParams[i].ParameterType) && JSObject.IsParamFloating(bParams[i].ParameterType))
                        return 1;
                    if (JSObject.IsParamIntegral(aParams[i].ParameterType) && !JSObject.IsParamIntegral(bParams[i].ParameterType))
                        return -1;
                    else if (!JSObject.IsParamIntegral(aParams[i].ParameterType) && JSObject.IsParamIntegral(bParams[i].ParameterType))
                        return 1;
                }

                return 0;
            }
        }

        string mOverallName;
        bool mThisArg;
        List<MethodInfo> mMethodInfo;
        object mMethodCall;
        public string Name
        {
            get
            {
                return mOverallName;
            }
        }
        bool mReadOnly;
        public bool ReadOnly { get { return mReadOnly; } }
        bool mDontEnum;
        public bool DontEnum { get { return mDontEnum; } }
        public bool DontDelete { get { return true; } }
        public bool Internal { get { return true; } }
        public object GetValue(ExecutionContext GLOBAL)
        {
            if (mMethodCall == null)
                mMethodCall = new JSMethodCall(this);
            return mMethodCall;
        }
        public void SetValue(ExecutionContext GLOBAL, object value)
        {
            mMethodCall = value;
        }

        public void Construct(MethodInfo[] methodInfo)
        {
            mMethodInfo = new List<MethodInfo>(methodInfo);
            mMethodInfo.Sort(new MethodSigComparer());
            mOverallName = mMethodInfo[0].Name;
            object[] attributes = mMethodInfo[0].GetCustomAttributes(true);
            mDontEnum = false;
            foreach (object attribute in attributes)
            {
                if (attribute is ThisArgAttribute)
                    mThisArg = true;
                if (attribute is BrowsableAttribute)
                    mDontEnum = !((BrowsableAttribute)attribute).Browsable;
                if (attribute is ReadOnlyAttribute)
                    mReadOnly = ((ReadOnlyAttribute)attribute).IsReadOnly;
            }
        }

        public JSNativeMethod(MethodInfo[] methodInfo)
        {
            Construct(methodInfo);
        }

        public JSNativeMethod(Type t, string name)
        {
            List<MethodInfo> methods = new List<MethodInfo>();
            foreach (MethodInfo method in t.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                if (method.Name == name)
                    methods.Add(method);
            Construct(methods.ToArray());
        }
    }
    public class Reference : Object
    {
        public string mPropertyName;
        public JSObjectBase mBase;
        public JSObjectBase GetBase()
        {
            return mBase;
        }

        public string GetPropertyName()
        {
            return mPropertyName;
        }

        public string Class { get { return "Reference_YouShouldNeverSeeThis"; } }
        public JSProperty this[string idx] { get { return null; } set { } }
        public IEnumerable<string> Properties { get { foreach (string s in new string[] { }) { yield return s; } } }
        public bool CanPut(string ident) { return false; }
        public bool HasProperty(string ident) { return false; }
        public bool HasOwnProperty(string ident) { return false; }
        public bool Delete(string ident) { return false; }
        public object DefaultValue(string hint) { throw new TypeError("not a value ... ever (was for field " + mPropertyName + ")"); }
        public object Call(object t, JSObjectBase a, ExecutionContext x)
        {
            throw new TypeError("Reference is not a function");
        }
        public object Construct(object a, ExecutionContext x)
        {
            throw new TypeError("Reference is not a constructor");
        }
        public bool HasInstance(object child) { return false; }
        public object Match(string str, int idx) { throw new TypeError("Reference is not a regex"); }

        public static object GetValue(ExecutionContext GLOBAL, object reference)
        {
            Reference refer = reference as Reference;
            if (refer == null) return reference;
            JSObjectBase baseOb = refer.GetBase();
            if (baseOb == null) baseOb = GLOBAL.jobject;
            JSProperty prop = baseOb.GetItem(GLOBAL, refer.GetPropertyName());
            if (prop == null && baseOb == GLOBAL.jobject)
                throw new ReferenceError(baseOb.ToString() + " of type " + baseOb.Class + " doesn't have a " + (refer != null ? refer.GetPropertyName() : reference.ToString()) + " property.");
            else if (prop == null)
                return JSUndefined.Undefined;
            else
                return prop.GetValue(GLOBAL);
        }

        public static void PutValue(ExecutionContext GLOBAL, object reference, object val)
        {
            Reference refer = reference as Reference;
            if (refer == null) throw new ReferenceError(reference.ToString());
            JSObjectBase baseOb = refer.GetBase();
            if (baseOb == null) baseOb = GLOBAL.jobject;
            JSProperty prop = baseOb.GetItem(GLOBAL, refer.GetPropertyName());
            if (prop == null)
                baseOb.SetItem(GLOBAL, refer.GetPropertyName(), new JSSimpleProperty(refer.GetPropertyName(), GetValue(GLOBAL, val)));
            else if (!prop.ReadOnly) prop.SetValue(GLOBAL, GetValue(GLOBAL, val));
        }

        public override string ToString()
        {
            return "{ base: " + (mBase == null ? "(null)" : mBase.ToString()) + ", propName: " + GetPropertyName() + " }";
        }

        public Reference(JSObjectBase baseOb, string propertyName)
        {
            mPropertyName = propertyName;
            mBase = baseOb;
        }
    }
}
