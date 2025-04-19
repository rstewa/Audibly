// Author: rstewa · https://github.com/rstewa
// Updated: 03/17/2025

using Audibly.Models;
using Audibly.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Audibly.Repository.Sql;

public class SqlCollectionRepository(AudiblyContext db) : ICollectionRepository
{
    #region ICollectionRepository Members

    public async Task<IEnumerable<Collection>> GetAsync()
    {
        return await db.Collections
            .Include(x => x.Audiobooks)
            .OrderBy(folder => folder.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Collection>> GetAllChildrenAsync(Guid? parentId)
    {
        return await db.Collections
            .Include(x => x.Audiobooks)
            .Where(folder => folder.ParentFolderId == parentId)
            .ToListAsync();
    }

    public async Task<Collection?> GetAsync(Guid id)
    {
        return await db.Collections
            .Include(x => x.Audiobooks)
            .FirstOrDefaultAsync(folder => folder.Id == id);
    }

    public async Task<Collection?> UpsertAsync(Collection collection)
    {
        var existingFolder = await db.Collections
            .Include(x => x.Audiobooks)
            .FirstOrDefaultAsync(x => x.Id == collection.Id);

        if (existingFolder == null)
            db.Collections.Add(collection);
        else
            db.Entry(existingFolder).CurrentValues.SetValues(collection);

        await db.SaveChangesAsync();

        return collection;
    }

    public async Task DeleteAsync(Guid folderId)
    {
        var folder = await db.Collections
            .Include(x => x.Audiobooks)
            .FirstOrDefaultAsync(x => x.Id == folderId);

        if (folder != null)
        {
            db.Collections.Remove(folder);
            await db.SaveChangesAsync();
        }
    }

    #endregion
}