using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Alert.
/// Maps to [alert].[Alerts] table.
/// </summary>
public class AlertConfiguration : IEntityTypeConfiguration<Alert>
{
    public void Configure(EntityTypeBuilder<Alert> builder)
    {
        // Table mapping
        builder.ToTable("Alerts", "alert");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        // Required properties
        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.AlertType)
            .IsRequired();

        builder.Property(e => e.Severity)
            .IsRequired()
            .HasDefaultValue(AlertSeverity.Medium);

        builder.Property(e => e.Condition)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.ThrottleMinutes)
            .IsRequired()
            .HasDefaultValue(15);

        builder.Property(e => e.TriggerCount)
            .IsRequired()
            .HasDefaultValue(0);

        // Optional properties with max lengths
        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.JobId)
            .HasMaxLength(100);

        builder.Property(e => e.ServerName)
            .HasMaxLength(100);

        // JSON columns
        builder.Property(e => e.NotificationChannels)
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

        // Indexes
        builder.HasIndex(e => e.AlertType)
            .HasDatabaseName("IX_Alerts_AlertType");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_Alerts_IsActive");

        builder.HasIndex(e => e.Severity)
            .HasDatabaseName("IX_Alerts_Severity");

        builder.HasIndex(e => e.JobId)
            .HasDatabaseName("IX_Alerts_JobId")
            .HasFilter("[JobId] IS NOT NULL");

        builder.HasIndex(e => e.ServerName)
            .HasDatabaseName("IX_Alerts_ServerName")
            .HasFilter("[ServerName] IS NOT NULL");

        builder.HasIndex(e => new { e.IsActive, e.AlertType })
            .HasDatabaseName("IX_Alerts_IsActive_AlertType");

        // Relationships
        builder.HasOne(e => e.Job)
            .WithMany(j => j.Alerts)
            .HasForeignKey(e => e.JobId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Server)
            .WithMany(s => s.Alerts)
            .HasForeignKey(e => e.ServerName)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Instances)
            .WithOne(ai => ai.Alert)
            .HasForeignKey(ai => ai.AlertId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
