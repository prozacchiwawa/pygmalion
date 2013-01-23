/* Pygmalion -- Javascript in a small amount of C# */
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.ComponentModel;

namespace pygmalion
{
    public class GlobalArgAttribute : Attribute { }
    public class ThisArgAttribute : Attribute { }
    public class ApplyAttribute : Attribute 
    {
        string mMethodName;
        public string MethodName { get { return mMethodName; } set { mMethodName = value; } }
        public ApplyAttribute(string name) { mMethodName = name; }
    }
    public class LengthAttribute : Attribute
    {
        int mLength; 
        public int Length { get { return mLength; } set { mLength = value; } }
        public LengthAttribute(int len) { mLength = len; }
    }

    public interface JSObjectBase
    {
        string Class { get; }
        JSProperty GetItem(ExecutionContext GLOBAL, string name);
        void SetItem(ExecutionContext GLOBAL, string name, JSProperty value);
        IEnumerable<string> Properties { get; }
        bool CanPut(ExecutionContext GLOBAL, string name);
        bool HasProperty(ExecutionContext GLOBAL, string name);
        bool HasOwnProperty(string name);
        bool Delete(string name);
        object DefaultValue(ExecutionContext GLOBAL, string hint);
        object Construct(ExecutionContext GLOBAL, JSObjectBase args, ExecutionContext x);
        object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x);
        bool HasInstance(ExecutionContext GLOBAL, object ident);
        object Match(string str, int idx);
    }

    public class JSUndefined : Object
    {
        public static object Undefined = new JSUndefined();
        public string Class { get { return "undefined"; } }
        public JSProperty this[string name]
        {
            get
            {
                throw new TypeError("undefined does not have properties");
            }
            set
            {
                throw new TypeError("undefined does not have properties");
            }
        }
        public IEnumerable<string> Properties { get { foreach (string x in new string[] { }) yield return x; } }
        public bool CanPut(string name) { return false; }
        public bool HasProperty(string name) { return false; }
        public bool HasOwnProperty(string name) { return false; }
        public bool Delete(string name) { return false; }
        public object DefaultValue(ExecutionContext GLOBAL, string hint) { return Undefined; }
        public object Construct(ExecutionContext GLOBAL, object args, ExecutionContext x) { throw new TypeError("undefined is not a constructor"); }
        public object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x) { throw new TypeError("undefined is not a function"); }
        public bool HasInstance(object ident) { throw new TypeError("undefined has no fields"); }
        public object Match(string str, int idx) { throw new TypeError("the undefined value is not a valid regex"); }
        public override string ToString()
        {
            return "";
        }
    }

    public class JSObject : JSObjectBase
    {
        public IEnumerable<string> Properties
        {
            get
            {
                foreach (KeyValuePair<string, JSProperty> kv in mProperties)
                    if (!kv.Value.DontEnum)
                        yield return kv.Key;
            }
        }
        public virtual string Class { get { return "Object"; } }
        public virtual bool CanPut(ExecutionContext GLOBAL, string name)
        {
            JSProperty prop, prot;
            if (mProperties.TryGetValue(name, out prop))
                return !prop.ReadOnly;
            if (mProperties.TryGetValue("prototype", out prot))
            {
                if (prot.GetValue(GLOBAL) == null || !(prop.GetValue(GLOBAL) is JSObjectBase)) return true;
                return ((JSObjectBase)prot.GetValue(GLOBAL)).CanPut(GLOBAL, name);
            }
            else return true;
        }
        public virtual bool HasProperty(ExecutionContext GLOBAL, string name)
        {
            if (mProperties.ContainsKey(name)) return true;
            else
            {
                JSProperty prot;
                if (!mProperties.TryGetValue("prototype", out prot)) return false;
                if (prot.GetValue(GLOBAL) == null || !(prot.GetValue(GLOBAL) is JSObjectBase)) return false;
                else return ((JSObjectBase)prot.GetValue(GLOBAL)).HasProperty(GLOBAL, name);
            }
        }
        public virtual bool HasOwnProperty(string name)
        {
            return mProperties.ContainsKey(name);
        }
        public virtual bool Delete(string name)
        {
            JSProperty prop;
            if (mProperties.TryGetValue(name, out prop))
            {
                if (prop.DontDelete) return false;
                mProperties.Remove(name);
                return true;
            }
            else return false;
        }
        public virtual object DefaultValue(ExecutionContext GLOBAL, string hint)
        {
            string checkFirst, checkSecond;

            if (hint == "string")
            {
                checkFirst = "toString";
                checkSecond = "valueOf";
            }
            else
            {
                checkFirst = "valueOf";
                checkSecond = "toString";
            }

            JSProperty toStringMethod = this.GetItem(GLOBAL, checkFirst);
            if (toStringMethod != null)
                try
                {
                    return ((JSObjectBase)toStringMethod.GetValue(GLOBAL)).Call(GLOBAL, this, new JSArray(GLOBAL), GLOBAL.currentContext);
                }
            catch (Exception) { }

            JSProperty valueOfMethod = this.GetItem(GLOBAL, checkSecond);
            if (valueOfMethod != null)
                try
                {
                    return ((JSObjectBase)valueOfMethod.GetValue(GLOBAL)).Call(GLOBAL, this, new JSArray(GLOBAL), GLOBAL.currentContext);
                }
                catch (Exception ex)
                {
                    throw new TypeError("Can't convert to primitive type: " + ex.Message);
                }

            return ToString();
        }
        Dictionary<string, JSProperty> mProperties = new Dictionary<string, JSProperty>();

        public virtual JSProperty GetItem(ExecutionContext GLOBAL, string ident)
        {
            JSProperty result;
            if (mProperties.TryGetValue(ident, out result))
                return result;
            else
            {
                JSProperty prop;
                if (mProperties.TryGetValue("prototype", out prop))
                {
                    if (prop != null && prop.GetValue(GLOBAL) is JSObjectBase)
                        return ((JSObjectBase)prop.GetValue(GLOBAL)).GetItem(GLOBAL, ident);
                    else return null;
                }
                else return null;
            }
        }
        public virtual void SetItem(ExecutionContext GLOBAL, string ident, JSProperty value)
        {
            mProperties[ident] = value;
        }

        public virtual object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            throw new TypeError(Class + " is not a function");
        }

        public virtual object Construct(ExecutionContext GLOBAL, JSObjectBase args, ExecutionContext x)
        {
            throw new TypeError(Class + " is not a constructor");
        }

        public virtual bool HasInstance(ExecutionContext GLOBAL, object ob)
        {
            if (ob == null || ob is JSUndefined || ob is bool || ob is double || ob is string)
                return false;
            object o = this.GetItem(GLOBAL, "prototype").GetValue(GLOBAL);
            if (o == null || o is JSUndefined || o is bool || o is double || o is string)
                throw new TypeError(Class + " is not a progenitor in this case");
            do
            {
                ob = JSObject.GetPrototype(GLOBAL, ob);
                if (o == ob) return true;
            }
            while (ob != null);
            return false;
        }

        public virtual object Match(string str, int idx)
        {
            throw new TypeError(Class + " is not a regex");
        }

        [GlobalArg]
        public string ToString(ExecutionContext GLOBAL)
        {
            JSProperty tostr;
            if (!mProperties.TryGetValue("toString", out tostr) ||
                GetType().GetMethod("ToString").DeclaringType == typeof(JSObject))
                return "[object " + Class + "]";
            else
                return ((JSObjectBase)tostr.GetValue(GLOBAL)).Call(GLOBAL, this, new JSArray(GLOBAL), GLOBAL.currentContext).ToString();
        }

        [ThisArg, Browsable(false)]
        public static string StaticToString(object thisOb)
        {
            return "[object " + JSObject.Typeof(thisOb) + "]";
        }

        public void DefProp(ExecutionContext GLOBAL, string name, object defaultValue)
        {
            this.SetItem(GLOBAL, name, new JSSimpleProperty(name, defaultValue));
        }
        public void DefProp(ExecutionContext GLOBAL, string name, object defaultValue, bool enumerable)
        {
            this.SetItem(GLOBAL, name, new JSSimpleProperty(name, defaultValue, enumerable));
        }
        public void DefProp(ExecutionContext GLOBAL, string name, object defaultValue, bool enumerable, bool removable)
        {
            this.SetItem(GLOBAL, name, new JSSimpleProperty(name, defaultValue, enumerable, removable));
        }
        public void DefProp(ExecutionContext GLOBAL, string name, object defaultValue, bool enumerable, bool removable, bool writable)
        {
            this.SetItem(GLOBAL, name, new JSSimpleProperty(name, defaultValue, enumerable, removable, writable));
        }
        public void DefProp(ExecutionContext GLOBAL, string name, object defaultValue, bool enumerable, bool removable, bool writable, bool internl)
        {
            this.SetItem(GLOBAL, name, new JSSimpleProperty(name, defaultValue, enumerable, removable, writable, internl));
        }

        public static int CompareTo(ExecutionContext GLOBAL, object me, object other)
        {
            // null == undefined and vice versa, but nothing else (not even <=)
            if ((me is JSUndefined || other is JSUndefined) ||
                (me == null || other == null))
                throw new FalseReturn();
            if (me is bool)
                return (bool)me ? -1 : 0 + (JSObject.ToBool(GLOBAL, other) ? 1 : 0);
            else if (me is double)
            {
                double a = (double)me;
                double b = JSObject.ToNumber(GLOBAL, other);
                return a < b ? -1 : a > b ? 1 : 0;
            }
            else return me.ToString().CompareTo(other.ToString());
        }

        public static object ToJS(ExecutionContext GLOBAL, object val)
        {
            if (val == null)
                return null;
            else if (val is bool)
                return (bool)val;
            else if (val is byte)
                return (double)(byte)val;
            else if (val is short)
                return (double)(short)val;
            else if (val is ushort)
                return (double)(ushort)val;
            else if (val is int)
                return (double)(int)val;
            else if (val is uint)
                return (double)(uint)val;
            else if (val is long)
                return (double)(long)val;
            else if (val is ulong)
                return (double)(ulong)val;
            else if (val is float)
                return (double)(float)val;
            else if (val is double)
                return (double)val;
            else if (val is decimal)
                return (double)(decimal)val;
            else if (val is char)
                return "" + (char)val;
            else if (val is string)
                return (string)val;
            else if (val is Array)
                return new JSArray(GLOBAL, (Array)val);
            else if (val is DateTime)
                return new JSDate(GLOBAL, (DateTime)val);
            else if (val is JSObjectBase)
                return val;
            else if (val is Type)
                return new JSClassWrapper(GLOBAL, (Type)val);
            else
                return new JSInstanceWrapper(GLOBAL, val);
        }

        static bool IsParamsArray(ParameterInfo pi)
        {
            object[] attributes = pi.GetCustomAttributes(false);

            foreach (object attribute in attributes)
            {
                if (attribute is ParamArrayAttribute)
                    return true;
            }

            return false;
        }

        public static bool IsParamFloating(Type t)
        {
            return
                t == typeof(decimal) ||
                t == typeof(double) ||
                t == typeof(float);
        }

        public static bool IsParamIntegral(Type t)
        {
            return
                t == typeof(byte) ||
                t == typeof(short) ||
                t == typeof(ushort) ||
                t == typeof(int) ||
                t == typeof(uint) ||
                t == typeof(long) ||
                t == typeof(ulong);
        }

        //
        // Basic matches
        //
        // The following requires double, float and decimal to be sorted before
        // integral types.  We'll generally prefer using double, float or decimal
        // versions of a function to integral versions, due to javascript's
        // preference of double for numerics
        //
        // ParamsArrayAttribute <- stop loop, try to convert all remaining arguments
        // IsOptional <- undefined or null -> RawDefaultValue
        // IsOut or IsRef <- (Nullable?)
        // float ... double and decimal <- number
        // byte ... ulong <- number
        // char <- jsstring, length = 1
        // string <- jsstring
        // Array <- jsarray (try to convert elements)
        // any kind of object <- IsAssignableFrom
        // bool <- almost anything
        // 
        public static bool ConvertToType(ExecutionContext GLOBAL, object arg, Type argtype, out object result)
        {
            result = null;

            // Can't match: nonnumber into integral slot
            if (IsParamFloating(argtype))
            {
                if (!(arg is double)) return false;
                if (argtype == typeof(double))
                {
                    result = (double)arg;
                    return true;
                }
                else if (argtype == typeof(float))
                {
                    result = (float)(double)arg;
                    return true;
                }
                else if (argtype == typeof(decimal))
                {
                    result = (decimal)(double)arg;
                    return true;
                }
                else return false;
            }

            if (IsParamIntegral(argtype))
            {
                if (argtype == typeof(byte))
                {
                    result = (byte)(double)arg;
                    return true;
                }
                else if (argtype == typeof(short))
                {
                    result = (short)(double)arg;
                    return true;
                }
                else if (argtype == typeof(ushort))
                {
                    result = (ushort)(double)arg;
                    return true;
                }
                else if (argtype == typeof(int))
                {
                    result = (int)(double)arg;
                    return true;
                }
                else if (argtype == typeof(uint))
                {
                    result = (uint)(double)arg;
                    return true;
                }
                else if (argtype == typeof(long))
                {
                    result = (long)(double)arg;
                    return true;
                }
                else if (argtype == typeof(ulong))
                {
                    result = (ulong)(double)arg;
                    return true;
                }
                else return false;
            }

            if (argtype == typeof(char))
            {
                string str = arg as string;
                if (arg is double)
                {
                    result = (char)(double)arg;
                    return true;
                }
                else if (str != null && str.Length == 1)
                {
                    result = str[0];
                    return true;
                }
                else return false;
            }

            if (argtype == typeof(string))
            {
                result = arg.ToString();
                return true;
            }

            if (argtype.IsArray)
            {
                JSArray ja = arg as JSArray;
                if (ja == null) return false;
                Type eltType = argtype.GetElementType();
                System.Collections.ArrayList lo = new System.Collections.ArrayList();
                foreach (object aob in ja)
                {
                    object obelt;
                    if (!ConvertToType(GLOBAL, aob, eltType, out obelt))
                        return false;
                    lo.Add(obelt);
                }
                result = lo.ToArray(eltType);
                return true;
            }

            if (argtype == typeof(DateTime))
            {
                if (arg is JSDate)
                {
                    result = ((JSDate)arg).Value;
                    return true;
                }
                else if (arg is DateTime)
                {
                    result = arg;
                    return true;
                }
                else if (arg is long)
                {
                    JSDate jd = new JSDate(GLOBAL);
                    JSDate.setTime(jd, (long)arg);
                    result = jd.Value;
                    return true;
                }
                else if (arg is double)
                {
                    JSDate jd = new JSDate(GLOBAL);
                    JSDate.setTime(jd, (long)((double)arg));
                    result = jd.Value;
                    return true;
                }
                else if (arg is decimal)
                {
                    JSDate jd = new JSDate(GLOBAL);
                    JSDate.setTime(jd, (long)((decimal)arg));
                    result = jd.Value;
                    return true;
                }
                else if (arg is JSInstanceWrapper)
                {
                    JSInstanceWrapper iw = (JSInstanceWrapper)arg;
                    if (iw.This is DateTime)
                    {
                        result = iw.This;
                        return true;
                    }
                }
            }

            if (argtype == typeof(JSDate))
            {
                if (arg is JSDate)
                {
                    result = arg;
                    return true;
                }
                else if (arg is DateTime)
                {
                    result = new JSDate(GLOBAL, (DateTime)arg);
                    return true;
                }
                else if (arg is long)
                {
                    JSDate jd = new JSDate(GLOBAL);
                    JSDate.setTime(jd, (long)arg);
                    result = jd;
                    return true;
                }
                else if (arg is double)
                {
                    JSDate jd = new JSDate(GLOBAL);
                    JSDate.setTime(jd, (long)((double)arg));
                    result = jd;
                    return true;
                }
                else if (arg is decimal)
                {
                    JSDate jd = new JSDate(GLOBAL);
                    JSDate.setTime(jd, (long)((decimal)arg));
                    result = jd;
                    return true;
                }
                else if (arg is JSInstanceWrapper)
                {
                    JSInstanceWrapper iw = (JSInstanceWrapper)arg;
                    if (iw.This is DateTime)
                    {
                        result = new JSDate(GLOBAL, (DateTime)iw.This);
                        return true;
                    }
                }
            }

            if (arg is JSInstanceWrapper)
            {
                JSInstanceWrapper wrapper = (JSInstanceWrapper)arg;
                if (!argtype.IsAssignableFrom(wrapper.This.GetType()))
                    return false;
                result = wrapper.This;
                return true;
            }

            if (argtype == typeof(bool))
            {
                string str = arg as string;
                if (arg is bool) result = (bool)arg;
                if (arg is double) result = (double)arg != 0.0;
                if (str != null) result = str.ToString().Length != 0;
                result = !(arg == null || arg == JSUndefined.Undefined);
                return true;
            }

            return false;
        }

        [ThisArg, Browsable(false), GlobalArg]
        public static string ToPrimitive(ExecutionContext GLOBAL, object thisOb)
        {
            if (thisOb is Reference)
                thisOb = Reference.GetValue(GLOBAL, thisOb);
            else if (thisOb == null)
                return "";
            if (thisOb is bool)
                return ((bool)thisOb) ? "true" : "false";
            else if (thisOb is double)
                return ((double)thisOb).ToString();
            else if (thisOb is string)
                return (string)thisOb;
            else return thisOb.ToString();
        }

        [ThisArg, Browsable(false), GlobalArg]
        public static object ToPrimitive(ExecutionContext GLOBAL, object ob, string preferredType)
        {
            if (ob == null ||
                ob is JSUndefined ||
                ob is bool ||
                ob is double ||
                ob is decimal ||
                ob is string)
                return ob;
            else if (ob is JSObjectBase)
                return ToPrimitive(GLOBAL, ((JSObjectBase)ob).DefaultValue(GLOBAL, preferredType));
            else
                return ToPrimitive(GLOBAL, ob);
        }

        [GlobalArg]
        public static bool ToBool(ExecutionContext GLOBAL, object ob)
        {
            while (ob is Reference) { ob = Reference.GetValue(GLOBAL, ob); }
            if (ob == null) return false;
            else if (ob is JSUndefined) return false;
            if (ob is bool) return (bool)ob;
            if (ob is double)
            {
                double val = (double)ob;
                return val != 0.0f && !double.IsNaN(val);
            }
            string stringVal = ob as string;
            return !(stringVal != null && stringVal.ToString() == "");
        }

        [GlobalArg]
        public static double ToNumber(ExecutionContext GLOBAL, object ob)
        {
            if (ob is JSUndefined) return double.NaN;
            else if (ob == null) return 0.0;
            else if (ob is bool) return ((bool)ob) ? 1.0 : 0.0;
            else if (ob is decimal) return (double)((decimal)ob);
            else if (ob is double) return (double)ob;
            else if (ob is DateTime) return (double)((DateTime)ob).ToBinary();
            string stringVal = ob as string;
            if (stringVal != null)
            {
                if (stringVal.Length == 0) return double.NaN;
                double sign = stringVal[0] == '-' ? -1 : 1;
                if (sign < 0)
                    stringVal = stringVal.Substring(1);
                stringVal = stringVal.Trim(new char[] { '\t', ' ', '\u00a0', '\u000c', '\u000b', '\u000d', '\u000a', '\u2028', '\u2029' });
                if (stringVal.ToLower().StartsWith("0x"))
                    try { return sign * (double)long.Parse(stringVal.Substring(2), NumberStyles.HexNumber); }
                    catch (FormatException) { return double.NaN; }
                    catch (OverflowException) { return double.NaN; }
                else
                    try { return sign * double.Parse(stringVal); }
                    catch (FormatException) { return double.NaN; }
                    catch (OverflowException) { return double.NaN; }
            }
            else
                try { return ToNumber(GLOBAL, ToPrimitive(GLOBAL, ob, "number")); }
                catch (FormatException) { return 0.0; }
                catch (OverflowException) { return 0.0; }
        }

        public static bool ConvertArgumentToType(ExecutionContext GLOBAL, object arg, ParameterInfo argtype, out object result)
        {
            result = null;

            if (argtype.IsOptional && arg == JSUndefined.Undefined)
            {
                result = argtype.RawDefaultValue;
                return true;
            }

            if (argtype.ParameterType.IsAssignableFrom(arg.GetType()))
            {
                result = arg;
                return true;
            }

            // Handle out params somehow 
            return ConvertToType(GLOBAL, arg, argtype.ParameterType, out result);
        }

        public static object[] SatisfyArgumentList(ExecutionContext GLOBAL, JSObjectBase args, ParameterInfo[] argtypes)
        {
            int i = 0;
            object resob;
            List<object> result = new List<object>();

            // Special case for GLOBAL
            if (argtypes.Length > 0 && argtypes[0].ParameterType == typeof(ExecutionContext))
            {
                result.Add(GLOBAL);
                i = 1;
            }
            for (; i < JSObject.ToNumber(GLOBAL, args.GetItem(GLOBAL, "length").GetValue(GLOBAL)) && i < argtypes.Length; i++)
            {
                // Do array match on the rest
                if (IsParamsArray(argtypes[i]))
                    break;

                if (ConvertArgumentToType(GLOBAL, args.GetItem(GLOBAL, i.ToString()).GetValue(GLOBAL), argtypes[i], out resob))
                    result.Add(resob);
                else return null;
            }

            if (i < argtypes.Length)
            {
                if (!IsParamsArray(argtypes[i]))
                    return null;
                else
                {
                    // Empty varargs ...
                    System.Collections.ArrayList empty = new System.Collections.ArrayList();
                    result.Add(empty.ToArray(argtypes[i].ParameterType.GetElementType()));
                    return result.ToArray();
                }
            }

            if (i < argtypes.Length)
            {
                Type eltType = argtypes[i].ParameterType.GetElementType();
                System.Collections.ArrayList paramset = new System.Collections.ArrayList();
                for (; i < JSObject.ToNumber(GLOBAL, args.GetItem(GLOBAL, "length").GetValue(GLOBAL)); i++)
                {
                    if (ConvertToType(GLOBAL, args.GetItem(GLOBAL, i.ToString()).GetValue(GLOBAL), eltType, out resob))
                        paramset.Add(resob);
                    else return null;
                }
                result.Add(paramset.ToArray(eltType));
            }

            return result.ToArray();
        }

        public static string StringLiteral(string lit, out int count)
        {
            char term = lit[0];
            lit = lit.Substring(1, lit.Length - 2);
            string str = "";
            int state = 0;
            int accum = 0;
            for (count = 0; count < lit.Length; count++)
            {
                char ch = lit[count];
                switch (state)
                {
                    case 0:
                        switch (ch)
                        {
                            case '\\':
                                state = 1;
                                break;

                            case '"':
                            case '\'':
                                if (ch == term) return str; 
                                else str += ch; break;

                            default:
                                str += ch;
                                break;
                        }
                        break;

                    case 1:
                        switch (ch)
                        {
                            case 'n':
                                str += '\n';
                                state = 0;
                                break;
                            case 'r':
                                str += '\r';
                                state = 0;
                                break;
                            case 't':
                                str += '\t';
                                state = 0;
                                break;
                            case 'b':
                                str += '\b';
                                state = 0;
                                break;
                            case 'v':
                                str += '\v';
                                state = 0;
                                break;
                            case 'x':
                                state = 100;
                                state = 0;
                                break;
                            default:
                                if (ch >= '0' && ch <= '9')
                                {
                                    accum = ch - '0';
                                    state = 10;
                                }
                                else
                                {
                                    str += ch;
                                    state = 0;
                                }
                                break;
                        }
                        break;

                    case 10:
                    case 11:
                        state++;
                        if (ch >= '0' && ch <= '9' && state < 13)
                        {
                            accum *= 10;
                            accum += ch - '0';
                        }
                        else if (ch == term)
                        {
                            str += (char)accum;
                            return str;
                        }
                        else
                        {
                            str += (char)accum;
                            str += ch;
                            state = 0;
                        }
                        if (state == 12)
                        {
                            str += (char)accum;
                            state = 0;
                        }
                        break;


                    case 100:
                    case 101:
                    case 102:
                        accum <<= 4;
                        accum |= int.Parse("" + ch, NumberStyles.HexNumber);
                        state++;
                        if (state == 103)
                        {
                            char ch1 = (char)accum;
                            str += ch1;
                            state = 0;
                        }
                        break;
                }
            }
            return str;
        }
        public static JSObjectBase ToObject(ExecutionContext GLOBAL, object ob)
        {
            string stringVal = ob as string;
            if (ob is bool) return new BoolObject(GLOBAL, (bool)ob);
            else if (ob == null) return new JSObject();
            else if (ob is double) return new NumberObject(GLOBAL, (double)ob);
            else if (stringVal != null) return new StringObject(GLOBAL, stringVal.ToString());
            else if (ob is DateTime) return new JSDate(GLOBAL, (DateTime)ob);
            else if (!(ob is JSObjectBase)) return new JSInstanceWrapper(GLOBAL, ob);
            else return (JSObjectBase)ob;
        }

        public static string Typeof(object t)
        {
            string v;
            if (t is JSUndefined)
                v = "Undefined";
            else if (t == null)
                v = "Object";
            else if (t is bool)
                v = "Boolean";
            else if (t is double)
                v = "Number";
            else if (t is string)
                v = "String";
            else if (t is JSArray)
                v = "Object";
            else v = ((JSObjectBase)t).Class;
            return v;
        }

        public static object GetPrototype(ExecutionContext GLOBAL, object o)
        {
            if (o is JSObjectBase) return ((JSObjectBase)o).GetItem(GLOBAL, "prototype").GetValue(GLOBAL);
            else return JSUndefined.Undefined;
        }
    }

    public class ObjectFun : JSObject
    {
        public JSObject mPrototype;
        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            return Construct(GLOBAL, a, x);
        }

        public override object Construct(ExecutionContext GLOBAL, JSObjectBase args, ExecutionContext x)
        {
            JSObject result;
            int alen = (int)JSObject.ToNumber(GLOBAL, args.GetItem(GLOBAL, "length").GetValue(GLOBAL));
            if (alen > 0) result = (JSObject)JSObject.ToObject(GLOBAL, args.GetItem(GLOBAL, "0").GetValue(GLOBAL));
            else result = new JSObject();
            result.DefProp(GLOBAL, "prototype", mPrototype);
            result.DefProp(GLOBAL, "constructor", this);
            return result;
        }

        public ObjectFun(ExecutionContext GLOBAL)
        {
            mPrototype = new JSObject();
            mPrototype.SetItem(GLOBAL, "toString", new JSNativeMethod(typeof(JSObject), "StaticToString"));
            mPrototype.SetItem(GLOBAL, "valueOf", new JSNativeMethod(typeof(JSObject), "ToPrimitive"));
            DefProp(GLOBAL, "prototype", mPrototype, false, false, false, false);
        }
    }

    public class TypeError : Exception
    {
        public readonly string filename;
        public readonly int lineno;
        public TypeError(string msg) : base(msg) { }
        public TypeError(string msg, string file, int line)
            : base(msg)
        {
            filename = file;
            lineno = line;
        }
    }

    public class URIError : Exception
    {
        public URIError(string s) : base(s) { }
    }

    public class StringObject : JSObject
    {
        string mValue;
        public override string ToString() { return mValue; }
        [ThisArg]
        public static string Substring(object thisOb, int start)
        {
            return thisOb.ToString().Substring(start);
        }
        [ThisArg]
        public static string Substring(object thisOb, long start, long end)
        {
            string str = thisOb.ToString();
            if (end > str.Length) return str.Substring((int)start);
            else return str.Substring((int)start, (int)(end - start));
        }
        [ThisArg]
        public static string []Split(object thisOb, object pattern)
        {
            if (pattern is Regex)
            {
                return new Regex(pattern.ToString(), RegexOptions.ECMAScript).Split(thisOb.ToString());
            }
            else
            {
                return thisOb.ToString().Split(pattern.ToString().ToCharArray());
            }
        }
        public static string fromCharCode(int code)
        {
            return "" + (char)code;
        }
        [ThisArg, GlobalArg]
        public static double charCodeAt(ExecutionContext GLOBAL, object o, int at)
        {
            string s = JSObject.ToPrimitive(GLOBAL, o);
            if (at >= s.Length) return double.NaN;
            else return (int)s[at];
        }
        [ThisArg, GlobalArg]
        public static double length(ExecutionContext GLOBAL, object o) 
        { 
            return JSObject.ToPrimitive(GLOBAL, o).ToString().Length; 
        }
        [ThisArg, GlobalArg]
        public static string charAt(ExecutionContext GLOBAL, object o, int at)
        {
            string prim = JSObject.ToPrimitive(GLOBAL, o).ToString();
            if (at >= prim.Length) return "";
            else return "" + prim[at];
        }

        [GlobalArg]
        public StringObject(ExecutionContext GLOBAL, string val) 
        { 
            mValue = val;
            StringFun StaticStringFun = (StringFun)jsexec.GlobalOrNull(GLOBAL, "StaticStringFun");
            DefProp(GLOBAL, "constructor", StaticStringFun, false, false, false);
            JSObjectBase prototype = StaticStringFun != null ? StaticStringFun.GetItem(GLOBAL, "prototype").GetValue(GLOBAL) as JSObjectBase : null;
            DefProp(GLOBAL, "prototype", prototype, false, false, false);
            if (prototype != null)
            {
                JSSimpleProperty lengthProp = new JSSimpleProperty("length", this);
                lengthProp.DefineGetter(new JSNativeMethod(typeof(StringObject), "length").GetValue(GLOBAL));
                prototype.SetItem(GLOBAL, "length", lengthProp);
            }
        }
    }

    public class StringFun : JSObject
    {
        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            int alen = (int)JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "length").GetValue(GLOBAL));
            if (alen > 0) return a.GetItem(GLOBAL, "0").GetValue(GLOBAL).ToString();
            else return "";
        }
        public override object Construct(ExecutionContext GLOBAL, JSObjectBase a, ExecutionContext x)
        {
            JSObject result;
            int alen = (int)JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "length").GetValue(GLOBAL));
            if (alen > 0) result = new StringObject(GLOBAL, a.GetItem(GLOBAL, "0").GetValue(GLOBAL).ToString());
            else result = new StringObject(GLOBAL, "");
            result.DefProp(GLOBAL, "constructor", this);
            return result;
        }
        [GlobalArg]
        public StringFun(ExecutionContext GLOBAL)
        {
            JSObject prototype = new StringObject(GLOBAL, "");
            prototype.SetItem(GLOBAL, "toString", new JSNativeMethod(typeof(JSObject), "ToString"));
            prototype.SetItem(GLOBAL, "valueOf", new JSNativeMethod(typeof(JSObject), "ToPrimitive"));
            prototype.SetItem(GLOBAL, "substring", new JSNativeMethod(typeof(StringObject), "Substring"));
            prototype.SetItem(GLOBAL, "split", new JSNativeMethod(typeof(StringObject), "Split"));
            prototype.SetItem(GLOBAL, "fromCharCode", new JSNativeMethod(typeof(StringObject), "fromCharCode"));
            prototype.SetItem(GLOBAL, "charCodeAt", new JSNativeMethod(typeof(StringObject), "charCodeAt"));
            prototype.SetItem(GLOBAL, "charAt", new JSNativeMethod(typeof(StringObject), "charAt"));
            DefProp(GLOBAL, "prototype", prototype, false, false, false, false);
        }
    }
}
