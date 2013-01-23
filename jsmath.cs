using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pygmalion
{
    internal class JSMath
    {
        static Random mRandom = new Random();

        [Browsable(false)]
        public static readonly double E = System.Math.E;
        [Browsable(false)]
        public static readonly double LN10 = System.Math.Log(10.0);
        [Browsable(false)]
        public static readonly double LN2 = System.Math.Log(2.0);
        [Browsable(false)]
        public static readonly double LOG2E = System.Math.Log(System.Math.E, 2.0);
        [Browsable(false)]
        public static readonly double LOG10E = System.Math.Log(System.Math.E, 10.0);
        [Browsable(false)]
        public static readonly double PI = System.Math.PI;
        [Browsable(false)]
        public static readonly double SQRT1_2 = System.Math.Sqrt(0.5);
        [Browsable(false)]
        public static readonly double SQRT2 = System.Math.Sqrt(2.0);

        public static double abs(double a) { return System.Math.Abs(a); }
        public static double acos(double a) { return System.Math.Acos(a); }
        public static double asin(double a) { return System.Math.Asin(a); }
        public static double atan(double a) { return System.Math.Atan(a); }
        public static double atan2(double y, double x) { return System.Math.Atan2(y, x); }
        public static double ceil(double a) { return System.Math.Ceiling(a); }
        public static double cos(double a) { return System.Math.Cos(a); }
        public static double exp(double a) { return System.Math.Exp(a); }
        public static double floor(double a) { return System.Math.Floor(a); }
        public static double log(double a) { return System.Math.Log(a); }
        [Length(2)]
        public static double max(params double[] vals)
        {
            if (vals.Length == 0) return Double.NegativeInfinity;
            double max = vals[0];
            foreach (double val in vals)
            {
                if (Double.IsNaN(val)) return Double.NaN;
                max = System.Math.Max(val, max);
            }
            return max;
        }
        [Length(2)]
        public static double min(params double[] vals)
        {
            if (vals.Length == 0) return Double.PositiveInfinity;
            double min = vals[0];
            foreach (double val in vals)
            {
                if (Double.IsNaN(val)) return Double.NaN;
                min = System.Math.Min(val, min);
            }
            return min;
        }
        public static double pow(double x, double y) { return System.Math.Pow(x, y); }
        public static double random() { return mRandom.NextDouble(); }
        public static double round(double x) { return System.Math.Round(x); }
        public static double sin(double a) { return System.Math.Sin(a); }
        public static double sqrt(double a) { return System.Math.Sqrt(a); }
        public static double tan(double a) { return System.Math.Tan(a); }
    }
}
