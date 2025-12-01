using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for User.
/// Maps to [auth].[Users] table.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table mapping
        builder.ToTable("Users", "auth");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        // Required properties
        builder.Property(e => e.Username)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Role)
            .IsRequired()
            .HasDefaultValue(UserRole.Viewer);

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.IsLocked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.FailedLoginCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.SecurityStamp)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValueSql("NEWID()");

        // Optional properties with max lengths
        builder.Property(e => e.DisplayName)
            .HasMaxLength(200);

        // JSON columns
        builder.Property(e => e.Preferences)
            .HasColumnType("nvarchar(max)");

        // Audit columns
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(100);

        builder.Property(e => e.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(e => e.UpdatedBy)
            .HasMaxLength(100);

        // Unique indexes
        builder.HasIndex(e => e.Username)
            .HasDatabaseName("UQ_Users_Username")
            .IsUnique();

        builder.HasIndex(e => e.Email)
            .HasDatabaseName("UQ_Users_Email")
            .IsUnique();

        // Regular indexes
        builder.HasIndex(e => e.Role)
            .HasDatabaseName("IX_Users_Role");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_Users_IsActive");

        builder.HasIndex(e => new { e.IsActive, e.Role })
            .HasDatabaseName("IX_Users_IsActive_Role");

        builder.HasIndex(e => e.IsLocked)
            .HasDatabaseName("IX_Users_IsLocked")
            .HasFilter("[IsLocked] = 1");

        // Relationships
        builder.HasMany(e => e.ApiKeys)
            .WithOne(ak => ak.User)
            .HasForeignKey(ak => ak.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
