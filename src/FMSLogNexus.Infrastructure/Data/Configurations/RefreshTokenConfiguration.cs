using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FMSLogNexus.Core.Entities;

namespace FMSLogNexus.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for RefreshToken.
/// Maps to [auth].[RefreshTokens] table.
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        // Table mapping
        builder.ToTable("RefreshTokens", "auth");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        // Required properties
        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.TokenHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(e => e.ExpiresAt)
            .IsRequired();

        // Optional properties with max lengths
        builder.Property(e => e.CreatedByIp)
            .HasMaxLength(50);

        builder.Property(e => e.CreatedByUserAgent)
            .HasMaxLength(500);

        builder.Property(e => e.RevokedByIp)
            .HasMaxLength(50);

        builder.Property(e => e.RevokedReason)
            .HasMaxLength(200);

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");

        builder.HasIndex(e => e.TokenHash)
            .HasDatabaseName("IX_RefreshTokens_TokenHash")
            .IsUnique();

        builder.HasIndex(e => e.ExpiresAt)
            .HasDatabaseName("IX_RefreshTokens_ExpiresAt");

        builder.HasIndex(e => e.RevokedAt)
            .HasDatabaseName("IX_RefreshTokens_RevokedAt")
            .HasFilter("[RevokedAt] IS NOT NULL");

        builder.HasIndex(e => new { e.UserId, e.RevokedAt, e.ExpiresAt })
            .HasDatabaseName("IX_RefreshTokens_UserId_RevokedAt_ExpiresAt");

        // Composite index for cleanup queries (no filter - SQL Server doesn't support OR or non-deterministic functions in filtered indexes)
        builder.HasIndex(e => new { e.RevokedAt, e.ExpiresAt })
            .HasDatabaseName("IX_RefreshTokens_Cleanup");

        // Relationships
        builder.HasOne(e => e.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
