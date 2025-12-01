using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FMSLogNexus.Core.Entities;

namespace FMSLogNexus.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for DashboardCacheEntry.
/// Maps to [system].[DashboardCache] table.
/// COMMENTED OUT: DashboardCacheEntry entity does not exist in FMSLogNexus.Core.Entities
/// </summary>
/*
public class DashboardCacheEntryConfiguration : IEntityTypeConfiguration<DashboardCacheEntry>
{
    public void Configure(EntityTypeBuilder<DashboardCacheEntry> builder)
    {
        // Table mapping
        builder.ToTable("DashboardCache", "system");

        // Primary key (string)
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("CacheKey")
            .HasMaxLength(200);

        // Required properties
        builder.Property(e => e.CacheValue)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.CachedAt)
            .IsRequired()
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(e => e.ExpiresAt)
            .IsRequired();

        // Optional properties with max lengths
        builder.Property(e => e.Category)
            .HasMaxLength(50);

        // Indexes
        builder.HasIndex(e => e.ExpiresAt)
            .HasDatabaseName("IX_DashboardCache_ExpiresAt");

        builder.HasIndex(e => e.Category)
            .HasDatabaseName("IX_DashboardCache_Category")
            .HasFilter("[Category] IS NOT NULL");

        builder.HasIndex(e => e.CachedAt)
            .HasDatabaseName("IX_DashboardCache_CachedAt");
    }
}
*/
