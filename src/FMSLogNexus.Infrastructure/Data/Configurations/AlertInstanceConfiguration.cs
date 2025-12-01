using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for AlertInstance.
/// Maps to [alert].[AlertInstances] table.
/// </summary>
public class AlertInstanceConfiguration : IEntityTypeConfiguration<AlertInstance>
{
    public void Configure(EntityTypeBuilder<AlertInstance> builder)
    {
        // Table mapping
        builder.ToTable("AlertInstances", "alert");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        // Required properties
        builder.Property(e => e.AlertId)
            .IsRequired();

        builder.Property(e => e.TriggeredAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(e => e.Status)
            .IsRequired()
            .HasDefaultValue(AlertInstanceStatus.New);

        builder.Property(e => e.Severity)
            .IsRequired();

        builder.Property(e => e.Message)
            .IsRequired()
            .HasMaxLength(4000);

        // Optional properties with max lengths
        builder.Property(e => e.JobId)
            .HasMaxLength(100);

        builder.Property(e => e.ServerName)
            .HasMaxLength(100);

        builder.Property(e => e.AcknowledgedBy)
            .HasMaxLength(100);

        builder.Property(e => e.AcknowledgeNote)
            .HasMaxLength(1000);

        builder.Property(e => e.ResolvedBy)
            .HasMaxLength(100);

        builder.Property(e => e.ResolutionNote)
            .HasMaxLength(1000);

        builder.Property(e => e.NotificationsSent)
            .HasColumnType("nvarchar(max)");

        // JSON columns
        builder.Property(e => e.Context)
            .HasColumnType("nvarchar(max)");

        // Indexes
        builder.HasIndex(e => e.AlertId)
            .HasDatabaseName("IX_AlertInstances_AlertId");

        builder.HasIndex(e => e.TriggeredAt)
            .HasDatabaseName("IX_AlertInstances_TriggeredAt")
            .IsDescending(true);

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_AlertInstances_Status");

        builder.HasIndex(e => e.Severity)
            .HasDatabaseName("IX_AlertInstances_Severity");

        builder.HasIndex(e => new { e.Status, e.TriggeredAt })
            .HasDatabaseName("IX_AlertInstances_Status_TriggeredAt")
            .HasFilter("[Status] IN (0, 1)"); // New, Acknowledged

        builder.HasIndex(e => new { e.AlertId, e.Status })
            .HasDatabaseName("IX_AlertInstances_AlertId_Status");

        builder.HasIndex(e => e.JobId)
            .HasDatabaseName("IX_AlertInstances_JobId")
            .HasFilter("[JobId] IS NOT NULL");

        builder.HasIndex(e => e.ServerName)
            .HasDatabaseName("IX_AlertInstances_ServerName")
            .HasFilter("[ServerName] IS NOT NULL");

        // Relationships
        builder.HasOne(e => e.Alert)
            .WithMany(a => a.Instances)
            .HasForeignKey(e => e.AlertId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
