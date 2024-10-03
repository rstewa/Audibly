// Author: rstewa · https://github.com/rstewa
// Created: 04/15/2024
// Updated: 10/02/2024

using Audibly.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Audibly.Repository.Sql;

public class SqlAudiblyRepository : IAudiblyRepository
{
    private readonly DbContextOptions<AudiblyContext> _dbContextOptions;

    public SqlAudiblyRepository(DbContextOptions<AudiblyContext> dbContextOptions)
    {
        _dbContextOptions = dbContextOptions;
        // using var db = new AudiblyContext(_dbContextOptions);
        // db.Database.EnsureCreated();
    }

    public IAudiobookRepository Audiobooks => new SqlAudiobookRepository(
        new AudiblyContext(_dbContextOptions));
}