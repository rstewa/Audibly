// Author: rstewa · https://github.com/rstewa
// Updated: 03/11/2025

using Audibly.Models;

namespace Audibly.Repository.Interfaces;

public interface IFolderRepository
{
    Task<IEnumerable<Folder>> GetAsync();
    Task<Folder?> GetAsync(Guid id);
    Task<Folder?> UpsertAsync(Folder folder);
    Task DeleteAsync(Guid folderId);
}