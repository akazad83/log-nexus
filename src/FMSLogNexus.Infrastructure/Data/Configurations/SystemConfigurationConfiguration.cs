using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FMSLogNexus.Core.Entities;

namespace FMSLogNexus.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for SystemConfiguration.
/// Maps to [system].[Configuration] table.
/// </summary>
public class SystemConfigurationConfiguration : IEntityTypeConfiguration<SystemConfiguration>
{
    public void Configure(EntityTypeBuilder<SystemConfiguration> builder)
    {
        // Table mapping
        builder.ToTable("Configuration", "system");

        // Primary key (string)
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("ConfigKey")
            .HasMaxLength(100);

        // Required properties
        builder.Property(e => e.Value)
            .HasColumnName("ConfigValue")
            .IsRequired()
            .HasMaxLength(4000);

        // Optional properties with max lengths
        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.Category)
            .HasMaxLength(50);

        builder.Property(e => e.DataType)
            .HasMaxLength(50)
            .HasDefaultValue("string");

        // TODO: ValidationRegex property doesn't exist in SystemConfiguration entity
        // builder.Property(e => e.ValidationRegex)
        //     .HasMaxLength(500);

        // Audit columns
        builder.Property(e => e.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(e => e.UpdatedBy)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(e => e.Category)
            .HasDatabaseName("IX_Configuration_Category")
            .HasFilter("[Category] IS NOT NULL");
    }
}
