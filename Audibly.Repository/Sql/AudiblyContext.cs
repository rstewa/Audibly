﻿// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/22/2024

using Audibly.Models;
using Microsoft.EntityFrameworkCore;

namespace Audibly.Repository;

public class AudiblyContext : DbContext
{
    /// <summary>
    ///     Creates a new Audibly DbContext.
    /// </summary>
    /// <param name="options"></param>
    public AudiblyContext(DbContextOptions<AudiblyContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Audiobook>()
            .HasIndex(a => new
            {
                a.Author, a.Title
            })
            .IsUnique();
    }

    /// <summary>
    ///     Gets the audiobooks DbSet.
    /// </summary>
    public DbSet<Audiobook> Audiobooks { get; set; }

    public DbSet<ChapterInfo> Chapters { get; set; }
}