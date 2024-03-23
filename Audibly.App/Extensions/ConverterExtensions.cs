// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/22/2024

using System;

namespace Audibly.App.Extensions;

public static class ConverterExtensions
{
    public static int ToInt(this double num)
    {
        return Convert.ToInt32(num);
    }

    public static double ToDouble(this string num)
    {
        return Convert.ToDouble(num);
    }

    public static double ToDouble(this object num)
    {
        return Convert.ToDouble(num);
    }
}