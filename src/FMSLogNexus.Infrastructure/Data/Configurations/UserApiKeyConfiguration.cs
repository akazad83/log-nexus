using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FMSLogNexus.Core.Entities;

namespace FMSLogNexus.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for UserApiKey.
/// Maps to [auth].[UserApiKeys] table.
/// </summary>
public class UserApiKeyConfiguration : IEntityTypeConfiguration<UserApiKey>
{
    public void Configure(EntityTypeBuilder<UserApiKey> builder)
    {
        // Table mapping
        builder.ToTable("UserApiKeys", "auth");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        // Required properties
        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.KeyHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.KeyPrefix)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Optional properties with max lengths
        builder.Property(e => e.ServerName)
            .HasMaxLength(100);

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.Scopes)
            .HasMaxLength(1000);

        builder.Property(e => e.RevokedBy)
            .HasMaxLength(100);

        builder.Property(e => e.RevocationReason)
            .HasMaxLength(500);

        builder.Property(e => e.LastUsedIp)
            .HasMaxLength(50);

        // Indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_UserApiKeys_UserId");

        builder.HasIndex(e => e.KeyPrefix)
            .HasDatabaseName("IX_UserApiKeys_KeyPrefix");

        builder.HasIndex(e => e.KeyHash)
            .HasDatabaseName("IX_UserApiKeys_KeyHash")
            .IsUnique();

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_UserApiKeys_IsActive");

        builder.HasIndex(e => e.RevokedAt)
            .HasDatabaseName("IX_UserApiKeys_RevokedAt")
            .HasFilter("[RevokedAt] IS NOT NULL");

        builder.HasIndex(e => new { e.UserId, e.IsActive })
            .HasDatabaseName("IX_UserApiKeys_UserId_IsActive");

        builder.HasIndex(e => e.ExpiresAt)
            .HasDatabaseName("IX_UserApiKeys_ExpiresAt")
            .HasFilter("[ExpiresAt] IS NOT NULL");

        builder.HasIndex(e => e.ServerName)
            .HasDatabaseName("IX_UserApiKeys_ServerName")
            .HasFilter("[ServerName] IS NOT NULL");

        // Relationships
        builder.HasOne(e => e.User)
            .WithMany(u => u.ApiKeys)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Server)
            .WithMany(s => s.ApiKeys)
            .HasForeignKey(e => e.ServerName)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
