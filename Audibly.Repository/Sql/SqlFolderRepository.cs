// Author: rstewa · https://github.com/rstewa
// Updated: 03/11/2025

using Audibly.Models;
using Audibly.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Audibly.Repository.Sql;

public class SqlFolderRepository(AudiblyContext db) : IFolderRepository
{
    #region IFolderRepository Members

    public async Task<IEnumerable<Folder>> GetAsync()
    {
        return await db.Folders
            .Include(x => x.Audiobooks)
            .OrderBy(folder => folder.Name)
            .ToListAsync();
    }

    public async Task<Folder?> GetAsync(Guid id)
    {
        return await db.Folders
            .Include(x => x.Audiobooks)
            .FirstOrDefaultAsync(folder => folder.Id == id);
    }

    public async Task<Folder?> UpsertAsync(Folder folder)
    {
        var existingFolder = await db.Folders
            .Include(x => x.Audiobooks)
            .FirstOrDefaultAsync(x => x.Id == folder.Id);

        if (existingFolder == null)
            db.Folders.Add(folder);
        else
            db.Entry(existingFolder).CurrentValues.SetValues(folder);

        await db.SaveChangesAsync();

        return folder;
    }

    public async Task DeleteAsync(Guid folderId)
    {
        var folder = await db.Folders
            .Include(x => x.Audiobooks)
            .FirstOrDefaultAsync(x => x.Id == folderId);

        if (folder != null)
        {
            db.Folders.Remove(folder);
            await db.SaveChangesAsync();
        }
    }

    #endregion
}