using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace pygmalion
{
    public class JSClassWrapper : JSObject
    {
        Type mThisType;
        JSNativeMethod mApplyMethod;

        public Type ThisType { get { return mThisType; } }
        public override string Class
        {
            get
            {
                return mThisType.GetType().Name;
            }
        }
        public override bool HasInstance(ExecutionContext GLOBAL, object ident)
        {
            JSInstanceWrapper iw = ident as JSInstanceWrapper;
            if (iw == null) return false;
            return mThisType.IsAssignableFrom(iw.This.GetType());
        }
        public override object Construct(ExecutionContext GLOBAL, JSObjectBase args, ExecutionContext x)
        {
            object[] arglist = null;
            foreach (ConstructorInfo c in mThisType.GetConstructors())
            {
                arglist = JSObject.SatisfyArgumentList(GLOBAL, args, c.GetParameters());
                if (arglist != null)
                {
                    return JSObject.ToJS(GLOBAL, c.Invoke(arglist));
                }
            }
            throw new TypeError("Wrong argument types for " + mThisType.Name + " constructor");
        }
        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            if (mApplyMethod == null) throw new TypeError(Class + " is not callable");
            return ((JSObjectBase)mApplyMethod.GetValue(GLOBAL)).Call(GLOBAL, t, a, x);
        }

        public JSClassWrapper(ExecutionContext GLOBAL, Type t)
        {
            object thisOb = null;
            mThisType = t;
            BindingFlags flags = BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public;
            string applyMethod = null;
            object []attributes = t.GetCustomAttributes(false);
            List<MethodInfo> applying = new List<MethodInfo>();

            foreach (object attribute in attributes)
            {
                if (attribute is ApplyAttribute)
                    applyMethod = ((ApplyAttribute)attribute).MethodName;
            }

            if (t == typeof(Type))
            {
                thisOb = t;
                flags |= BindingFlags.Instance;
            }
            Dictionary<string, List<MethodInfo>> namedMethods = new Dictionary<string, List<MethodInfo>>();

            foreach (FieldInfo fieldInfo in mThisType.GetFields(flags))
            {
                this.SetItem(GLOBAL, fieldInfo.Name, new JSNativeField(thisOb, fieldInfo));
            }
            foreach (PropertyInfo propInfo in mThisType.GetProperties(flags))
            {
                this.SetItem(GLOBAL, propInfo.Name, new JSNativeProperty(thisOb, propInfo));
            }
            foreach (MethodInfo methodInfo in mThisType.GetMethods(flags))
            {
                List<MethodInfo> ml;
                if (applyMethod != null && methodInfo.Name == applyMethod && methodInfo.IsStatic)
                    applying.Add(methodInfo);
                if (!namedMethods.TryGetValue(methodInfo.Name, out ml))
                {
                    ml = new List<MethodInfo>(new MethodInfo[] { methodInfo });
                    namedMethods[methodInfo.Name] = ml;
                }
                else
                    ml.Add(methodInfo);
            }

            foreach (EventInfo eventInfo in mThisType.GetEvents(flags))
            {
                this.SetItem(GLOBAL, eventInfo.Name, new JSNativeEvent(GLOBAL, thisOb, eventInfo));
            }
            foreach (KeyValuePair<string, List<MethodInfo>> method in namedMethods)
            {
                this.SetItem(GLOBAL, method.Key, new JSNativeMethod(method.Value.ToArray()));
            }
            if (applying.Count > 0)
            {
                mApplyMethod = new JSNativeMethod(applying.ToArray());
            }
        }
        [GlobalArg]
        public static void Register(ExecutionContext GLOBAL, JSObjectBase scope, Type t)
        {
            string[] fullName = t.FullName.Split(new char[] { '.' });
            JSClassWrapper wrapper = new JSClassWrapper(GLOBAL, t);
            RegisterNamespace(GLOBAL, scope, fullName, fullName.Length - 2);
        }
        public static JSObjectBase RegisterNamespace(ExecutionContext GLOBAL, JSObjectBase scope, string[] names, int idx)
        {
            JSObjectBase parent = idx == 0 ? scope : RegisterNamespace(GLOBAL, scope, names, idx - 1);
            if (!parent.HasProperty(GLOBAL, names[idx]))
            {
                JSObject wrapper = new JSObject();
                parent.GetItem(GLOBAL, names[idx]).SetValue(GLOBAL, wrapper);
                return wrapper;
            }
            return (JSObjectBase)parent.GetItem(GLOBAL, names[idx]).GetValue(GLOBAL);
        }

        static Dictionary<Type, JSClassWrapper> mRegisteredTypes = new Dictionary<Type, JSClassWrapper>();
        public static JSClassWrapper RegisterClass(ExecutionContext GLOBAL, Type t)
        {
            JSClassWrapper wrapper;
            if (mRegisteredTypes.TryGetValue(t, out wrapper))
                return wrapper;
            else
            {
                wrapper = new JSClassWrapper(GLOBAL, t);
                mRegisteredTypes[t] = wrapper;
                return wrapper;
            }
        }
    }

    public class JSInstanceWrapper : JSObject
    {
        object mThisRef;
        JSClassWrapper mClassWrapper;

        public object This { get { return mThisRef; } }
        public override string Class
        {
            get
            {
                return This == null ? "null" : This.GetType().Name;
            }
        }
        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            Delegate d = mThisRef as Delegate;
            MethodInfo m = d.Method;

            if (d == null) throw new TypeError(Class + " isn't callable");

            object[] arglist = JSObject.SatisfyArgumentList(GLOBAL, a, m.GetParameters());
            if (arglist != null)
                return JSObject.ToJS(GLOBAL, d.DynamicInvoke(arglist));
            else
                throw new TypeError("Wrong arguments for method " + m.DeclaringType.Name + "." + m.Name);
        }
        public override bool HasInstance(ExecutionContext GLOBAL, object ident)
        {
            return false;
        }
        public override object Match(string str, int idx)
        {
            if (mThisRef is Regex)
                return ((Regex)mThisRef).Match(str, idx).Value;
            else throw new TypeError(Class + " is not a regex");
        }
        public JSInstanceWrapper(ExecutionContext GLOBAL, object thisRef)
        {
            mThisRef = thisRef;
            mClassWrapper = JSClassWrapper.RegisterClass(GLOBAL, mThisRef.GetType());
            Dictionary<string, List<MethodInfo>> namedMethods = new Dictionary<string, List<MethodInfo>>();
            Type myType = mThisRef.GetType();
            foreach (FieldInfo fieldInfo in myType.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public))
            {
                this.SetItem(GLOBAL, fieldInfo.Name, new JSNativeField(mThisRef, fieldInfo));
            }
            foreach (PropertyInfo propInfo in myType.GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public))
            {
                this.SetItem(GLOBAL, propInfo.Name, new JSNativeProperty(mThisRef, propInfo));
            }
            foreach (MethodInfo methodInfo in myType.GetMethods(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public))
            {
                List<MethodInfo> ml;
                if (!namedMethods.TryGetValue(methodInfo.Name, out ml))
                {
                    ml = new List<MethodInfo>(new MethodInfo[] { methodInfo });
                    namedMethods[methodInfo.Name] = ml;
                }
            }
            foreach (EventInfo eventInfo in myType.GetEvents(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public))
            {
                this.SetItem(GLOBAL, eventInfo.Name, new JSNativeEvent(GLOBAL, mThisRef, eventInfo));
            }

            DefProp(GLOBAL, "prototype", JSClassWrapper.RegisterClass(GLOBAL, thisRef.GetType()));

            foreach (KeyValuePair<string, List<MethodInfo>> method in namedMethods)
            {
                this.SetItem(GLOBAL, method.Key, new JSNativeMethod(method.Value.ToArray()));
            }
        }
    }
}
