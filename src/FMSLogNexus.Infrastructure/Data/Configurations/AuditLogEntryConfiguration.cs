using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FMSLogNexus.Core.Entities;

namespace FMSLogNexus.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for AuditLogEntry.
/// Maps to [system].[AuditLog] table.
/// COMMENTED OUT: AuditLogEntry entity does not exist in FMSLogNexus.Core.Entities
/// </summary>
/*
public class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        // Table mapping
        builder.ToTable("AuditLog", "system");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        // Required properties
        builder.Property(e => e.Timestamp)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(e => e.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        // Optional properties with max lengths
        builder.Property(e => e.UserId);

        builder.Property(e => e.Username)
            .HasMaxLength(100);

        builder.Property(e => e.EntityId)
            .HasMaxLength(100);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(50);

        // JSON columns
        builder.Property(e => e.OldValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.NewValues)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.AdditionalData)
            .HasColumnType("nvarchar(max)");

        // Indexes
        builder.HasIndex(e => e.Timestamp)
            .HasDatabaseName("IX_AuditLog_Timestamp")
            .IsDescending(true);

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_AuditLog_UserId")
            .HasFilter("[UserId] IS NOT NULL");

        builder.HasIndex(e => e.Action)
            .HasDatabaseName("IX_AuditLog_Action");

        builder.HasIndex(e => e.EntityType)
            .HasDatabaseName("IX_AuditLog_EntityType");

        builder.HasIndex(e => new { e.EntityType, e.EntityId })
            .HasDatabaseName("IX_AuditLog_EntityType_EntityId");

        builder.HasIndex(e => new { e.UserId, e.Timestamp })
            .HasDatabaseName("IX_AuditLog_UserId_Timestamp");

        builder.HasIndex(e => new { e.Action, e.Timestamp })
            .HasDatabaseName("IX_AuditLog_Action_Timestamp");
    }
}
*/
