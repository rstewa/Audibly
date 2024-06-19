// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/22/2024

using System;

namespace Audibly.App.Extensions;

public static class StringExtensions
{
    public static Uri AsUri(this string str)
    {
        return new Uri(str);
    }
    
    public static double AsDouble(this string str)
    {
        return double.Parse(str);
    }
}