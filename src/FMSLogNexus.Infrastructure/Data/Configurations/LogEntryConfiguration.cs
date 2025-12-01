using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FMSLogNexus.Core.Entities;

namespace FMSLogNexus.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for LogEntry.
/// Maps to [log].[LogEntries] table.
/// </summary>
public class LogEntryConfiguration : IEntityTypeConfiguration<LogEntry>
{
    public void Configure(EntityTypeBuilder<LogEntry> builder)
    {
        // Table mapping
        builder.ToTable("LogEntries", "log");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasDefaultValueSql("NEWSEQUENTIALID()");

        // Required properties
        builder.Property(e => e.Timestamp)
            .IsRequired();

        builder.Property(e => e.Level)
            .IsRequired();

        builder.Property(e => e.Message)
            .IsRequired()
            .HasMaxLength(8000);

        builder.Property(e => e.ServerName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.ReceivedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        // Optional properties with max lengths
        builder.Property(e => e.MessageTemplate)
            .HasMaxLength(2000);

        builder.Property(e => e.JobId)
            .HasMaxLength(100);

        builder.Property(e => e.Category)
            .HasMaxLength(500);

        builder.Property(e => e.SourceContext)
            .HasMaxLength(500);

        builder.Property(e => e.CorrelationId)
            .HasMaxLength(100);

        builder.Property(e => e.TraceId)
            .HasMaxLength(64);

        builder.Property(e => e.SpanId)
            .HasMaxLength(32);

        builder.Property(e => e.ParentSpanId)
            .HasMaxLength(32);

        builder.Property(e => e.ExceptionType)
            .HasMaxLength(500);

        builder.Property(e => e.ExceptionMessage)
            .HasMaxLength(4000);

        builder.Property(e => e.ExceptionSource)
            .HasMaxLength(500);

        builder.Property(e => e.Environment)
            .HasMaxLength(50);

        builder.Property(e => e.ApplicationVersion)
            .HasMaxLength(50);

        builder.Property(e => e.ClientIp)
            .HasMaxLength(50);

        // JSON columns
        builder.Property(e => e.Properties)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.Tags)
            .HasMaxLength(1000);

        builder.Property(e => e.ExceptionStackTrace)
            .HasColumnType("nvarchar(max)");

        // Indexes for common queries
        builder.HasIndex(e => e.Timestamp)
            .HasDatabaseName("IX_LogEntries_Timestamp");

        builder.HasIndex(e => new { e.Timestamp, e.Level })
            .HasDatabaseName("IX_LogEntries_Timestamp_Level");

        builder.HasIndex(e => new { e.JobId, e.Timestamp })
            .HasDatabaseName("IX_LogEntries_JobId_Timestamp")
            .HasFilter("[JobId] IS NOT NULL");

        builder.HasIndex(e => new { e.JobExecutionId, e.Timestamp })
            .HasDatabaseName("IX_LogEntries_JobExecutionId_Timestamp")
            .HasFilter("[JobExecutionId] IS NOT NULL");

        builder.HasIndex(e => new { e.ServerName, e.Timestamp })
            .HasDatabaseName("IX_LogEntries_ServerName_Timestamp");

        builder.HasIndex(e => e.CorrelationId)
            .HasDatabaseName("IX_LogEntries_CorrelationId")
            .HasFilter("[CorrelationId] IS NOT NULL");

        builder.HasIndex(e => e.TraceId)
            .HasDatabaseName("IX_LogEntries_TraceId")
            .HasFilter("[TraceId] IS NOT NULL");

        builder.HasIndex(e => new { e.Level, e.Timestamp })
            .HasDatabaseName("IX_LogEntries_Level_Timestamp")
            .HasFilter("[Level] >= 4"); // Errors and Critical only

        // Relationships - Use NoAction to avoid cascade path conflicts with SQL Server
        builder.HasOne(e => e.Job)
            .WithMany(j => j.LogEntries)
            .HasForeignKey(e => e.JobId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(e => e.JobExecution)
            .WithMany(je => je.LogEntries)
            .HasForeignKey(e => e.JobExecutionId)
            .OnDelete(DeleteBehavior.NoAction);

        // Server relationship: LogEntry.ServerName -> Server.Id (which is ServerName)
        builder.HasOne(e => e.Server)
            .WithMany(s => s.LogEntries)
            .HasForeignKey(e => e.ServerName)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
