// Author: rstewa · https://github.com/rstewa
// Updated: 03/11/2025

namespace Audibly.Repository.Interfaces;

/// <summary>
///     Defines methods for interacting with the Audibly database.
/// </summary>
public interface IAudiblyRepository
{
    /// <summary>
    ///     Returns the audiobook repository.
    /// </summary>
    IAudiobookRepository Audiobooks { get; }

    IFolderRepository Folders { get; }
}