using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FMSLogNexus.Core.Entities;

namespace FMSLogNexus.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Job.
/// Maps to [job].[Jobs] table.
/// </summary>
public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        // Table mapping
        builder.ToTable("Jobs", "job");

        // Primary key (string)
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("JobId")
            .HasMaxLength(100);

        // Required properties
        builder.Property(e => e.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.JobType)
            .IsRequired();

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.IsCritical)
            .IsRequired()
            .HasDefaultValue(false);

        // Optional properties with max lengths
        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.Category)
            .HasMaxLength(100);

        builder.Property(e => e.Tags)
            .HasMaxLength(500);

        builder.Property(e => e.ServerName)
            .HasMaxLength(100);

        builder.Property(e => e.ExecutablePath)
            .HasMaxLength(1000);

        builder.Property(e => e.Schedule)
            .HasMaxLength(100);

        builder.Property(e => e.ScheduleDescription)
            .HasMaxLength(200);

        // JSON columns
        builder.Property(e => e.Configuration)
            .HasColumnType("nvarchar(max)");

        // Statistics columns
        builder.Property(e => e.LastExecutionId);
        builder.Property(e => e.LastExecutionAt);
        builder.Property(e => e.LastStatus);
        builder.Property(e => e.LastDurationMs);
        builder.Property(e => e.TotalExecutions).HasDefaultValue(0);
        builder.Property(e => e.SuccessCount).HasDefaultValue(0);
        builder.Property(e => e.FailureCount).HasDefaultValue(0);
        builder.Property(e => e.AvgDurationMs);

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
        builder.HasIndex(e => e.ServerName)
            .HasDatabaseName("IX_Jobs_ServerName")
            .HasFilter("[ServerName] IS NOT NULL");

        builder.HasIndex(e => e.Category)
            .HasDatabaseName("IX_Jobs_Category")
            .HasFilter("[Category] IS NOT NULL");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_Jobs_IsActive");

        builder.HasIndex(e => e.IsCritical)
            .HasDatabaseName("IX_Jobs_IsCritical")
            .HasFilter("[IsCritical] = 1");

        builder.HasIndex(e => e.LastStatus)
            .HasDatabaseName("IX_Jobs_LastStatus")
            .HasFilter("[LastStatus] IS NOT NULL");

        builder.HasIndex(e => new { e.IsActive, e.LastStatus })
            .HasDatabaseName("IX_Jobs_IsActive_LastStatus");

        // Relationships
        builder.HasOne(e => e.Server)
            .WithMany(s => s.Jobs)
            .HasForeignKey(e => e.ServerName)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Executions)
            .WithOne(je => je.Job)
            .HasForeignKey(je => je.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.LogEntries)
            .WithOne(le => le.Job)
            .HasForeignKey(le => le.JobId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Alerts)
            .WithOne(a => a.Job)
            .HasForeignKey(a => a.JobId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
