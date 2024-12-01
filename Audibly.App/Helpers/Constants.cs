// Author: rstewa · https://github.com/rstewa
// Created: 4/15/2024
// Updated: 6/1/2024

using System;
using System.Reflection;

namespace Audibly.App.Helpers;

public static class Constants
{
    public const string VolumeGlyph0 = "\uE74F";
    public const string VolumeGlyph1 = "\uE993";
    public const string VolumeGlyph2 = "\uE994";
    public const string VolumeGlyph3 = "\uE995";

    public const string MaximizeGlyph = "\uE740";
    public const string MinimizeGlyph = "\uE73F";

    public const string MaximizeTooltip = "Maximize";
    public const string MinimizeTooltip = "Minimize";

    public const string DatabaseMigrationVersion = "2.1.0.0";

    // public const string ChangeLog
    public static string Version
    {
        get
        {
            var assembly = Assembly.GetEntryAssembly();
            if (assembly == null)
                return string.Empty;

            var version = assembly.GetName().Version;
            if (version == null)
                return string.Empty;

            return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
        }
    }

    public static int CompareVersions(string version1, string version2)
    {
        if (string.IsNullOrEmpty(version1) || string.IsNullOrEmpty(version2))
            throw new ArgumentException("Version strings cannot be null or empty.");

        var version1Parts = version1.Split('.');
        var version2Parts = version2.Split('.');

        for (var i = 0; i < Math.Max(version1Parts.Length, version2Parts.Length); i++)
        {
            var v1 = i < version1Parts.Length ? int.Parse(version1Parts[i]) : 0;
            var v2 = i < version2Parts.Length ? int.Parse(version2Parts[i]) : 0;

            if (v1 < v2)
                return -1;
            if (v1 > v2)
                return 1;
        }

        return 0;
    }
}