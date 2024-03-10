// Author: rstewa
// Created: 3/5/2024
// Updated: 3/5/2024

using Audibly.Models;
using Audibly.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Audibly.Repository.Sql;

public class SqlAudiobookRepository : IAudiobookRepository
{
    private readonly AudiblyContext _db;
    
    public SqlAudiobookRepository(AudiblyContext db)
    {
        _db = db;
    }
    
    public async Task<IEnumerable<Audiobook>> GetAsync()
    {
        return await _db.Audiobooks
            .AsNoTracking()
            .ToListAsync();
    }
    
    // TODO: fix this bug
    public async Task<Audiobook> GetAsync(Guid id)
    {
        return await _db.Audiobooks
            .AsNoTracking()
            .FirstOrDefaultAsync(audiobook => audiobook.Id == id);
    }
    
    public async Task<IEnumerable<Audiobook>> GetAsync(string search)
    {
        string[] parameters = search.Split(' ');
        return await _db.Audiobooks
            .Where(audiobook =>
                parameters.Any(parameter =>
                    audiobook.Author.StartsWith(parameter) ||
                    audiobook.Title.StartsWith(parameter) ||
                    audiobook.Description.StartsWith(parameter)))
            .OrderByDescending(audiobook =>
                parameters.Count(parameter =>
                    audiobook.Author.StartsWith(parameter) ||
                    audiobook.Title.StartsWith(parameter) ||
                    audiobook.Description.StartsWith(parameter)))
            .AsNoTracking()
            .ToListAsync();
    }
    
    public async Task<Audiobook> UpsertAsync(Audiobook audiobook)
    {
        var current = await _db.Audiobooks
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == audiobook.Id);
        
        if (current == null)
        {
            _db.Audiobooks.Add(audiobook);
        }
        else
        {
            _db.Audiobooks.Update(audiobook);
        }
        
        await _db.SaveChangesAsync();
        return audiobook;
    }
    
    public async Task DeleteAsync(Guid audiobookId)
    {
        var audiobook = await _db.Audiobooks
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == audiobookId);
        
        if (audiobook != null)
        {
            _db.Audiobooks.Remove(audiobook);
            await _db.SaveChangesAsync();
        }
    }
}