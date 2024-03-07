// Author: rstewa
// Created: 3/5/2024
// Updated: 3/5/2024

using Audibly.Models;

namespace Audibly.Repository.Interfaces;

public interface IAudiobookRepository
{
        /// <summary>
        /// Returns all audiobooks. 
        /// </summary>
        Task<IEnumerable<Audiobook>> GetAsync();

        /// <summary>
        /// Returns all audiobooks with a data field matching the start of the given string. 
        /// </summary>
        Task<IEnumerable<Audiobook>> GetAsync(string search);

        /// <summary>
        /// Returns the audiobook with the given id. 
        /// </summary>
        Task<Audiobook> GetAsync(Guid id);

        /// <summary>
        /// Adds a new audiobook if the audiobook does not exist, updates the 
        /// existing audiobook otherwise.
        /// </summary>
        Task<Audiobook> UpsertAsync(Audiobook audiobook);

        /// <summary>
        /// Deletes a audiobook.
        /// </summary>
        Task DeleteAsync(Guid audiobookId);
}