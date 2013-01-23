using System;
using System.Collections.Generic;
using System.Text;

namespace pygmalion
{
    public class JSArray : JSObject, IEnumerable<object>
    {
        long mLength;
        ExecutionContext GLOBAL;
        public long length 
        { 
            get { return mLength; } 
            set 
            {
                long i;
                for (i = value; i < mLength; i++)
                {
                    string istr = i.ToString();
                    if (HasProperty(GLOBAL, istr))
                        Delete(istr);
                }
                mLength = value; 
            }
        }

        public override string Class { get { return "Array"; } }

        public override JSProperty GetItem(ExecutionContext GLOBAL, string index)
        {
            return base.GetItem(GLOBAL, index);
        }
        public override void SetItem(ExecutionContext GLOBAL, string index, JSProperty value)
        {
            long i;
            if (long.TryParse(index, out i) && i > length - 1)
            {
                mLength = i + 1;
            }
            base.SetItem(GLOBAL, index, value);
        }

        public JSProperty this[long idx]
        {
            get { return this.GetItem(GLOBAL, idx.ToString()); }
            set { this.SetItem(GLOBAL, idx.ToString(), value); }
        }

        [GlobalArg]
        public static JSArray concat(ExecutionContext GLOBAL, params JSArray[] arrays)
        {
            JSArray newArray = new JSArray(GLOBAL);
            foreach (JSArray ja in arrays)
            {
                int i;
                for (i = 0; i < ja.length; i++)
                {
                    newArray.push(ja[i].GetValue(GLOBAL));
                }
            }
            return newArray;
        }

        [ThisArg, GlobalArg]
        public static string join(ExecutionContext GLOBAL, object self)
        {
            return join(GLOBAL, self, ",");
        }

        [ThisArg, GlobalArg]
        public static string join(ExecutionContext GLOBAL, object selfOb, string sep)
        {
            JSObjectBase self = (JSObjectBase)selfOb;
            List<string> strings = new List<string>();
            int idx;

            for (idx = 0; idx < JSObject.ToNumber(GLOBAL, self.GetItem(GLOBAL, "length").GetValue(GLOBAL)); idx++)
            {
                string istr = idx.ToString();
                if (self.HasOwnProperty(istr) && self.GetItem(GLOBAL, istr).GetValue(GLOBAL) != null)
                    strings.Add(JSObject.ToPrimitive(GLOBAL, self.GetItem(GLOBAL, istr).GetValue(GLOBAL)));
                else
                    strings.Add("");
            }
            return string.Join(sep, strings.ToArray());
        }

        public object pop()
        {
            long idx = length - 1;
            if (idx < 0) return JSUndefined.Undefined;
            else
            {
                object theOb = this[idx].GetValue(GLOBAL);
                Delete(idx.ToString());
                return theOb;
            }
        }

        public object top()
        {
            if (length == 0) return JSUndefined.Undefined;
            else return this[length - 1].GetValue(GLOBAL);
        }

        public virtual object push(object o)
        {
            this[length] = new JSSimpleProperty(length.ToString(), o);
            return o;
        }

        public void reverse()
        {
            long idx;
            Dictionary<long, object> reversed = new Dictionary<long, object>();

            for (idx = 0; idx < length; idx++)
            {
                JSProperty prop = this[idx];
                if (prop != null)
                {
                    object o = prop.GetValue(GLOBAL);
                    if (!(o == null && o == JSUndefined.Undefined))
                        reversed[length - idx - 1] = o;
                }
            }

            for (idx = 0; idx < length; idx++)
            {
                object o;
                if (reversed.TryGetValue(idx, out o) && !(o == null && o == JSUndefined.Undefined))
                    this[idx] = new JSSimpleProperty(idx.ToString(), o);
                else
                    Delete(idx.ToString());
            }
        }

        public object shift()
        {
            int i;
            object result;

            if (length == 0) return JSUndefined.Undefined;

            result = this[0].GetValue(GLOBAL);

