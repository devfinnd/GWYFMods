using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GolfStats.Api.Data.Entities;

public sealed record GolfSession
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }

    public ICollection<Player> Players { get; init; } = [];
    public ICollection<ScoreEntry> Scores { get; init; } = [];
}

public sealed class GolfSessionConfiguration : IEntityTypeConfiguration<GolfSession>
{
    public void Configure(EntityTypeBuilder<GolfSession> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasMany(x => x.Players)
            .WithOne(x => x.CurrentSession)
            .HasForeignKey(x => x.CurrentSessionId);

        builder.HasMany(x => x.Scores)
            .WithOne(x => x.Session)
            .HasForeignKey(x => x.SessionId);
    }
}
