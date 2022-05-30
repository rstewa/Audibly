using System;

namespace Audibly.Extensions
{
    public static class ConverterExtensions
    {
        public static int ToInt(this double num) => Convert.ToInt32(num);
    }
}
