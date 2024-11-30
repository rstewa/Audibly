// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/22/2024

using Audibly.Models;

namespace Audibly.Repository.Interfaces;

public interface IAudiobookRepository
{
        /// <summary>
        ///     Returns all audiobooks.
        /// </summary>
        Task<IEnumerable<Audiobook>> GetAsync();

        /// <summary>
        ///     Returns all audiobooks with a data field matching the start of the given string.
        /// </summary>
        Task<IEnumerable<Audiobook>> GetAsync(string search);

        /// <summary>
        ///     Returns the audiobook with the given file path.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        Task<Audiobook?> GetByFilePathAsync(string filePath);

    // get now playing audiobook
    /// <summary>
    ///     Returns the audiobook that is currently playing.
    /// </summary>
    /// <returns></returns>
    Task<Audiobook?> GetNowPlayingAsync();

    /// <summary>
    ///     Returns the audiobook with the given title and author.
    /// </summary>
    /// <param name="title"></param>
    /// <param name="author"></param>
    /// <param name="composer"></param>
    /// <returns></returns>
    Task<Audiobook?> GetByTitleAuthorComposerAsync(string title, string author, string composer);

    /// <summary>
    ///     Returns the audiobook with the given id.
    /// </summary>
    Task<Audiobook?> GetAsync(Guid id);

    /// <summary>
    ///     Adds a new audiobook if the audiobook does not exist, updates the
    ///     existing audiobook otherwise.
    /// </summary>
    Task<Audiobook?> UpsertAsync(Audiobook audiobook);

    /// <summary>
    ///     Deletes a audiobook.
    /// </summary>
    Task DeleteAsync(Guid audiobookId);
}