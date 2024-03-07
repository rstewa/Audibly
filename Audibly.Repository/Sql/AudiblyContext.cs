// Author: rstewa
// Created: 3/5/2024
// Updated: 3/5/2024

using Audibly.Models;
using Microsoft.EntityFrameworkCore;

namespace Audibly.Repository;

public class AudiblyContext : DbContext
{
   // public string DbPath { get; }
   
   /// <summary>
   /// Creates a new Audibly DbContext.
   /// </summary>
   /// <param name="options"></param>
   public AudiblyContext(DbContextOptions<AudiblyContext> options) : base(options)
   {
      // var folder = Environment.SpecialFolder.LocalApplicationData;
      // var path = Environment.GetFolderPath(folder);
      // DbPath = System.IO.Path.Join(path, "audibly.db");
   }
   
   // The following configures EF to create a Sqlite database file in the
   // special "local" folder for your platform.
   // protected override void OnConfiguring(DbContextOptionsBuilder options)
   //    => options.UseSqlite($"Data Source={DbPath}");
   
   /// <summary>
   /// Gets the audiobooks DbSet.
   /// </summary>
   public DbSet<Audiobook> Audiobooks { get; set; }
}