            for (i = 1; i < length; i++)
            {
                object o = this[i].GetValue(GLOBAL);
                if (!(o == null && o == JSUndefined.Undefined))
                    this[i - 1].SetValue(GLOBAL, o);
                else
                    Delete((i - 1).ToString());
            }

            return result;
        }

        public JSArray slice(long start, long end)
        {
            long i, target = 0;
            JSArray result;

            if (start < 0)
                start = length - start - 1;
            if (end < 0)
                end = length - end - 1;

            result = new JSArray(GLOBAL, end - start);

            for (i = start; i < end; i++, target++)
            {
                object o = this[i].GetValue(GLOBAL);
                if (!(o == null && o == JSUndefined.Undefined))
                    result[target].SetValue(GLOBAL, o);
            }

            return result;
        }

        int lexorder(ExecutionContext GLOBAL, object a, object b)
        {
            string astr = JSObject.ToPrimitive(GLOBAL, a), bstr = JSObject.ToPrimitive(GLOBAL, b);
            return astr.CompareTo(bstr);
        }

        public delegate int sortorder(ExecutionContext GLOBAL, object a, object b);
        public JSArray sort()
        {
            return sort(lexorder);
        }

        class comparisonAdapter : IComparer<object>
        {
            ExecutionContext GLOBAL;
            sortorder mOrder;
            int IComparer<object>.Compare(object a, object b)
            {
                return mOrder(GLOBAL, a, b);
            }

            public comparisonAdapter(ExecutionContext GLOBAL, sortorder order) { this.GLOBAL = GLOBAL; mOrder = order; }
        }

        class JSComparisonAdapter : IComparer<object>
        {
            JSObjectBase mSorter;
            ExecutionContext GLOBAL;
            int IComparer<object>.Compare(object a, object b)
            {
                if (a.Equals(b)) return 0;
                JSArray array = new JSArray(GLOBAL, new object[] { a, b });
                double number = JSObject.ToNumber(GLOBAL, mSorter.Call(GLOBAL, GLOBAL.currentContext.thisOb, array, GLOBAL.currentContext));
                return (int)number;
            }
            public JSComparisonAdapter(ExecutionContext GLOBAL, JSObjectBase sorter) { this.GLOBAL = GLOBAL; mSorter = sorter; }
        }

        public JSArray sort(sortorder sortfun)
        {
            List<object> objects = new List<object>();
            int i;

            for (i = 0; i < length; i++)
            {
                object o = this.GetItem(GLOBAL, i.ToString()).GetValue(GLOBAL);
                if (!(o == null || o == JSUndefined.Undefined))
                {
                    objects.Add(o);
                    Delete(i.ToString());
                }
            }

            objects.Sort(new comparisonAdapter(GLOBAL, sortfun));

            for (i = 0; i < objects.Count; i++)
            {
                this[i] = new JSSimpleProperty(i.ToString(), objects[i]);
            }

            return this;
        }

        public JSArray sort(ExecutionContext GLOBAL, JSObjectBase sortfun)
        {
            List<object> objects = new List<object>();
            int i;

            for (i = 0; i < length; i++)
            {
                object o = this[i].GetValue(GLOBAL);
                if (!(o == null || o == JSUndefined.Undefined))
                {
                    objects.Add(o);
                    Delete(i.ToString());
                }
            }

            objects.Sort(new JSComparisonAdapter(GLOBAL, sortfun));

            for (i = 0; i < objects.Count; i++)
            {
                this[i] = new JSSimpleProperty(i.ToString(), objects[i]);
            }

            return this;
        }

        public JSArray splice(long first)
        {
            return splice(first, length - 1);
        }

        public JSArray splice(long first, long len)
        {
            JSArray result = new JSArray(GLOBAL);
            if (first < 0) first += length;
            if (len < 0) return result;
            if (first < 0) first = 0;
            long i = first, end = Math.Min(first + len, length);
            while (i < end)
                result.push(this[i++].GetValue(GLOBAL));
            while (i < length)
                this[first++] = this[i++];
            length = first;
            return result;
        }

