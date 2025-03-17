// Author: rstewa · https://github.com/rstewa
// Updated: 03/17/2025

using Audibly.Models;

namespace Audibly.Repository.Interfaces;

public interface ICollectionRepository
{
    Task<IEnumerable<Collection>> GetAsync();
    Task<IEnumerable<Collection>> GetAllChildrenAsync(Guid? parentId);
    Task<Collection?> GetAsync(Guid id);
    Task<Collection?> UpsertAsync(Collection collection);
    Task DeleteAsync(Guid folderId);
}