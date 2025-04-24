using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GolfStats.Api.Data.Entities;

public sealed record ScoreEntry
{
    public Guid Id { get; } = Guid.CreateVersion7();

    public required Guid SessionId { get; init; }
    public required string SteamId { get; init; }
    public required string Level { get; init; }
    public required int HoleIndex { get; init; }
    public required int Score { get; init; }
    public required DateTimeOffset Timestamp { get; init; }

    public GolfSession Session { get; init; }
}

public sealed class ScoreEntryConfiguration : IEntityTypeConfiguration<ScoreEntry>
{
    public void Configure(EntityTypeBuilder<ScoreEntry> builder)
    {
        builder.HasKey(x => x.Id);
    }
}
