// Author: rstewa · https://github.com/rstewa
// Created: 10/03/2024
// Updated: 10/03/2024

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Audibly.Repository.Sql;

public class AudiblyContextFactory : IDesignTimeDbContextFactory<AudiblyContext>
{
    public AudiblyContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AudiblyContext>();

        var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dbPath = folderPath + @"\Audibly.db";
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        return new AudiblyContext(optionsBuilder.Options);
    }
}