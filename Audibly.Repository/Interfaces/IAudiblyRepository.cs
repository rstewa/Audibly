// Author: rstewa
// Created: 3/5/2024
// Updated: 3/5/2024

namespace Audibly.Repository.Interfaces;

/// <summary>
/// Defines methods for interacting with the Audibly database.
/// </summary>
public interface IAudiblyRepository
{
    /// <summary>
    /// Returns the audiobook repository.
    /// </summary>
    IAudiobookRepository Audiobooks { get; }
}