// Author: rstewa · https://github.com/rstewa
// Updated: 03/17/2025

using Audibly.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Audibly.Repository.Sql;

public class SqlAudiblyRepository : IAudiblyRepository
{
    private readonly DbContextOptions<AudiblyContext> _dbContextOptions;

    public SqlAudiblyRepository(DbContextOptions<AudiblyContext> dbContextOptions)
    {
        _dbContextOptions = dbContextOptions;
        // todo: prob need to re-comment the following 2 lines
        // using var db = new AudiblyContext(_dbContextOptions);
        // db.Database.EnsureCreated();
    }

    #region IAudiblyRepository Members

    public IAudiobookRepository Audiobooks => new SqlAudiobookRepository(
        new AudiblyContext(_dbContextOptions));

    public ICollectionRepository Collections => new SqlCollectionRepository(
        new AudiblyContext(_dbContextOptions));

    #endregion
}