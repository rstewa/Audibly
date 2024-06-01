// Author: rstewa · https://github.com/rstewa
// Created: 4/15/2024
// Updated: 6/1/2024

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
}