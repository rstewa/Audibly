// Author: rstewa · https://github.com/rstewa

namespace Audibly.App.ViewModels;

/// <summary>
///     Represents a watched folder that is scanned for audiobooks.
/// </summary>
public class WatchedFolder
{
    public string Token { get; set; } = string.Empty;
    public string DisplayPath { get; set; } = string.Empty;
}

