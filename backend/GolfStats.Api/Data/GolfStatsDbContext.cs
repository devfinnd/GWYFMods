using GolfStats.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GolfStats.Api.Data;

public sealed class GolfStatsDbContext(DbContextOptions<GolfStatsDbContext> options) : DbContext(options)
{
    public DbSet<GolfSession> Sessions { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<ScoreEntry> Scores { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GolfStatsDbContext).Assembly);
    }
}
