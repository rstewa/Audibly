// Author: rstewa · https://github.com/rstewa
// Created: 10/22/2024
// Updated: 10/24/2024

using Audibly.Models;
using Audibly.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Audibly.Repository.Sql;

public class SqlAudiobookRepository(AudiblyContext db) : IAudiobookRepository
{
    public async Task<IEnumerable<Audiobook>> GetAsync()
    {
        return await db.Audiobooks
            .Include(x => x.SourcePaths.OrderBy(source => source.Index))
            .Include(x => x.Chapters.OrderBy(chapter => chapter.Index))
            .OrderBy(audiobook => audiobook.Title)
            // .AsNoTracking()  // todo: testing this out
            .ToListAsync();
    }

    public Task<Audiobook?> GetAsync(string title, string author, string composer)
    {
        return db.Audiobooks
            .Include(x => x.SourcePaths.OrderBy(source => source.Index))
            .Include(x => x.Chapters.OrderBy(chapter => chapter.Index))
            .AsNoTracking()
            .FirstOrDefaultAsync(audiobook =>
                audiobook.Title == title &&
                audiobook.Author == author &&
                audiobook.Composer == composer);
    }

    public async Task<Audiobook?> GetAsync(Guid id)
    {
        return await db.Audiobooks
            .Include(x => x.SourcePaths.OrderBy(source => source.Index))
            .Include(x => x.Chapters.OrderBy(chapter => chapter.Index))
            .AsNoTracking()
            .FirstOrDefaultAsync(audiobook => audiobook.Id == id);
    }

    public async Task<IEnumerable<Audiobook>> GetAsync(string search)
    {
        var parameters = search.Split(' ');
        return await db.Audiobooks
            .Include(x => x.SourcePaths.OrderBy(source => source.Index))
            .Include(x => x.Chapters.OrderBy(chapter => chapter.Index))
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

    public async Task<Audiobook?> UpsertAsync(Audiobook audiobook)
    {
        var current = await db.Audiobooks
            .Include(x => x.SourcePaths.OrderBy(source => source.Index))
            .Include(x => x.Chapters.OrderBy(chapter => chapter.Index))
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Title == audiobook.Title && a.Author == audiobook.Author);

        // TODO: fix this bug
        if (current != null && current.Id != audiobook.Id)
            return audiobook;

        if (current == null)
            db.Audiobooks.Add(audiobook);
        else
            db.Audiobooks.Update(audiobook);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception e)
        {
            return null;
        }

        return audiobook;
    }

    public async Task DeleteAsync(Guid audiobookId)
    {
        var audiobook = await db.Audiobooks
            .Include(x => x.SourcePaths)
            .Include(x => x.Chapters)
            .FirstOrDefaultAsync(a => a.Id == audiobookId);

        if (audiobook != null) db.Remove(audiobook);

        try
        {
            await db.SaveChangesAsync();
        }
        // this is for a really annoying edge case I was running into for some reason
        catch (DbUpdateConcurrencyException ex)
        {
            foreach (var entry in ex.Entries)
                if (entry.Entity is Audiobook || entry.Entity is SourceFile || entry.Entity is ChapterInfo)
                {
                    var proposedValues = entry.CurrentValues;
                    var databaseValues = entry.GetDatabaseValues();

                    if (databaseValues == null)
                    {
                        // The entity was deleted by another process
                        db.Entry(entry.Entity).State = EntityState.Detached;
                    }
                    else
                    {
                        foreach (var property in proposedValues.Properties)
                        {
                            var proposedValue = proposedValues[property];
                            var databaseValue = databaseValues[property];

                            // Decide which value should be written to database
                            proposedValues[property] = proposedValue;
                        }

                        // Refresh original values to bypass next concurrency check
                        entry.OriginalValues.SetValues(databaseValues);
                    }
                }
                else
                {
                    throw new NotSupportedException(
                        "Don't know how to handle concurrency conflicts for "
                        + entry.Metadata.Name);
                }

            // Retry the save operation
            await db.SaveChangesAsync();
        }
    }
}