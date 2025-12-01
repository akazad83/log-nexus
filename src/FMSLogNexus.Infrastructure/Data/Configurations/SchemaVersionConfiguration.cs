using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FMSLogNexus.Core.Entities;

namespace FMSLogNexus.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for SchemaVersion.
/// Maps to [system].[SchemaVersions] table.
/// COMMENTED OUT: SchemaVersion entity does not exist in FMSLogNexus.Core.Entities
/// </summary>
/*
public class SchemaVersionConfiguration : IEntityTypeConfiguration<SchemaVersion>
{
    public void Configure(EntityTypeBuilder<SchemaVersion> builder)
    {
        // Table mapping
        builder.ToTable("SchemaVersions", "system");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        // Required properties
        builder.Property(e => e.Version)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.ScriptName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.AppliedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(e => e.Success)
            .IsRequired()
            .HasDefaultValue(true);

        // Optional properties with max lengths
        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.AppliedBy)
            .HasMaxLength(100);

        builder.Property(e => e.Checksum)
            .HasMaxLength(100);

        builder.Property(e => e.ErrorMessage)
            .HasMaxLength(4000);

        // Indexes
        builder.HasIndex(e => e.Version)
            .HasDatabaseName("UQ_SchemaVersions_Version")
            .IsUnique();

        builder.HasIndex(e => e.AppliedAt)
            .HasDatabaseName("IX_SchemaVersions_AppliedAt");

        builder.HasIndex(e => e.Success)
            .HasDatabaseName("IX_SchemaVersions_Success")
            .HasFilter("[Success] = 0");
    }
}
*/
