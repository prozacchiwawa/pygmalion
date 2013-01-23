using System;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;

namespace pygmalion
{
    public class StdLibFunction : JSObject
    {
        public override string Class
        {
            get { return "Function"; }
        }
    }
    public class VersionFun : StdLibFunction
    {
        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            return "0";
        }

        public override object Construct(ExecutionContext GLOBAL, JSObjectBase a, ExecutionContext x)
        {
            return JSUndefined.Undefined;
        }
    }

    public class GcFun : StdLibFunction
    {
        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            System.Console.WriteLine("used memory " + GC.GetTotalMemory(true));
            return JSUndefined.Undefined;
        }
    }

    public class ParseInt : StdLibFunction
    {
        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            int alen = (int)JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "length").GetValue(GLOBAL));
            if (alen == 0) return Double.NaN;
            else return (double)(long)JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "0").GetValue(GLOBAL));
        }
    }

    public class ParseFloat : StdLibFunction
    {
        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            int alen = (int)JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "length").GetValue(GLOBAL));
            if (alen == 0) return Double.NaN;
            else return JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "0").GetValue(GLOBAL));
        }
    }

    public class EscapeFun : StdLibFunction
    {
        // Punct
        // Mask 1 excludes @*_-.
        // Mask 2 excludes @*_-.
        // Mask 4 excludes @*_-.+/
        // AlphaNumeric, spaces
        // Mask 1 excludes letters and numbers
        // Mask 2 escapes ' ' with +
        // Mask 4 excludes letters and numbers
        // 1 and 4 unexclude ' '
        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            int alen = (int)JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "length").GetValue(GLOBAL));
            int mask = 4;
            if (alen == 0) return JSUndefined.Undefined;
            else if (alen > 1)
            {
                mask = (int)JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "1").GetValue(GLOBAL));
            }
            string instr = JSObject.ToPrimitive(GLOBAL, a.GetItem(GLOBAL, "0").GetValue(GLOBAL));
            StringBuilder result = new StringBuilder();
            foreach (char ch in instr)
            {
                if (ch == ' ' && mask == 2)
                {
                    result.Append('+');
                }
                else if ((ch == '+' || ch == '/') && (mask & 4) != 0)
                {
                    result.Append(ch);
                }
                else if ((ch == '@' || ch == '*' || ch == '_' || ch == '-' || ch == '.') && mask != 0)
                {
                    result.Append(ch);
                }
                else if (char.IsLetterOrDigit(ch) && mask != 0)
                {
                    result.Append(ch);
                }
                else
                {
                    if (ch >= (char)0x100)
                        result.Append(string.Format("%u{0:X4}", (int)ch));
                    else
                        result.Append(string.Format("%{0:X2}", (int)ch));
                }
            }
            return result.ToString();
        }
    }

    public class UnescapeFun : StdLibFunction
    {
        Regex escapedChar = new Regex("%[0-9a-fA-F]{2}|%u[0-9a-fA-F]{4}");
        public override string Class { get { return "function"; } }
        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            int alen = (int)JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "length").GetValue(GLOBAL));
            StringBuilder result = new StringBuilder();
            Match escChar;
            if (alen == 0) return JSUndefined.Undefined;
            string instr = JSObject.ToPrimitive(GLOBAL, a.GetItem(GLOBAL, "0").GetValue(GLOBAL));
            int startAt = 0;
            int lastUsed = 0;
            while (startAt < instr.Length && (escChar = escapedChar.Match(instr, startAt)).Success)
            {
                startAt = escChar.Index;
                result.Append(instr.Substring(lastUsed, startAt - lastUsed));
                if (escChar.Groups[0].Value[1] == 'u')
                {
                    startAt += 6;
                    result.Append((char)int.Parse(escChar.Groups[0].Value.Substring(2), NumberStyles.HexNumber));
                }
                else
                {
                    startAt += 3;
                    result.Append((char)int.Parse(escChar.Groups[0].Value.Substring(1), NumberStyles.HexNumber));
                }
                lastUsed = startAt;
            }
            if (lastUsed < instr.Length)
                result.Append(instr.Substring(lastUsed));
            return result.ToString();
        }
    }

    public class isFiniteFun : StdLibFunction
    {
        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            int alen = (int)JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "length").GetValue(GLOBAL));
            if (alen == 0) return false;
            double val = JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "0").GetValue(GLOBAL));
            return !double.IsNaN(val) && !double.IsPositiveInfinity(val) && !double.IsNegativeInfinity(val);
        }
    }

    internal class URIFunctionPrivate
    {
        public static string uriEmptySet = "";
        public static string uriReserved = ";/?:@&=+$,";
        public static bool uriUnescaped(char ch, bool addOctothorpe)
        {
            return char.IsLetterOrDigit(ch) || uriMark.Contains(ch) || (addOctothorpe && ch == '#');
        }
        public static string uriMark = "-_.!~*()";
        public static string uriReservedPlusOctothorpe = ";/?:@&=+$,#";

        public static string Decode(string uri, string reservedSet)
        {
            byte[] result = new byte[uri.Length];
            int i, d = 0;
            try
            {
                for (i = 0; i < uri.Length; i++)
                {
                    char ch = uri[i];
                    if (ch == '%')
                    {
                        char r = (char)int.Parse(uri.Substring(i + 1, 2), NumberStyles.HexNumber);
                        if (reservedSet.Contains(r))
                        {
                            result[d++] = (byte)'%';
                            result[d++] = (byte)uri[i + 1];
                            result[d++] = (byte)uri[i + 2];
                        }
                        else
                            result[d++] = (byte)r;
                        i += 2;
                    }
                    else
                    {
                        result[d++] = (byte)ch;
                    }
                }
                return UTF8Encoding.UTF8.GetString(result, 0, d);
            }
            catch (Exception e)
            {
                throw new URIError(e.Message);
            }
        }

        public static string Encode(byte []bytes, bool addOctothorpe)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes)
            {
                if (uriUnescaped((char)b, addOctothorpe))
                    sb.Append(string.Format("%{0:X2}", (int)b));
                else
                    sb.Append((char)b);
            }
            return sb.ToString();
        }
    }

    public class DecodeURIFun : StdLibFunction
    {
        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            int alen = (int)JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "length").GetValue(GLOBAL));
            if (alen == 0) return "";
            string uri = a.GetItem(GLOBAL, "0").GetValue(GLOBAL).ToString();
            return URIFunctionPrivate.Decode(uri, URIFunctionPrivate.uriReservedPlusOctothorpe);
        }
    }

    public class DecodeURIComponentFun : StdLibFunction
    {
        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            int alen = (int)JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "length").GetValue(GLOBAL));
            if (alen == 0) return "";
            string uri = a.GetItem(GLOBAL, "0").GetValue(GLOBAL).ToString();
            return URIFunctionPrivate.Decode(uri, URIFunctionPrivate.uriEmptySet);
        }
    }

    public class EncodeURIFun : StdLibFunction
    {
        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            int alen = (int)JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "length").GetValue(GLOBAL));
            if (alen == 0) return "";
            byte[] bytes = UTF8Encoding.UTF8.GetBytes(a.GetItem(GLOBAL, "0").GetValue(GLOBAL).ToString());
            return URIFunctionPrivate.Encode(bytes, false);
        }
    }

    public class EncodeURIComponentFun : StdLibFunction
    {
        public override object Call(ExecutionContext GLOBAL, object t, JSObjectBase a, ExecutionContext x)
        {
            int alen = (int)JSObject.ToNumber(GLOBAL, a.GetItem(GLOBAL, "length").GetValue(GLOBAL));
            if (alen == 0) return "";
            byte[] bytes = UTF8Encoding.UTF8.GetBytes(a.GetItem(GLOBAL, "0").GetValue(GLOBAL).ToString());
            return URIFunctionPrivate.Encode(bytes, true);
        }
    }
}
