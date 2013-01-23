using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pygmalion
{
    [Apply("Date")]
    class JSDate : JSObject
    {
        static double EndOfTime = 8.64e15;

        static bool finite(double val)
        {
            return !double.IsNaN(val) && !double.IsPositiveInfinity(val) && !double.IsNegativeInfinity(val);
        }

        public static double MakeTime(double hour, double min, double sec, double ms)
        {
            if (!finite(hour) || !finite(min) || !finite(sec) || !finite(ms)) return double.NaN;
            return ((((((int)hour * 60) + (int)min) * 60) + (int)sec) * 1000) + (int)ms;
        }

        public static double MakeDay(double year, double month, double day)
        {
            if (!finite(year) || !finite(month) || !finite(day)) return double.NaN;
            DateTime dt = new DateTime((int)year, (int)month, (int)day);
            return dt.ToBinary() / 10000;
        }

        public static double MakeDate(double day, double ms)
        {
            if (!finite(day) || !finite(ms)) return double.NaN;
            return (day * 1000 * 3600 * 24) + ms;
        }

        public static double TimeClip(double ms)
        {
            if (!finite(ms)) return double.NaN;
            if (ms > EndOfTime) return double.NaN;
            return Math.Floor(ms);
        }

        public static string Date(params double[] args)
        {
            return DateTime.Now.ToString();
        }

        [ThisArg]
        public static int getDate(JSDate dt)
        {
            return dt.Value.Day;
        }

        [ThisArg]
        public static int getDay(JSDate dt)
        {
            return (int)dt.Value.DayOfWeek;
        }

        [ThisArg]
        public static int getFullYear(JSDate dt)
        {
            return dt.Value.Year;
        }

        [ThisArg]
        public static int getHours(JSDate dt)
        {
            return dt.Value.Hour;
        }

        [ThisArg]
        public static int getMilliseconds(JSDate dt)
        {
            return dt.Value.Millisecond;
        }

        [ThisArg]
        public static int getMinutes(JSDate dt)
        {
            return dt.Value.Minute;
        }

        [ThisArg]
        public static int getMonth(JSDate dt)
        {
            return dt.Value.Month;
        }

        [ThisArg]
        public static int getSeconds(JSDate dt)
        {
            return dt.Value.Second;
        }

        [ThisArg]
        public static long getTime(JSDate dt)
        {
            return (dt.Value.Ticks - (new DateTime(1970, 1, 1)).Ticks) / 10000; 
        }

        [ThisArg]
        public static int getTimezoneOffset(JSDate dt)
        {
            return (int)((dt.Value.ToFileTime() - dt.Value.ToFileTimeUtc()) / 60 * 1000 * 10000);
        }

        [ThisArg]
        public static int getUTCDate(JSDate dt)
        {
            return dt.Value.ToUniversalTime().Day;
        }

        [ThisArg]
        public static int getUTCDay(JSDate dt)
        {
            return (int)dt.Value.ToUniversalTime().DayOfWeek;
        }

        [ThisArg]
        public static int getUTCMonth(JSDate dt)
        {
            return dt.Value.ToUniversalTime().Month;
        }

        [ThisArg]
        public static int getUTCFullYear(JSDate dt)
        {
            return dt.Value.ToUniversalTime().Year;
        }

        [ThisArg]
        public static int getUTCHours(JSDate dt)
        {
            return dt.Value.ToUniversalTime().Hour;
        }

        [ThisArg]
        public static int getUTCMinutes(JSDate dt)
        {
            return dt.Value.ToUniversalTime().Minute;
        }

        [ThisArg]
        public static int getUTCSeconds(JSDate dt)
        {
            return dt.Value.ToUniversalTime().Second;
        }

        [ThisArg]
        public static int getUTCMilliseconds(JSDate dt)
        {
            return dt.Value.ToUniversalTime().Millisecond;
        }

        [ThisArg]
        public static int getYear(JSDate dt)
        {
            return dt.Value.Year;
        }

        [GlobalArg]
        public static long parse(ExecutionContext GLOBAL, string date)
        {
            return JSDate.getTime(new JSDate(GLOBAL, DateTime.Parse(date)));
        }

        [ThisArg]
        public static void setDate(JSDate dt, int newdate)
        {
            dt.Value = new DateTime(dt.Value.Year, dt.Value.Month, newdate, dt.Value.Hour, dt.Value.Minute, dt.Value.Second, dt.Value.Millisecond);
        }

        [ThisArg]
        public static void setFullYear(JSDate dt, int newyear)
        {
            dt.Value = new DateTime(newyear, dt.Value.Month, dt.Value.Day, dt.Value.Hour, dt.Value.Minute, dt.Value.Second, dt.Value.Millisecond);
        }

        [ThisArg]
        public static void setHours(JSDate dt, int newhour)
        {
            dt.Value = new DateTime(dt.Value.Year, dt.Value.Month, dt.Value.Day, newhour, dt.Value.Minute, dt.Value.Second, dt.Value.Millisecond);
        }

        [ThisArg]
        public static void setMilliseconds(JSDate dt, int milliseconds)
        {
            dt.Value = new DateTime(dt.Value.Year, dt.Value.Month, dt.Value.Day, dt.Value.Hour, dt.Value.Minute, dt.Value.Second, milliseconds);
        }

        [ThisArg]
        public static void setMinutes(JSDate dt, int minutes)
        {
            dt.Value = new DateTime(dt.Value.Year, dt.Value.Month, dt.Value.Day, dt.Value.Hour, minutes, dt.Value.Second, dt.Value.Millisecond);
        }

        [ThisArg]
        public static void setMonth(JSDate dt, int month)
        {
            dt.Value = new DateTime(dt.Value.Year, month, dt.Value.Day, dt.Value.Hour, dt.Value.Minute, dt.Value.Second, dt.Value.Millisecond);
        }

        [ThisArg]
        public static void setSeconds(JSDate dt, int seconds)
        {
            dt.Value = new DateTime(dt.Value.Year, dt.Value.Month, dt.Value.Day, dt.Value.Hour, dt.Value.Minute, seconds, dt.Value.Millisecond);
        }

        [ThisArg]
        public static void setTime(JSDate dt, long time)
        {
            dt.Value = new DateTime(time * 10000 + ((new DateTime(1970, 1, 1)).Ticks));
        }

        [ThisArg]
        public static void setUTCDate(JSDate dt, int date)
        {
            DateTime utc = dt.Value.ToUniversalTime();
            utc = new DateTime(utc.Year, utc.Month, date, utc.Hour, utc.Minute, utc.Second, utc.Millisecond);
            dt.Value = utc.ToLocalTime();
        }

        [ThisArg]
        public static void setUTCMonth(JSDate dt, int month)
        {
            DateTime utc = dt.Value.ToUniversalTime();
            utc = new DateTime(utc.Year, month, utc.Day, utc.Hour, utc.Minute, utc.Second, utc.Millisecond);
            dt.Value = utc.ToLocalTime();
        }

        [ThisArg]
        public static void setUTCFullYear(JSDate dt, int year)
        {
            DateTime utc = dt.Value.ToUniversalTime();
            utc = new DateTime(year, utc.Month, utc.Day, utc.Hour, utc.Minute, utc.Second, utc.Millisecond);
            dt.Value = utc.ToLocalTime();
        }

        [ThisArg]
        public static void setUTCHours(JSDate dt, int hours)
        {
            DateTime utc = dt.Value.ToUniversalTime();
            utc = new DateTime(utc.Year, utc.Month, utc.Day, hours, utc.Minute, utc.Second, utc.Millisecond);
            dt.Value = utc.ToLocalTime();
        }

        [ThisArg]
        public static void setUTCMinutes(JSDate dt, int minutes)
        {
            DateTime utc = dt.Value.ToUniversalTime();
            utc = new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, minutes, utc.Second, utc.Millisecond);
            dt.Value = utc.ToLocalTime();
        }

        [ThisArg]
        public static void setUTCSeconds(JSDate dt, int seconds)
        {
            DateTime utc = dt.Value.ToUniversalTime();
            utc = new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, seconds, utc.Millisecond);
            dt.Value = utc.ToLocalTime();
        }

        [ThisArg]
        public static void setUTCMilliseconds(JSDate dt, int milliseconds)
        {
            setMilliseconds(dt, milliseconds);
        }

        [ThisArg]
        public static void setYear(JSDate dt, int year)
        {
            int newyear = dt.Value.Year;
            newyear = (newyear - newyear % 100) + (year % 100);
            dt.Value = new DateTime(newyear, dt.Value.Month, dt.Value.Day, dt.Value.Hour, dt.Value.Minute, dt.Value.Second, dt.Value.Millisecond);
        }

        [ThisArg]
        public static string toGMTString(JSDate dt)
        {
            return toUTCString(dt);
        }

        [ThisArg]
        public static string toLocaleDateString(JSDate dt)
        {
            return dt.Value.ToLongDateString();
        }

        [ThisArg]
        public static string toLocaleTimeString(JSDate dt)
        {
            return dt.Value.ToShortTimeString();
        }

        [ThisArg]
        public static string toLocaleString(JSDate dt)
        {
            return dt.Value.ToString();
        }

        [ThisArg]
        public static string toDateString(JSDate dt)
        {
            return dt.Value.ToLongDateString();
        }
        
        [ThisArg]
        public static string toTimeString(JSDate dt)
        {
            return dt.Value.ToShortTimeString();
        }

        [ThisArg]
        public static string toUTCString(JSDate dt)
        {
            return dt.Value.ToUniversalTime().ToString();
        }

        public static long UTC(ExecutionContext GLOBAL)
        {
            return JSDate.getTime(new JSDate(GLOBAL));
        }

        // Object part

        public override string Class { get { return "Date"; } }

        DateTime mDateTime;
        [Browsable(false)]
        public DateTime Value { get { return mDateTime; } set { mDateTime = value; } }

        public override string ToString()
        {
            return mDateTime.ToString();
        }

        public static void SetupPrototype(ExecutionContext GLOBAL, JSObject Prototype)
        {
            Prototype.SetItem(GLOBAL, "getDate", new JSNativeMethod(typeof(JSDate), "getDate"));
            Prototype.SetItem(GLOBAL, "getDay", new JSNativeMethod(typeof(JSDate), "getDay"));
            Prototype.SetItem(GLOBAL, "getFullYear", new JSNativeMethod(typeof(JSDate), "getFullYear"));
            Prototype.SetItem(GLOBAL, "getHours", new JSNativeMethod(typeof(JSDate), "getHours"));
            Prototype.SetItem(GLOBAL, "getMilliseconds", new JSNativeMethod(typeof(JSDate), "getMilliseconds"));
            Prototype.SetItem(GLOBAL, "getMinutes", new JSNativeMethod(typeof(JSDate), "getMinutes"));
            Prototype.SetItem(GLOBAL, "getMonth", new JSNativeMethod(typeof(JSDate), "getMonth"));
            Prototype.SetItem(GLOBAL, "getSeconds", new JSNativeMethod(typeof(JSDate), "getSeconds"));
            Prototype.SetItem(GLOBAL, "getTime", new JSNativeMethod(typeof(JSDate), "getTime"));
            Prototype.SetItem(GLOBAL, "getTimezoneOffset", new JSNativeMethod(typeof(JSDate), "getTimezoneOffset"));
            Prototype.SetItem(GLOBAL, "getUTCDate", new JSNativeMethod(typeof(JSDate), "getUTCDate"));
            Prototype.SetItem(GLOBAL, "getUTCDay", new JSNativeMethod(typeof(JSDate), "getUTCDay"));
            Prototype.SetItem(GLOBAL, "getUTCMonth", new JSNativeMethod(typeof(JSDate), "getUTCMonth"));
            Prototype.SetItem(GLOBAL, "getUTCFullYear", new JSNativeMethod(typeof(JSDate), "getUTCFullYear"));
            Prototype.SetItem(GLOBAL, "getUTCHours", new JSNativeMethod(typeof(JSDate), "getUTCHours"));
            Prototype.SetItem(GLOBAL, "getUTCMinutes", new JSNativeMethod(typeof(JSDate), "getUTCMinutes"));
            Prototype.SetItem(GLOBAL, "getUTCSeconds", new JSNativeMethod(typeof(JSDate), "getUTCSeconds"));
            Prototype.SetItem(GLOBAL, "getUTCMilliseconds", new JSNativeMethod(typeof(JSDate), "getUTCMilliseconds"));
            Prototype.SetItem(GLOBAL, "getYear", new JSNativeMethod(typeof(JSDate), "getYear"));
            Prototype.SetItem(GLOBAL, "parse", new JSNativeMethod(typeof(JSDate), "parse"));
            Prototype.SetItem(GLOBAL, "setDate", new JSNativeMethod(typeof(JSDate), "setDate"));
            Prototype.SetItem(GLOBAL, "setFullYear", new JSNativeMethod(typeof(JSDate), "setFullYear"));
            Prototype.SetItem(GLOBAL, "setHours", new JSNativeMethod(typeof(JSDate), "setHours"));
            Prototype.SetItem(GLOBAL, "setMilliseconds", new JSNativeMethod(typeof(JSDate), "setMilliseconds"));
            Prototype.SetItem(GLOBAL, "setMinutes", new JSNativeMethod(typeof(JSDate), "setMinutes"));
            Prototype.SetItem(GLOBAL, "setMonth", new JSNativeMethod(typeof(JSDate), "setMonth"));
            Prototype.SetItem(GLOBAL, "setSeconds", new JSNativeMethod(typeof(JSDate), "setSeconds"));
            Prototype.SetItem(GLOBAL, "setTime", new JSNativeMethod(typeof(JSDate), "setTime"));
            Prototype.SetItem(GLOBAL, "setUTCDate", new JSNativeMethod(typeof(JSDate), "setUTCDate"));
            Prototype.SetItem(GLOBAL, "setUTCMonth", new JSNativeMethod(typeof(JSDate), "setUTCMonth"));
            Prototype.SetItem(GLOBAL, "setUTCFullYear", new JSNativeMethod(typeof(JSDate), "setUTCFullYear"));
            Prototype.SetItem(GLOBAL, "setUTCHours", new JSNativeMethod(typeof(JSDate), "setUTCHours"));
            Prototype.SetItem(GLOBAL, "setUTCMinutes", new JSNativeMethod(typeof(JSDate), "setUTCMinutes"));
            Prototype.SetItem(GLOBAL, "setUTCSeconds", new JSNativeMethod(typeof(JSDate), "setUTCSeconds"));
            Prototype.SetItem(GLOBAL, "setUTCMilliseconds", new JSNativeMethod(typeof(JSDate), "setUTCMilliseconds"));
            Prototype.SetItem(GLOBAL, "setYear", new JSNativeMethod(typeof(JSDate), "setYear"));
            Prototype.SetItem(GLOBAL, "toDateString", new JSNativeMethod(typeof(JSDate), "toDateString"));
            Prototype.SetItem(GLOBAL, "toGMTString", new JSNativeMethod(typeof(JSDate), "toGMTString"));
            Prototype.SetItem(GLOBAL, "toLocaleDateString", new JSNativeMethod(typeof(JSDate), "toLocaleDateString"));
            Prototype.SetItem(GLOBAL, "toLocaleTimeString", new JSNativeMethod(typeof(JSDate), "toLocaleTimeString"));
            Prototype.SetItem(GLOBAL, "toLocaleString", new JSNativeMethod(typeof(JSDate), "toLocaleString"));
//toSource() 	Represents the source code of an object 	1 	-
            Prototype.SetItem(GLOBAL, "toString", new JSNativeMethod(typeof(JSDate), "ToString"));
            Prototype.SetItem(GLOBAL, "toTimeString", new JSNativeMethod(typeof(JSDate), "toTimeString"));
            Prototype.SetItem(GLOBAL, "toUTCString", new JSNativeMethod(typeof(JSDate), "toUTCString"));
            Prototype.SetItem(GLOBAL, "UTC", new JSNativeMethod(typeof(JSDate), "UTC"));
            Prototype.SetItem(GLOBAL, "valueOf", new JSNativeMethod(typeof(JSDate), "getTime"));
        }

        [GlobalArg]
        public JSDate(ExecutionContext GLOBAL)
        {
            mDateTime = DateTime.UtcNow;
            JSDate mPrototype = (JSDate)jsexec.GlobalOrNull(GLOBAL, "DatePrototype");
            SetItem(GLOBAL, "prototype", new JSSimpleProperty("prototype", mPrototype));
        }

        [GlobalArg]
        public JSDate(ExecutionContext GLOBAL, DateTime dt) : this(GLOBAL) { mDateTime = dt; }
        [GlobalArg]
        public JSDate(ExecutionContext GLOBAL, double year) : this(GLOBAL) { }
        [GlobalArg]
        public JSDate(ExecutionContext GLOBAL, double year, double month, params double[] args)
            : this(GLOBAL, year)
        {
            if (args.Length == 0)
                mDateTime = new DateTime((int)year, (int)month, 1);
            else if (args.Length == 1)
                mDateTime = new DateTime((int)year, (int)month, (int)args[0]);
            else if (args.Length == 2)
                mDateTime = new DateTime((int)year, (int)month, (int)args[0], (int)args[1], 0, 0);
            else if (args.Length == 3)
                mDateTime = new DateTime((int)year, (int)month, (int)args[0], (int)args[1], (int)args[2], 0);
            else if (args.Length == 4)
                mDateTime = new DateTime((int)year, (int)month, (int)args[0], (int)args[1], (int)args[2], (int)args[3]);
            else
                mDateTime = new DateTime((int)year, (int)month, (int)args[0], (int)args[1], (int)args[2], (int)args[3], (int)args[4]);
        }
    }
}
