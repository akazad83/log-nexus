using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FMSLogNexus.Core.Entities;

namespace FMSLogNexus.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for JobExecution.
/// Maps to [job].[JobExecutions] table.
/// </summary>
public class JobExecutionConfiguration : IEntityTypeConfiguration<JobExecution>
{
    public void Configure(EntityTypeBuilder<JobExecution> builder)
    {
        // Table mapping
        builder.ToTable("JobExecutions", "job");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        // Required properties
        builder.Property(e => e.JobId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.StartedAt)
            .IsRequired();

        builder.Property(e => e.Status)
            .IsRequired();

        builder.Property(e => e.ServerName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.TriggerType)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Manual");

        // Optional properties with max lengths
        builder.Property(e => e.TriggeredBy)
            .HasMaxLength(200);

        builder.Property(e => e.CorrelationId)
            .HasMaxLength(100);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(4000);

        builder.Property(e => e.ErrorCategory)
            .HasMaxLength(100);

        // JSON columns
        builder.Property(e => e.Parameters)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.ResultSummary)
            .HasColumnType("nvarchar(max)");

        // Log count columns with defaults
        builder.Property(e => e.LogCount).HasDefaultValue(0);
        builder.Property(e => e.TraceCount).HasDefaultValue(0);
        builder.Property(e => e.DebugCount).HasDefaultValue(0);
        builder.Property(e => e.InfoCount).HasDefaultValue(0);
        builder.Property(e => e.WarningCount).HasDefaultValue(0);
        builder.Property(e => e.ErrorCount).HasDefaultValue(0);
        builder.Property(e => e.CriticalCount).HasDefaultValue(0);

        // Indexes
        builder.HasIndex(e => new { e.JobId, e.StartedAt })
            .HasDatabaseName("IX_JobExecutions_JobId_StartedAt")
            .IsDescending(false, true);

        builder.HasIndex(e => e.StartedAt)
            .HasDatabaseName("IX_JobExecutions_StartedAt")
            .IsDescending(true);

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_JobExecutions_Status");

        builder.HasIndex(e => new { e.Status, e.StartedAt })
            .HasDatabaseName("IX_JobExecutions_Status_StartedAt")
            .HasFilter("[Status] IN (0, 1)"); // Pending, Running

        builder.HasIndex(e => e.ServerName)
            .HasDatabaseName("IX_JobExecutions_ServerName");

        builder.HasIndex(e => e.CorrelationId)
            .HasDatabaseName("IX_JobExecutions_CorrelationId")
            .HasFilter("[CorrelationId] IS NOT NULL");

        builder.HasIndex(e => new { e.JobId, e.Status })
            .HasDatabaseName("IX_JobExecutions_JobId_Status");

        // Relationships
        builder.HasOne(e => e.Job)
            .WithMany(j => j.Executions)
            .HasForeignKey(e => e.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Server)
            .WithMany(s => s.Executions)
            .HasForeignKey(e => e.ServerName)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.LogEntries)
            .WithOne(le => le.JobExecution)
            .HasForeignKey(le => le.JobExecutionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
