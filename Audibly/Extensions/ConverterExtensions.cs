using System;

namespace Audibly.Extensions
{
    public static class ConverterExtensions
    {
        public static int ToInt(this double num) => Convert.ToInt32(num);
        public static double ToDouble(this string num) => Convert.ToDouble(num);
        public static double ToDouble(this object num) => Convert.ToDouble(num);
    }
}
