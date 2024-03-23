// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/22/2024

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
}