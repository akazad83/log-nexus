using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FMSLogNexus.Core.Entities;
using FMSLogNexus.Core.Enums;

namespace FMSLogNexus.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Server.
/// Maps to [job].[Servers] table.
/// </summary>
public class ServerConfiguration : IEntityTypeConfiguration<Server>
{
    public void Configure(EntityTypeBuilder<Server> builder)
    {
        // Table mapping
        builder.ToTable("Servers", "job");

        // Primary key (string)
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("ServerName")
            .HasMaxLength(100);

        // Required properties
        builder.Property(e => e.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasDefaultValue(ServerStatus.Unknown);

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.HeartbeatIntervalSeconds)
            .IsRequired()
            .HasDefaultValue(60);

        // Optional properties with max lengths
        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(50);

        builder.Property(e => e.AgentVersion)
            .HasMaxLength(50);

        builder.Property(e => e.AgentType)
            .HasMaxLength(50);

        // JSON columns
        builder.Property(e => e.Metadata)
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
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_Servers_Status");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_Servers_IsActive");

        builder.HasIndex(e => e.LastHeartbeat)
            .HasDatabaseName("IX_Servers_LastHeartbeat");

        builder.HasIndex(e => new { e.IsActive, e.Status })
            .HasDatabaseName("IX_Servers_IsActive_Status");

        // Relationships (inverse navigation)
        builder.HasMany(e => e.Jobs)
            .WithOne(j => j.Server)
            .HasForeignKey(j => j.ServerName)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Executions)
            .WithOne(je => je.Server)
            .HasForeignKey(je => je.ServerName)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.LogEntries)
            .WithOne(le => le.Server)
            .HasForeignKey(le => le.ServerName)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.ApiKeys)
            .WithOne(ak => ak.Server)
            .HasForeignKey(ak => ak.ServerName)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Alerts)
            .WithOne(a => a.Server)
            .HasForeignKey(a => a.ServerName)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
