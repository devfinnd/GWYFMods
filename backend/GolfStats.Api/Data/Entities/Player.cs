using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GolfStats.Api.Data.Entities;

public sealed record Player
{
    public string SteamId { get; set; }
    public string DisplayName { get; set; }
    public Guid? CurrentSessionId { get; set; }
    public GolfSession? CurrentSession { get; set; }
    public ICollection<ScoreEntry> Scores { get; init; } = [];
}

public sealed class PlayerConfiguration : IEntityTypeConfiguration<Player>
{
    public void Configure(EntityTypeBuilder<Player> builder)
    {
        builder.HasKey(x => x.SteamId);

        builder.HasMany(x => x.Scores)
            .WithOne()
            .HasForeignKey(x => x.SteamId);

        builder.HasOne(x => x.CurrentSession)
            .WithMany(x => x.Players)
            .HasForeignKey(x => x.CurrentSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