        [ThisArg, GlobalArg]
        public static string StaticToString(ExecutionContext GLOBAL, object t)
        {
            return join(GLOBAL, t, ",");
        }

        public override string ToString()
        {
            return join(GLOBAL, this, ",");
        }

        public IEnumerator<object> GetEnumerator()
        {
            int i;
            for (i = 0; i < length; i++)
            {
                yield return this[i].GetValue(GLOBAL);
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            int i;
            for (i = 0; i < length; i++)
            {
                yield return this[i].GetValue(GLOBAL);
            }
        }

        void MakeStdProperties(ExecutionContext GLOBAL)
        {
            ArrayFun StaticArrayFun = (ArrayFun)jsexec.GlobalOrNull(GLOBAL, "StaticArrayFun");
            this.SetItem(GLOBAL, "length", new JSNativeProperty(this, GetType().GetProperty("length")));
            DefProp(GLOBAL, "constructor", StaticArrayFun);
        }

        [GlobalArg]
        public JSArray(ExecutionContext GLOBAL)
        {
            this.GLOBAL = GLOBAL;
            MakeStdProperties(GLOBAL);
        }

        [GlobalArg]
        public JSArray(ExecutionContext GLOBAL, long len)
        {
            this.GLOBAL = GLOBAL;
            mLength = len;
            MakeStdProperties(GLOBAL);
        }

        [GlobalArg]
        public JSArray(ExecutionContext GLOBAL, object[] args)
        {
            this.GLOBAL = GLOBAL;
            int idx = 0;
            foreach (object a in args)
            {
                this[idx] = new JSSimpleProperty(idx.ToString(), a);
                idx++;
            }
            MakeStdProperties(GLOBAL);
        }

        public JSArray(ExecutionContext GLOBAL, Array args)
        {
            this.GLOBAL = GLOBAL;
            int idx = 0;
            foreach (object a in args)
            {
                this[idx] = new JSSimpleProperty(idx.ToString(), JSObject.ToJS(GLOBAL, a));
                idx++;
            }
            MakeStdProperties(GLOBAL);
        }
    }

    class ArrayFun : JSObject
    {
        JSObject mPrototype;
        public ArrayFun(ExecutionContext GLOBAL)
        {
            mPrototype = new JSArray(GLOBAL);
            DefProp(GLOBAL, "prototype", mPrototype, false, false, false);
            mPrototype.SetItem(GLOBAL, "toString", new JSNativeMethod(typeof(JSArray), "StaticToString"));
            mPrototype.SetItem(GLOBAL, "valueOf", new JSNativeMethod(typeof(JSArray), "ToString"));
            mPrototype.SetItem(GLOBAL, "join", new JSNativeMethod(typeof(JSArray), "join"));
            mPrototype.SetItem(GLOBAL, "reverse", new JSNativeMethod(typeof(JSArray), "reverse"));
            mPrototype.SetItem(GLOBAL, "sort", new JSNativeMethod(typeof(JSArray), "sort"));
            mPrototype.DefProp(GLOBAL, "constructor", this);
        }
        public override string Class { get { return "function"; } }
        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            return Construct(GLOBAL, a, x);
        }

        public override object Construct(ExecutionContext GLOBAL, JSObjectBase a, ExecutionContext x)
        {
            JSArray ar = new JSArray(GLOBAL);
            long alen = (long)JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "length").GetValue(GLOBAL));
            if (alen == 1 && a.GetItem(GLOBAL, "0").GetValue(GLOBAL) is double)
            {
                ar = new JSArray(GLOBAL, (long)JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "0").GetValue(GLOBAL)));
            }
            else
            {
                long i;
                for (i = 0; i < alen; i++)
                    ar.push(a.GetItem(GLOBAL, i.ToString()).GetValue(GLOBAL));
            }
            ar.DefProp(GLOBAL, "prototype", mPrototype, false);
            ar.DefProp(GLOBAL, "constructor", this);
            return ar;
        }
    }
}
