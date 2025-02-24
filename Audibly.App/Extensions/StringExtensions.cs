// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/22/2024

using System;
using System.Security.Cryptography;
using System.Text;

namespace Audibly.App.Extensions;

public static class StringExtensions
{
    public static string FormatText(this string str, params object[] args)
    {
        return str.Replace("\\n", "\r\n\r\n").Replace("\\", "");
    }
    public static string GetSha256Hash(this string str)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(str);
        var hash = SHA256.HashData(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    public static Uri AsUri(this string str)
    {
        return new Uri(str);
    }

    public static double AsDouble(this string str)
    {
        return double.Parse(str);
    }
}