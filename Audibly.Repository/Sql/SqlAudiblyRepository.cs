// Author: rstewa · https://github.com/rstewa
// Created: 3/21/2024
// Updated: 3/22/2024

using Audibly.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Audibly.Repository.Sql;

public class SqlAudiblyRepository : IAudiblyRepository
{
    private readonly DbContextOptions<AudiblyContext> _dbOptions;

    public SqlAudiblyRepository(DbContextOptionsBuilder<AudiblyContext> dbOptionsBuilder)
    {
        _dbOptions = dbOptionsBuilder.Options;
        using var db = new AudiblyContext(_dbOptions);
        db.Database.EnsureCreated();
    }

    public IAudiobookRepository Audiobooks => new SqlAudiobookRepository(
        new AudiblyContext(_dbOptions));
}