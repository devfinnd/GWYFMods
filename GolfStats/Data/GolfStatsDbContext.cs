using GolfStats.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GolfStats.Data;

public sealed class GolfStatsDbContext(DbContextOptions<GolfStatsDbContext> options) : DbContext(options)
{
    public DbSet<GolfSession> Sessions { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<ScoreEntry> Scores { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GolfSession>()
            .HasKey(s => s.Id);

        modelBuilder.Entity<GolfSession>()
            .HasMany(x => x.Scores)
            .WithOne(x => x.Session);

        modelBuilder.Entity<ScoreEntry>()
            .HasKey(x => x.Id);

        modelBuilder.Entity<ScoreEntry>()
            .HasOne(x => x.Player)
            .WithMany(x => x.Scores)
            .HasForeignKey(x => x.SteamId);

        modelBuilder.Entity<Player>()
            .HasKey(x => x.SteamId);

        modelBuilder.Entity<Player>()
            .HasOne(x => x.CurrentSession)
            .WithMany(x => x.Players)
            .HasForeignKey(x => x.CurrentSessionId);
    }
}
