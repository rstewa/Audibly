// Author: rstewa · https://github.com/rstewa
// Updated: 03/17/2025

using Audibly.Models;
using Microsoft.EntityFrameworkCore;

namespace Audibly.Repository.Sql;

public class AudiblyContext : DbContext
{
    /// <summary>
    ///     Creates a new Audibly DbContext.
    /// </summary>
    /// <param name="options"></param>
    public AudiblyContext(DbContextOptions<AudiblyContext> options) : base(options)
    {
    }

    /// <summary>
    ///     Gets the audiobooks DbSet.
    /// </summary>
    public DbSet<Audiobook> Audiobooks { get; set; }

    public DbSet<ChapterInfo> Chapters { get; set; }

    public DbSet<SourceFile> SourceFiles { get; set; }

    public DbSet<Collection> Folders { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dbPath = folderPath + @"\Audibly.db";
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // modelBuilder.Entity<Audiobook>()
        //     .HasMany(a => a.SourcePaths)
        //     .WithOne(s => s.Audiobook)
        //     .HasForeignKey(s => s.AudiobookId)
        //     .OnDelete(DeleteBehavior.Cascade);
        //
        // modelBuilder.Entity<Audiobook>()
        //     .HasMany(a => a.Chapters)
        //     .WithOne(c => c.Audiobook)
        //     .HasForeignKey(c => c.AudiobookId)
        //     .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Audiobook>()
            .HasIndex(a => new { a.Author, a.Title })
            .IsUnique();

        modelBuilder.Entity<Collection>();
    }
}